using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class YouTubeSourceManager : IYouTubeSourceManager
{
    private readonly IYouTubeSourceDataStore _youTubeSourceDataStore;
    private readonly IMemoryCache _cache;

    private const string CacheKeyAll = "YouTubeSources_All";
    private static string CacheKeyByUser(string ownerEntraOid) => $"YouTubeSources_User_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public YouTubeSourceManager(IYouTubeSourceDataStore youTubeSourceDataStore, IMemoryCache cache)
    {
        _youTubeSourceDataStore = youTubeSourceDataStore;
        _cache = cache;
    }

    public async Task<YouTubeSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<YouTubeSource>> SaveAsync(YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        var result = await _youTubeSourceDataStore.SaveAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<List<YouTubeSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKeyAll, out List<YouTubeSource>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _youTubeSourceDataStore.GetAllAsync(cancellationToken);
        _cache.Set(CacheKeyAll, result, CacheOptions);
        return result;
    }

    public async Task<List<YouTubeSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyByUser(ownerEntraOid);
        if (_cache.TryGetValue(cacheKey, out List<YouTubeSource>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _youTubeSourceDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        _cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        var result = await _youTubeSourceDataStore.DeleteAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var result = await _youTubeSourceDataStore.DeleteAsync(primaryKey, cancellationToken);
        _cache.Remove(CacheKeyAll);
        return result;
    }

    public async Task<YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetByUrlAsync(url, cancellationToken);
    }

    public async Task<YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetByVideoIdAsync(videoId, cancellationToken);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetCollectorOwnerOidAsync(cancellationToken);
    }

    public async Task<PagedResult<YouTubeSource>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<YouTubeSource>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _youTubeSourceDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    private void InvalidateUserCaches(string ownerEntraOid)
    {
        _cache.Remove(CacheKeyAll);
        _cache.Remove(CacheKeyByUser(ownerEntraOid));
    }
}
