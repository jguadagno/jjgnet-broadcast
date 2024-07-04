namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ILinkedInSettings
{
    /// <summary>
    /// The Client Id
    /// </summary>
    string ClientId { get; set; }

    /// <summary>
    /// The client secret
    /// </summary>
    string ClientSecret { get; set; }

    /// <summary>
    /// The scopes requested
    /// </summary>
    string Scopes { get; set; }

    /// <summary>
    /// The URL to get the authorization code
    /// </summary>
    string AuthorizationUrl { get; set; }

    /// <summary>
    /// The URL to get the access token
    /// </summary>
    string AccessTokenUrl { get; set; }
}