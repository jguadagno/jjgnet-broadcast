using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class Value
{
    [JsonPropertyName("uploadMechanism")]
    public UploadMechanism UploadMechanism { get; set; }
    
    [JsonPropertyName("mediaArtifact")]
    public string MediaArtifact { get; set; }
    
    /// <summary>
    /// The identifier for the media. This is used when creating a share.
    /// </summary>
    /// <remarks>You will use this value for the corresponding Share</remarks>
    [JsonPropertyName("asset")]
    public string Asset { get; set; }
    
    [JsonPropertyName("assetRealTimeTopic")]
    public string AssetRealTimeTopic { get; set; }
}