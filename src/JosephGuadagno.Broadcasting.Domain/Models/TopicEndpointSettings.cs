using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// The Topic Endpoint Settings
/// </summary>
public class TopicEndpointSettings : ITopicEndpointSettings
{
    /// <summary>
    /// The Topic Name
    /// </summary>
    public required string TopicName { get; set; }

    /// <summary>
    /// The Endpoint Url for the Topic
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The Key for the Topic
    /// </summary>
    public required string Key { get; set; }
}