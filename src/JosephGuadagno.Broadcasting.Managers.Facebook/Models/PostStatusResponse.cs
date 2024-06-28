using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Models;

class PostStatusResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("error")]
    public FacebookPostError? Error { get; set; }
}