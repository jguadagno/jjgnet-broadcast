using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IScheduledItemService
{
    Task<PagedResult<ScheduledItem>> GetScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId);
    Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem);
    Task<bool> DeleteScheduledItemAsync(int scheduledItemId);
    Task<PagedResult<ScheduledItem>> GetUnsentScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsToSendAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<PagedResult<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<PagedResult<ScheduledItem>> GetOrphanedScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
}