namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

/// <summary>
/// Represents an email message to be sent via Azure Communication Services.
/// </summary>
public class Email
{
    /// <summary>
    /// The email address of the sender.
    /// </summary>
    public required string FromMailAddress { get; set; }

    /// <summary>
    /// The display name of the sender.
    /// </summary>
    public string FromDisplayName { get; set; } = null!;

    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    public required string ToMailAddress { get; set; }

    /// <summary>
    /// The display name of the recipient.
    /// </summary>
    public string ToDisplayName { get; set; } = null!;

    /// <summary>
    /// The email address to reply to.
    /// </summary>
    public string ReplyToMailAddress { get; set; } = null!;

    /// <summary>
    /// The display name for the reply-to address.
    /// </summary>
    public string ReplyToDisplayName { get; set; } = null!;

    /// <summary>
    /// The subject of the email.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// The body of the email.
    /// </summary>
    public required string Body { get; set; }
}
