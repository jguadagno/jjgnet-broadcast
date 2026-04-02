using System.Net.Mail;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Queues email messages onto the Azure Storage Queue for processing by the email Azure Function.
/// Does NOT call Azure Communication Services directly.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Queues an email using default from/reply-to addresses from settings.
    /// </summary>
    Task QueueEmail(MailAddress toAddress, string subject, string body);

    /// <summary>
    /// Queues an email with explicit from and reply-to addresses.
    /// </summary>
    Task QueueEmail(MailAddress toAddress, string subject, string body, MailAddress fromAddress, MailAddress replyToAddress);

    /// <summary>
    /// Compatibility signature for ASP.NET Core email abstractions. Not implementing ASP.NET Identity IEmailSender.
    /// </summary>
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
