namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEventPublisherSettings
{
    public List<ITopicEndpointSettings> TopicEndpointSettings { get; set; }
}