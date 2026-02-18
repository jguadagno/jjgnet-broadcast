namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// The Topic Endpoint Settings
/// </summary>
public interface ITopicEndpointSettings
{
    /// <summary>
    /// The Topic Name
    /// </summary>
    public string TopicName { get; set; }

    /// <summary>
    /// The Endpoint Url for the Topic
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// The Key for the Topic
    /// </summary>
    public string Key { get; set; }
}