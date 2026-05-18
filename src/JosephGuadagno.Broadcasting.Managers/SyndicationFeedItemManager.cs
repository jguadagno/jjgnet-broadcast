using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class SyndicationFeedItemManager(ISyndicationFeedItemDataStore syndicationFeedItemDataStore, IMemoryCache cache)
	: ISyndicationFeedItemManager
{
	private const string CacheKeyAll = "SyndicationFeedItems_All";
    private static string CacheKeyByUser(string ownerEntraOid) => $"SyndicationFeedItems_User_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public async Task<SyndicationFeedItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<SyndicationFeedItem>> SaveAsync(SyndicationFeedItem entity, CancellationToken cancellationToken = default)
    {
        var result = await syndicationFeedItemDataStore.SaveAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<List<SyndicationFeedItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKeyAll, out List<SyndicationFeedItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await syndicationFeedItemDataStore.GetAllAsync(cancellationToken);
        cache.Set(CacheKeyAll, result, CacheOptions);
        return result;
    }

    public async Task<List<SyndicationFeedItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyByUser(ownerEntraOid);
        if (cache.TryGetValue(cacheKey, out List<SyndicationFeedItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await syndicationFeedItemDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(SyndicationFeedItem entity, CancellationToken cancellationToken = default)
    {
        var result = await syndicationFeedItemDataStore.DeleteAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var result = await syndicationFeedItemDataStore.DeleteAsync(primaryKey, cancellationToken);
        cache.Remove(CacheKeyAll);
        return result;
    }

    public async Task<SyndicationFeedItem?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.GetByFeedIdentifierAsync(feedIdentifier, cancellationToken);
    }

    public async Task<bool> IsFeedItemUniqueToUser(string feedIdentifier, string ownerOid, CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.IsFeedItemUniqueToUser(feedIdentifier, ownerOid, cancellationToken);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.GetCollectorOwnerOidAsync(cancellationToken);
    }

    public async Task<SyndicationFeedItem?> GetRandomSyndicationDataAsync(string ownerEntraOid, DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.GetRandomSyndicationDataAsync(ownerEntraOid, cutoffDate, excludedCategories, cancellationToken);
    }

    public async Task<PagedResult<SyndicationFeedItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<SyndicationFeedItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await syndicationFeedItemDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    private void InvalidateUserCaches(string ownerEntraOid)
    {
        cache.Remove(CacheKeyAll);
        cache.Remove(CacheKeyByUser(ownerEntraOid));
    }
}
