using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IScheduledItemDataStore : IDataRepository<ScheduledItem>
{
    public Task<List<ScheduledItem>> GetScheduledItemsToSendAsync();
    public Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync();
    public Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month);
    public Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn);
}