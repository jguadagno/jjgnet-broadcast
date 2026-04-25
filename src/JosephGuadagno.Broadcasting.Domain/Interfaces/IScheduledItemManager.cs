using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IScheduledItemManager : IManager<ScheduledItem>
{
    public Task<List<ScheduledItem>> GetScheduledItemsToSendAsync(CancellationToken cancellationToken = default);
    public Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync(CancellationToken cancellationToken = default);
    public Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    public Task<bool> SentScheduledItemAsync(int primaryKey, CancellationToken cancellationToken = default);
    public Task<bool> SentScheduledItemAsync(int primaryKey, DateTimeOffset sentOn, CancellationToken cancellationToken = default);
    public Task<List<ScheduledItem>> GetOrphanedScheduledItemsAsync(CancellationToken cancellationToken = default);
    
    Task<List<ScheduledItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(string ownerEntraOid, int year, int month, CancellationToken cancellationToken = default);
    Task<List<ScheduledItem>> GetOrphanedScheduledItemsAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsToSendAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<ScheduledItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(string ownerEntraOid, int year, int month, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<ScheduledItem>> GetAllAsync(int page, int pageSize, string sortBy = "sendondate", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<PagedResult<ScheduledItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "sendondate", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}
