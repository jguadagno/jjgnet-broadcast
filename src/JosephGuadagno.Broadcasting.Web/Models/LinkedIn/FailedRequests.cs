using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Web.Models.LinkedIn;

public class FailedRequests
{
    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; }
    
    [JsonPropertyName("state")]
    public string State { get; set; }
}