using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class EventPublisherSettings: IEventPublisherSettings
{
    public required List<ITopicEndpointSettings> TopicEndpointSettings { get; set; } = [];
}