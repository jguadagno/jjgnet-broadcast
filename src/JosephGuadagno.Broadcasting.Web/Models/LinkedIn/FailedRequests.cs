using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Web.Models.LinkedIn;

public class FailedRequests
{
    [JsonPropertyName("error")]
    public required string Error { get; set; }
    
    [JsonPropertyName("error_description")]
    public required string ErrorDescription { get; set; }
    
    [JsonPropertyName("state")]
    public required string State { get; set; }
}