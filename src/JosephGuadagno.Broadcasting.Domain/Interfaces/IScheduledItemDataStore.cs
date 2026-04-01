using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IScheduledItemDataStore : IDataRepository<ScheduledItem>
{
    public Task<List<ScheduledItem>> GetScheduledItemsToSendAsync();
    public Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync();
    public Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month);
    public Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn);
    Task<IEnumerable<Domain.Models.ScheduledItem>> GetOrphanedScheduledItemsAsync();
    
    Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize);
    Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(int page, int pageSize);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsToSendAsync(int page, int pageSize);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int page, int pageSize);
    Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(int page, int pageSize);
}