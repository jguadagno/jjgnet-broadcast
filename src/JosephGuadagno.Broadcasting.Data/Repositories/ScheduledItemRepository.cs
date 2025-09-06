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

    public async Task<ScheduledItem> SaveAsync(ScheduledItem entity)
    {
        return await _scheduledItemDataStore.SaveAsync(entity);
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

    public async Task<List<ScheduledItem>> GetScheduledItemsToSendAsync()
    {
        return await _scheduledItemDataStore.GetScheduledItemsToSendAsync();
    }

    public async Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync()
    {
        return await _scheduledItemDataStore.GetUnsentScheduledItemsAsync();
    }

    public async Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month)
    {
        return await _scheduledItemDataStore.GetScheduledItemsByCalendarMonthAsync(year, month);
    }

    public async Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn)
    {
        return await _scheduledItemDataStore.SentScheduledItemAsync(primaryKey, sentOn);
    }
}