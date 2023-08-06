using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.Models.Facebook;

class FacebookPostError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } 
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("error_subcode")]
    public int SubCode { get; set; }
    
    [JsonPropertyName("fbtrace_id")]
    public string FacebookTraceId { get; set; }
}