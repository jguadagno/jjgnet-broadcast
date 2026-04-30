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
    public required string ClientId { get; set; }
    
    /// <summary>
    /// The client secret
    /// </summary>
    public required string ClientSecret { get; set; }
    
    /// <summary>
    /// The scopes requested
    /// </summary>
    public required string Scopes { get; set; }
    
    /// <summary>
    /// The URL to get the authorization code
    /// </summary>
    public required string AuthorizationUrl { get; set; }
    
    /// <summary>
    /// The URL to get the access token
    /// </summary>
    public required string AccessTokenUrl { get; set; }
}
