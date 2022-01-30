using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class ScheduledItemRepository: IScheduledItemRepository
{
       
    private readonly IScheduledItemDataStore _scheduledItemDataStore;

    public ScheduledItemRepository(IScheduledItemDataStore scheduledItemDataStore)
    {
        _scheduledItemDataStore = scheduledItemDataStore;
    }
        
    public async Task<ScheduledItem> GetAsync(int primaryKey)
    {
        return await _scheduledItemDataStore.GetAsync(primaryKey);
    }

    public async Task<bool> SaveAsync(ScheduledItem entity)
    {
        return await _scheduledItemDataStore.SaveAsync(entity);
    }

    public async Task<bool> SaveAllAsync(List<ScheduledItem> entities)
    {
        return await _scheduledItemDataStore.SaveAllAsync(entities);
    }

    public async Task<List<ScheduledItem>> GetAllAsync()
    {
        return await _scheduledItemDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(ScheduledItem entity)
    {
        return await _scheduledItemDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _scheduledItemDataStore.DeleteAsync(primaryKey);
    }

    public async Task<List<ScheduledItem>> GetUpcomingScheduledItemsAsync(DateTimeOffset lastChecked)
    {
        return await _scheduledItemDataStore.GetUpcomingScheduledItemsAsync(lastChecked);
    }

    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn)
    {
        return await _scheduledItemDataStore.SentScheduledItemAsync(primaryKey, sentOn);
    }
}