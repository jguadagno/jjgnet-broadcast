using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

/// <summary>
/// The media to share on LinkedIn
/// </summary>
public class Media
{
    /// <summary>
    /// Must be configured to "READY" to be shared.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status => "READY";
    
    /// <summary>
    /// Provide a short description for your image or article.
    /// </summary>
    [JsonPropertyName("description")]
    public TextProperties Description { get; set; }
    
    /// <summary>
    /// ID of the uploaded image asset. If you are uploading an article, this field is not required
    /// </summary>
    /// <remarks>Must be in a URN format. Example: urn:li:digitalmediaAsset:D5622AQHqpGB5YNqcvg</remarks>
    [JsonPropertyName("media")]
    public string MediaUrn { get; set; }
    
    /// <summary>
    /// Provide the URL of the article you would like to share here
    /// </summary>
    [JsonPropertyName("originalUrl")]
    public string OriginalUrl { get; set; }
    
    /// <summary>
    /// Customize the title of your image or article
    /// </summary>
    [JsonPropertyName("title")]
    public TextProperties Title { get; set; }
}