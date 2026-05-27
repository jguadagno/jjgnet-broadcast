using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public interface IScheduledItemEventDispatcher
{
    Task DispatchAsync(ScheduledItem scheduledItem, CancellationToken cancellationToken = default);
}
