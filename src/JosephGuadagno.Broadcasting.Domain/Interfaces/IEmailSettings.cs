namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Configuration settings for the email sender backed by Azure Communication Services.
/// </summary>
public interface IEmailSettings
{
    /// <summary>
    /// The default sender email address.
    /// </summary>
    string FromAddress { get; set; }

    /// <summary>
    /// The default sender display name.
    /// </summary>
    string FromDisplayName { get; set; }

    /// <summary>
    /// The default reply-to email address.
    /// </summary>
    string ReplyToAddress { get; set; }

    /// <summary>
    /// The default reply-to display name.
    /// </summary>
    string ReplyToDisplayName { get; set; }

    /// <summary>
    /// The Azure Communication Services connection string.
    /// </summary>
    string AzureCommunicationsConnectionString { get; set; }
}