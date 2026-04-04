using JosephGuadagno.Broadcasting.Web.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// The application settings for the Web application
/// </summary>
public class Settings : ISettings
{
    /// <summary>
    /// The root URL for serving static content in the Web application.
    /// </summary>
    public required string StaticContentRootUrl { get; set; }
}