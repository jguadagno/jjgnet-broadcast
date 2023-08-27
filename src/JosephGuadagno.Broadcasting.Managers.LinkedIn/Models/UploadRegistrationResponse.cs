using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class UploadRegistrationResponse
{
    [JsonPropertyName("value")]
    public Value Value { get; set; }
}