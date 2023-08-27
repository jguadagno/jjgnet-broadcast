using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class ProfilePicture
{
    [JsonPropertyName("displayImage")]
    public string DisplayImage { get; set; }
}