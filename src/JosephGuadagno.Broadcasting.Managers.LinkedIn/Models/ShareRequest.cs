using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class ShareRequest
{

    /// <summary>
    /// The author of a share contains Person URN of the Member creating the share.
    /// </summary>
    /// <remarks>Needs to be formatted as a Person URn: Example "urn:li:person:{0}"</remarks>
    [JsonPropertyName("author")]
    public string Author { get; set; }
    
    /// <summary>
    /// Defines the state of the share. For the purposes of creating a share, the lifecycleState will always be PUBLISHED.
    /// </summary>
    [JsonPropertyName("lifecycleState")]
    public string LifecycleState => "PUBLISHED";
    
    /// <summary>
    /// Provides additional options while defining the content of the share.
    /// </summary>
    [JsonPropertyName("specificContent")]
    public SpecificContent SpecificContent { get; set; }
    
    [JsonPropertyName("visibility")]
    public Visibility Visibility { get; set; }
}