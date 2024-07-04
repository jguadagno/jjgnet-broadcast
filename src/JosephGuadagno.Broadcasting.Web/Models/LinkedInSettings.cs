using JosephGuadagno.Broadcasting.Web.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// The LinkedIn settings for the application
/// </summary>
public class LinkedInSettings : ILinkedInSettings
{
    /// <summary>
    /// The Client Id
    /// </summary>
    public string ClientId { get; set; }
    
    /// <summary>
    /// The client secret
    /// </summary>
    public string ClientSecret { get; set; }
    
    /// <summary>
    /// The scopes requested
    /// </summary>
    public string Scopes { get; set; }
    
    /// <summary>
    /// The URL to get the authorization code
    /// </summary>
    public string AuthorizationUrl { get; set; }
    
    /// <summary>
    /// The URL to get the access token
    /// </summary>
    public string AccessTokenUrl { get; set; }
}
