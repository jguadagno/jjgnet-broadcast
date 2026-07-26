namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// Represents the API request payload for saving LinkedIn platform settings.
/// </summary>
public class LinkedInApiRequest
{
    /// <summary>
    /// Gets or sets whether the platform is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn author identifier.
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn client identifier.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn client secret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn access token.
    /// </summary>
    public string? AccessToken { get; set; }
}
