using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class UploadRegistrationRequest
{
    [JsonPropertyName("registerUploadRequest")]
    public RegisterUploadRequest RegisterUploadRequest { get; set; }
}