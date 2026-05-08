using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Managers;

public class ScheduledItemManager : IScheduledItemManager
{
    private readonly IScheduledItemDataStore _scheduledItemDataStore;
    private readonly IMemoryCache _cache;

    private static string CacheKeyAllByOwner(string ownerEntraOid) => $"ScheduledItems_All_{ownerEntraOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public ScheduledItemManager(IScheduledItemDataStore scheduledItemDataStore, IMemoryCache cache)
    {
        _scheduledItemDataStore = scheduledItemDataStore;
        _cache = cache;
    }

    public async Task<ScheduledItem?> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<ScheduledItem>> SaveAsync(ScheduledItem entity, CancellationToken cancellationToken = default)
    {
        var result = await _scheduledItemDataStore.SaveAsync(entity, cancellationToken);
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return result;
    }

    public async Task<List<ScheduledItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyAllByOwner(ownerEntraOid);
        if (_cache.TryGetValue(cacheKey, out List<ScheduledItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _scheduledItemDataStore.GetAllAsync(ownerEntraOid, cancellationToken);
        _cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<OperationResult<bool>> DeleteAsync(ScheduledItem entity, CancellationToken cancellationToken = default)
    {
        InvalidateUserCaches(entity.CreatedByEntraOid);
        return await _scheduledItemDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var entity = await _scheduledItemDataStore.GetAsync(primaryKey, cancellationToken);
        if (entity is not null)
        {
            InvalidateUserCaches(entity.CreatedByEntraOid);
        }
        return await _scheduledItemDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetScheduledItemsToSendAsync(CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsToSendAsync(cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetUnsentScheduledItemsAsync(cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetUnsentScheduledItemsAsync(ownerEntraOid, cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(year, month, cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(string ownerEntraOid, int year, int month, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(ownerEntraOid, year, month, cancellationToken);
    }

    public async Task<bool> SentScheduledItemAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await SentScheduledItemAsync(primaryKey, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.SentScheduledItemAsync(primaryKey, sentOn, cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetOrphanedScheduledItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _scheduledItemDataStore.GetOrphanedScheduledItemsAsync(cancellationToken);
        return items.ToList();
    }

    public async Task<List<ScheduledItem>> GetOrphanedScheduledItemsAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var items = await _scheduledItemDataStore.GetOrphanedScheduledItemsAsync(ownerEntraOid, cancellationToken);
        return items.ToList();
    }

    public async Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(ownerEntraOid, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetUnsentScheduledItemsAsync(page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetUnsentScheduledItemsAsync(ownerEntraOid, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsToSendAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsToSendAsync(page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(year, month, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(string ownerEntraOid, int year, int month, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(ownerEntraOid, year, month, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetOrphanedScheduledItemsAsync(page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetOrphanedScheduledItemsAsync(ownerEntraOid, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize, string sortBy = "sendondate", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    public async Task<PagedResult<ScheduledItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "sendondate", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(ownerEntraOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }

    private void InvalidateUserCaches(string? ownerEntraOid)
    {
        if (!string.IsNullOrEmpty(ownerEntraOid))
        {
            _cache.Remove(CacheKeyAllByOwner(ownerEntraOid));
        }
    }
}
