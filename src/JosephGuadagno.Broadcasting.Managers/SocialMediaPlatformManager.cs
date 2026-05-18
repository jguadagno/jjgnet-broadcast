using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class SocialMediaPlatformManager(ISocialMediaPlatformDataStore dataStore, IMemoryCache cache)
	: ISocialMediaPlatformManager
{
	private const string CacheKeyAllActive = "SocialMediaPlatforms_All";
    private const string CacheKeyAllIncludingInactive = "SocialMediaPlatforms_AllIncludingInactive";
    private static string CacheKeyById(int id) => $"SocialMediaPlatform_{id}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public async Task<List<SocialMediaPlatform>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = includeInactive ? CacheKeyAllIncludingInactive : CacheKeyAllActive;
        if (cache.TryGetValue(cacheKey, out List<SocialMediaPlatform>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await dataStore.GetAllAsync(includeInactive: includeInactive, cancellationToken);
        cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<List<SocialMediaPlatform>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(includeInactive: true, cancellationToken);
    }

    public async Task<SocialMediaPlatform?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyById(id);
        if (cache.TryGetValue(cacheKey, out SocialMediaPlatform? cached))
        {
            return cached;
        }

        var result = await dataStore.GetAsync(id, cancellationToken);
        if (result is not null)
        {
            cache.Set(cacheKey, result, CacheOptions);
        }
        return result;
    }

    public async Task<SocialMediaPlatform?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await dataStore.GetByNameAsync(name, cancellationToken);
    }

    public async Task<SocialMediaPlatform?> AddAsync(SocialMediaPlatform platform, CancellationToken cancellationToken = default)
    {
        var result = await dataStore.AddAsync(platform, cancellationToken);
        InvalidateListCaches();
        return result;
    }

    public async Task<SocialMediaPlatform?> UpdateAsync(SocialMediaPlatform platform, CancellationToken cancellationToken = default)
    {
        var result = await dataStore.UpdateAsync(platform, cancellationToken);
        InvalidateListCaches();
        cache.Remove(CacheKeyById(platform.Id));
        return result;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await dataStore.DeleteAsync(id, cancellationToken);
        InvalidateListCaches();
        cache.Remove(CacheKeyById(id));
        return result;
    }

    public async Task<PagedResult<SocialMediaPlatform>> GetAllAsync(int page, int pageSize, string sortBy = "name", bool sortDescending = false, string? filter = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(includeInactive, cancellationToken);

        IEnumerable<SocialMediaPlatform> query = all;

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        query = sortBy.ToLowerInvariant() switch
        {
            "url"  => sortDescending ? query.OrderByDescending(p => p.Url)  : query.OrderBy(p => p.Url),
            "icon" => sortDescending ? query.OrderByDescending(p => p.Icon) : query.OrderBy(p => p.Icon),
            "isactive" => sortDescending ? query.OrderByDescending(p => p.IsActive) : query.OrderBy(p => p.IsActive),
            "credentialsetupdocumentationurl" => sortDescending ? query.OrderByDescending(p => p.CredentialSetupDocumentationUrl) : query.OrderBy(p => p.CredentialSetupDocumentationUrl),
            _ => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
        };

        var filtered = query.ToList();
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<SocialMediaPlatform> { Items = items, TotalCount = filtered.Count };
    }

    private void InvalidateListCaches()
    {
        cache.Remove(CacheKeyAllActive);
        cache.Remove(CacheKeyAllIncludingInactive);
    }
}

