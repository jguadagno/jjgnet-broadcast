using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

public class Settings : ISettings
{
    /// <summary>
    /// The URL of the API scope used for authentication and authorization purposes.
    /// </summary>
    public required string ApiScopeUrl { get; set; }

    /// <summary>
    /// The Client Id for the Azure AD Scalar App.
    /// </summary>
    public required string ScalarClientId { get; set; }

    /// <summary>
    /// The storage account connection string used for logging.
    /// </summary>
    public required string LoggingStorageAccount { get; set; }

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