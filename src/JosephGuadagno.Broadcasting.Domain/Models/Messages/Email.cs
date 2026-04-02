namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

/// <summary>
/// Represents an email message to be sent via Azure Communication Services.
/// </summary>
public class Email
{
    /// <summary>
    /// The email address of the sender.
    /// </summary>
    public string FromMailAddress { get; set; }

    /// <summary>
    /// The display name of the sender.
    /// </summary>
    public string FromDisplayName { get; set; }

    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    public string ToMailAddress { get; set; }

    /// <summary>
    /// The display name of the recipient.
    /// </summary>
    public string ToDisplayName { get; set; }

    /// <summary>
    /// The email address to reply to.
    /// </summary>
    public string ReplyToMailAddress { get; set; }

    /// <summary>
    /// The display name for the reply-to address.
    /// </summary>
    public string ReplyToDisplayName { get; set; }

    /// <summary>
    /// The subject of the email.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// The body of the email.
    /// </summary>
    public string Body { get; set; }
}
