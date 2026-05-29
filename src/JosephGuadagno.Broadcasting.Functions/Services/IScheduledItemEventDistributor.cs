using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public interface IScheduledItemEventDistributor
{
    Task DispatchAsync(ScheduledItem scheduledItem, CancellationToken cancellationToken = default);
}
