using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IScheduledItemService
{
    Task<List<ScheduledItem>> GetScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId);
    Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem);
    Task<bool> DeleteScheduledItemAsync(int scheduledItemId);
    Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<List<ScheduledItem>> GetScheduledItemsToSendAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<List<ScheduledItem>> GetOrphanedScheduledItemsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
}