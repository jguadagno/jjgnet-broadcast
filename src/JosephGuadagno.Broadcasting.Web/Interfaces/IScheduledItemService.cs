using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IScheduledItemService
{
    Task<List<ScheduledItem>?> GetScheduledItemsAsync();
    Task<ScheduledItem?> GetScheduledItemAsync(int scheduledItemId);
    Task<ScheduledItem?> SaveScheduledItemAsync(ScheduledItem scheduledItem);
    Task<bool> DeleteScheduledItemAsync(int scheduledItemId);
    Task<List<ScheduledItem>?> GetUpcomingScheduledItems();
}
