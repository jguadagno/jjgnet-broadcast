using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class ScheduledItemManager: IScheduledItemManager
{
    private readonly IScheduledItemDataStore _scheduledItemDataStore;

    public ScheduledItemManager(IScheduledItemDataStore scheduledItemDataStore)
    {
        _scheduledItemDataStore = scheduledItemDataStore;
    }
    
    public async Task<ScheduledItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<ScheduledItem> SaveAsync(ScheduledItem entity, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.SaveAsync(entity, cancellationToken);
    }

    public async Task<List<ScheduledItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(ScheduledItem entity, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
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

    public async Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(year, month, cancellationToken);
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
    
    public async Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetAllAsync(page, pageSize, cancellationToken);
    }
    
    public async Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetUnsentScheduledItemsAsync(page, pageSize, cancellationToken);
    }
    
    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsToSendAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsToSendAsync(page, pageSize, cancellationToken);
    }
    
    public async Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(year, month, page, pageSize, cancellationToken);
    }
    
    public async Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _scheduledItemDataStore.GetOrphanedScheduledItemsAsync(page, pageSize, cancellationToken);
    }
}