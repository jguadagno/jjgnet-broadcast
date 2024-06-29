using System.Text.Json.Serialization;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Models;

/// <summary>
/// The response from Facebook when requesting a token
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// The access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    /// <summary>
    /// The token type
    /// </summary> 
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
    
    /// <summary>
    /// The number of seconds the token is valid for
    /// </summary>
    /// <remarks>Typically 60 days but can be revoked at any time</remarks>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}