using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

// TODO: Add AzureFunction to process ScheduledItems

public class ScheduledItemManager: IScheduledItemManager
{
    private readonly IScheduledItemRepository _scheduledItemRepository;

    public ScheduledItemManager(IScheduledItemRepository scheduledItemRepository)
    {
        _scheduledItemRepository = scheduledItemRepository;
    }
    
    public async Task<ScheduledItem> GetAsync(int primaryKey)
    {
        return await _scheduledItemRepository.GetAsync(primaryKey);
    }

    public async Task<bool> SaveAsync(ScheduledItem entity)
    {
        return await _scheduledItemRepository.SaveAsync(entity);
    }

    public async Task<bool> SaveAllAsync(List<ScheduledItem> entities)
    {
        return await _scheduledItemRepository.SaveAllAsync(entities);
    }

    public async Task<List<ScheduledItem>> GetAllAsync()
    {
        return await _scheduledItemRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(ScheduledItem entity)
    {
        return await _scheduledItemRepository.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _scheduledItemRepository.DeleteAsync(primaryKey);
    }

    public async Task<List<ScheduledItem>> GetUpcomingScheduledItemsAsync(DateTimeOffset lastChecked)
    {
        return await _scheduledItemRepository.GetUpcomingScheduledItemsAsync(lastChecked);
    }

    public async Task<bool> SentScheduledItemAsync(int primaryKey)
    {
        return await SentScheduledItemAsync(primaryKey, DateTimeOffset.UtcNow);
    }
    
    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn)
    {
        return await _scheduledItemRepository.SentScheduledItemAsync(primaryKey, sentOn);
    }
}