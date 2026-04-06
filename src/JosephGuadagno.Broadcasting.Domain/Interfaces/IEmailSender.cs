using System.Net.Mail;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Queues email messages onto the Azure Storage Queue for processing by the email Azure Function.
/// Does NOT call Azure Communication Services directly.
/// </summary>
public interface IEmailSender
{
    Task QueueEmail(MailAddress toAddress, string subject, string body, CancellationToken cancellationToken = default);
    Task QueueEmail(MailAddress toAddress, string subject, string body, MailAddress fromAddress, MailAddress replyToAddress, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string email, string subject, string htmlMessage, CancellationToken cancellationToken = default);
}
