using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Web.Models.LinkedIn;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; set; }
    [JsonPropertyName("scope")]
    public string Scope { get; set; }
}