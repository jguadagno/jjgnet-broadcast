using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IScheduledItemService
{
    Task<List<ScheduledItem>> GetScheduledItemsAsync(int? page = 1, int? pageSize = 25);
    Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId);
    Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem);
    Task<bool> DeleteScheduledItemAsync(int scheduledItemId);
    Task<List<ScheduledItem>> GetUnsentScheduledItemsAsync(int? page = 1, int? pageSize = 25);
    Task<List<ScheduledItem>> GetScheduledItemsToSendAsync(int? page = 1, int? pageSize = 25);
    Task<List<ScheduledItem>> GetScheduledItemsByCalendarMonthAsync(int year, int month, int? page = 1, int? pageSize = 25);
    Task<List<ScheduledItem>> GetOrphanedScheduledItemsAsync(int? page = 1, int? pageSize = 25);
}