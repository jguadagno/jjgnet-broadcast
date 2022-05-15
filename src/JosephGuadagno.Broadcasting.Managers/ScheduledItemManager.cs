using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

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

    public async Task<ScheduledItem> SaveAsync(ScheduledItem entity)
    {
        return await _scheduledItemRepository.SaveAsync(entity);
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

    public async Task<List<ScheduledItem>> GetScheduledItemsToSendAsync()
    {
        return await _scheduledItemRepository.GetScheduledItemsToSendAsync();
    }

    public async Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync()
    {
        return await _scheduledItemRepository.GetUnsentScheduledItemsAsync();
    }

    public async Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month)
    {
        return await _scheduledItemRepository.GetScheduledItemsByCalendarMonthAsync(year, month);
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