using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Functions.Facebook.Models;

public class PostStatusResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("error")]
    public Error Error { get; set; }
}
    
public class Error
{
    [JsonPropertyName("message")]
    public string Message { get; set; } 
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("error_subcode")]
    public int ErrorSubcode { get; set; }
    [JsonPropertyName("fbtrace_id")]
    public string FacebookTraceId { get; set; }
}