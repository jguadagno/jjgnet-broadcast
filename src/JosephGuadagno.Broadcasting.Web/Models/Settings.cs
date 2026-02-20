using JosephGuadagno.Broadcasting.Web.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// The application settings for the Web application
/// </summary>
public class Settings : ISettings
{
    /// <summary>
    /// The root Uri for the JosephGuadagno.NET Broadcasting Api
    /// </summary>
    public required string ApiRootUrl { get; set; }

    /// <summary>
    /// The Uri to get a list of scopes for Api permissions
    /// </summary>
    public required string ApiScopeUrl { get; set; }

    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public required string LoggingStorageAccount { get; set; }

    /// <summary>
    /// The root URL for serving static content in the Web application.
    /// </summary>
    public required string StaticContentRootUrl { get; set; }

}