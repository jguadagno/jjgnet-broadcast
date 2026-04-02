using JosephGuadagno.Broadcasting.Web.Interfaces;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// The application settings for the Web application
/// </summary>
public class Settings : ISettings
{
    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public required string LoggingStorageAccount { get; set; }

    /// <summary>
    /// The root URL for serving static content in the Web application.
    /// </summary>
    public required string StaticContentRootUrl { get; set; }

    /// <summary>
    /// The default sender email address.
    /// </summary>
    public required string FromAddress { get; set; }

    /// <summary>
    /// The default sender display name.
    /// </summary>
    public required string FromDisplayName { get; set; }

    /// <summary>
    /// The default reply-to email address.
    /// </summary>
    public required string ReplyToAddress { get; set; }

    /// <summary>
    /// The default reply-to display name.
    /// </summary>
    public required string ReplyToDisplayName { get; set; }

    /// <summary>
    /// The Azure Communication Services connection string.
    /// </summary>
    public required string AzureCommunicationsConnectionString { get; set; }
}