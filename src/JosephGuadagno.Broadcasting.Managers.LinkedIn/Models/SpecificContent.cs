using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class SpecificContent
{
    [JsonPropertyName("com.linkedin.ugc.ShareContent")]
    public ShareContent ShareContent { get; set; }
}