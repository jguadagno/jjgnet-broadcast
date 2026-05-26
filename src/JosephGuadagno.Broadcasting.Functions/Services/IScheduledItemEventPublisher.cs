using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Functions.Services;

public interface IScheduledItemEventPublisher
{
    Task PublishAsync(ScheduledItem scheduledItem, CancellationToken cancellationToken = default);
}
