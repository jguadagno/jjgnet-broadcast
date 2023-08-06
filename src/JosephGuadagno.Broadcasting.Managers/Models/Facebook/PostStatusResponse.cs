using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.Models.Facebook;

class PostStatusResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("error")]
    public FacebookPostError? Error { get; set; }
}