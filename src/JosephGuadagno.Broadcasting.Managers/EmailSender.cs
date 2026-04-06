using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Queues;

using JosephGuadagno.AzureHelpers.Storage;
using JosephGuadagno.AzureHelpers.Storage.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;

using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public partial class EmailSender(
    QueueServiceClient queueServiceClient,
    IEmailSettings settings,
    ILogger<EmailSender> logger) : IEmailSender
{
    protected virtual IQueue GetQueue()
    {
        return new Queue(queueServiceClient, Domain.Constants.Queues.SendEmail);
    }

    public async Task QueueEmail(MailAddress toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
        var replyToAddress = new MailAddress(settings.ReplyToAddress, settings.ReplyToDisplayName);
        await QueueEmail(toAddress, subject, body, fromAddress, replyToAddress, cancellationToken);
    }

    public async Task QueueEmail(MailAddress toAddress, string subject, string body,
        MailAddress fromAddress, MailAddress replyToAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentNullException.ThrowIfNull(fromAddress);
        ArgumentNullException.ThrowIfNull(replyToAddress);

        var email = new Email
        {
            ToMailAddress = toAddress.Address,
            ToDisplayName = toAddress.DisplayName,
            FromMailAddress = fromAddress.Address,
            FromDisplayName = fromAddress.DisplayName,
            ReplyToMailAddress = replyToAddress.Address,
            ReplyToDisplayName = replyToAddress.DisplayName,
            Subject = subject,
            Body = body
        };

        try
        {
            var emailQueue = GetQueue();
            await emailQueue.AddMessageAsync(email);
            LogEmailQueued(toAddress.Address, subject);
        }
        catch (Exception ex)
        {
            LogEmailQueueFailed(toAddress.Address, subject, ex);
            throw;
        }
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage, CancellationToken cancellationToken = default)
    {
        var toAddress = new MailAddress(email);
        await QueueEmail(toAddress, subject, htmlMessage, cancellationToken);
    }
}