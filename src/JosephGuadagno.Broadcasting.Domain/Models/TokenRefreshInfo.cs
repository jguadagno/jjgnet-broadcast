namespace JosephGuadagno.Broadcasting.Domain.Models;

public class TokenRefreshInfo: TokenRefreshBase
{
    public TokenRefreshInfo() {}
        
    public TokenRefreshInfo(string tokenName)
    {
        if (string.IsNullOrEmpty(tokenName))
        {
            throw new ArgumentNullException(nameof(tokenName), "The token name must be specified.");
        }

        RowKey = tokenName;
    }
    
    /// <summary>
    /// The date and time the token was last checked
    /// </summary>
    /// <remarks>The date and time should be in UTC</remarks>
    public DateTime LastChecked { get; set; }
    
    /// <summary>
    /// The date and time the token was last refreshed
    /// </summary>
    /// <remarks>The date and time should be in UTC</remarks>
    public DateTime LastRefreshed { get; set; }
        
    /// <summary>
    /// The date and time the token will expire
    /// </summary>
    /// <remarks>The date and time should be in UTC</remarks>
    public DateTime Expires { get; set; }   
}