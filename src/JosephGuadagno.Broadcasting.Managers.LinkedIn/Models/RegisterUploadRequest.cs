using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class RegisterUploadRequest  
{
    [JsonPropertyName("recipes")]
    public string[] Recipes => new [] { "urn:li:digitalmediaRecipe:feedshare-image" };
    
    /// <summary>
    /// The owner of the media. This can be the member who is sharing the media, or the organization that owns the media.
    /// </summary>
    /// <remarks>This should be in the Person Urn format. Example: urn:li:person:8675309</remarks>
    [JsonPropertyName("owner")]
    public string Owner { get; set; }
    
    [JsonPropertyName("serviceRelationships")]
    public ServiceRelationships[] ServiceRelationships { get; set; }
}