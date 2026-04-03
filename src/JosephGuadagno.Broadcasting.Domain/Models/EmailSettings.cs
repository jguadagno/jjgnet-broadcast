using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class EmailSettings: IEmailSettings
{
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