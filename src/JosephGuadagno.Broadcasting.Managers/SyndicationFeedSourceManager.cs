using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class SyndicationFeedSourceManager : ISyndicationFeedSourceManager
{
    private readonly ISyndicationFeedSourceDataStore _syndicationFeedSourceDataStore;
    private readonly IMemoryCache _cache;

    private const string CacheKeyAll = "SyndicationFeedSources_All";
    private static string CacheKeyByUser(string ownerEntraOid) => $"SyndicationFeedSources_User_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public SyndicationFeedSourceManager(ISyndicationFeedSourceDataStore syndicationFeedSourceDataStore, IMemoryCache cache)
    {
        _syndicationFeedSourceDataStore = syndicationFeedSourceDataStore;
        _cache = cache;
    }

    public async Task<SyndicationFeedSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<SyndicationFeedSource>> SaveAsync(SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        var result = await _syndicationFeedSourceDataStore.SaveAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<List<SyndicationFeedSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKeyAll, out List<SyndicationFeedSource>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _syndicationFeedSourceDataStore.GetAllAsync(cancellationToken);
        _cache.Set(CacheKeyAll, result, CacheOptions);
        return result;
    }

    public async Task<List<SyndicationFeedSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyByUser(ownerEntraOid);
        if (_cache.TryGetValue(cacheKey, out List<SyndicationFeedSource>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _syndicationFeedSourceDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        _cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        var result = await _syndicationFeedSourceDataStore.DeleteAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var result = await _syndicationFeedSourceDataStore.DeleteAsync(primaryKey, cancellationToken);
        _cache.Remove(CacheKeyAll);
        return result;
    }

    public async Task<SyndicationFeedSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetByUrlAsync(url, cancellationToken);
    }

    public async Task<SyndicationFeedSource?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetByFeedIdentifierAsync(feedIdentifier, cancellationToken);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetCollectorOwnerOidAsync(cancellationToken);
    }

    public async Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories, cancellationToken);
    }

    public async Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(string ownerEntraOid, DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetRandomSyndicationDataAsync(ownerEntraOid, cutoffDate, excludedCategories, cancellationToken);
    }

    public async Task<PagedResult<SyndicationFeedSource>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<SyndicationFeedSource>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    private void InvalidateUserCaches(string ownerEntraOid)
    {
        _cache.Remove(CacheKeyAll);
        _cache.Remove(CacheKeyByUser(ownerEntraOid));
    }
}
