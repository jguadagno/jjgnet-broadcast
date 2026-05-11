using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class YouTubeItemManager : IYouTubeItemManager
{
    private readonly IYouTubeItemDataStore _YouTubeItemDataStore;
    private readonly IMemoryCache _cache;

    private const string CacheKeyAll = "YouTubeItems_All";
    private static string CacheKeyByUser(string ownerEntraOid) => $"YouTubeItems_User_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public YouTubeItemManager(IYouTubeItemDataStore YouTubeItemDataStore, IMemoryCache cache)
    {
        _YouTubeItemDataStore = YouTubeItemDataStore;
        _cache = cache;
    }

    public async Task<YouTubeItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _YouTubeItemDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<YouTubeItem>> SaveAsync(YouTubeItem entity, CancellationToken cancellationToken = default)
    {
        var result = await _YouTubeItemDataStore.SaveAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<List<YouTubeItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKeyAll, out List<YouTubeItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _YouTubeItemDataStore.GetAllAsync(cancellationToken);
        _cache.Set(CacheKeyAll, result, CacheOptions);
        return result;
    }

    public async Task<List<YouTubeItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyByUser(ownerEntraOid);
        if (_cache.TryGetValue(cacheKey, out List<YouTubeItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _YouTubeItemDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        _cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(YouTubeItem entity, CancellationToken cancellationToken = default)
    {
        var result = await _YouTubeItemDataStore.DeleteAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var result = await _YouTubeItemDataStore.DeleteAsync(primaryKey, cancellationToken);
        _cache.Remove(CacheKeyAll);
        return result;
    }

    public async Task<YouTubeItem?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _YouTubeItemDataStore.GetByUrlAsync(url, cancellationToken);
    }

    public async Task<YouTubeItem?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        return await _YouTubeItemDataStore.GetByVideoIdAsync(videoId, cancellationToken);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        return await _YouTubeItemDataStore.GetCollectorOwnerOidAsync(cancellationToken);
    }

    public async Task<PagedResult<YouTubeItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _YouTubeItemDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<YouTubeItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _YouTubeItemDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    private void InvalidateUserCaches(string ownerEntraOid)
    {
        _cache.Remove(CacheKeyAll);
        _cache.Remove(CacheKeyByUser(ownerEntraOid));
    }
}
