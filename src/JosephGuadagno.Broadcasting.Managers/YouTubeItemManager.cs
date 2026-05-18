using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class YouTubeItemManager(IYouTubeItemDataStore youTubeItemDataStore, IMemoryCache cache)
	: IYouTubeItemManager
{
	private const string CacheKeyAll = "YouTubeItems_All";
    private static string CacheKeyByUser(string ownerEntraOid) => $"YouTubeItems_User_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public async Task<YouTubeItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<YouTubeItem>> SaveAsync(YouTubeItem entity, CancellationToken cancellationToken = default)
    {
        var result = await youTubeItemDataStore.SaveAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<List<YouTubeItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKeyAll, out List<YouTubeItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await youTubeItemDataStore.GetAllAsync(cancellationToken);
        cache.Set(CacheKeyAll, result, CacheOptions);
        return result;
    }

    public async Task<List<YouTubeItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyByUser(ownerEntraOid);
        if (cache.TryGetValue(cacheKey, out List<YouTubeItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await youTubeItemDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(YouTubeItem entity, CancellationToken cancellationToken = default)
    {
        var result = await youTubeItemDataStore.DeleteAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var result = await youTubeItemDataStore.DeleteAsync(primaryKey, cancellationToken);
        cache.Remove(CacheKeyAll);
        return result;
    }

    public async Task<YouTubeItem?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.GetByUrlAsync(url, cancellationToken);
    }

    public async Task<YouTubeItem?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.GetByVideoIdAsync(videoId, cancellationToken);
    }

    public async Task<bool> IsVideoUniqueToUser(string videoId, string ownerOid, CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.IsVideoUniqueToUser(videoId, ownerOid, cancellationToken);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.GetCollectorOwnerOidAsync(cancellationToken);
    }

    public async Task<PagedResult<YouTubeItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<YouTubeItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await youTubeItemDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    private void InvalidateUserCaches(string ownerEntraOid)
    {
        cache.Remove(CacheKeyAll);
        cache.Remove(CacheKeyByUser(ownerEntraOid));
    }
}
