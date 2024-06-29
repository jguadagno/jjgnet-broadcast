namespace JosephGuadagno.Broadcasting.Managers.Facebook.Models;

/// <summary>
/// Token information provided by Facebook on a refresh request
/// </summary>
public class TokenInfo
{
    /// <summary>
    /// The new access token
    /// </summary>
    public string AccessToken { get; set; }
    
    /// <summary>
    /// The token type
    /// </summary>
    public string TokenType { get; set; }
    
    /// <summary>
    /// Indicates when the token expires
    /// </summary>
    /// <remarks>Typically 60 days but can be revoked at any time</remarks>
    public DateTime ExpiresOn { get; set; }
}