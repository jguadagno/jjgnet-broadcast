using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using JosephGuadagno.AzureHelpers.Storage;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public partial class EmailSender(
    QueueServiceClient queueServiceClient,
    IEmailSettings settings,
    ILogger<EmailSender> logger) : IEmailSender
{
    private readonly Queue _queue = new(queueServiceClient, JosephGuadagno.Broadcasting.Domain.Constants.Queues.SendEmail);

    public async Task QueueEmail(MailAddress toAddress, string subject, string body)
    {
        ArgumentNullException.ThrowIfNull(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var fromAddress = new MailAddress(settings.FromAddress, settings.FromDisplayName);
        var replyToAddress = new MailAddress(settings.ReplyToAddress, settings.ReplyToDisplayName);
        await QueueEmail(toAddress, subject, body, fromAddress, replyToAddress);
    }

    public async Task QueueEmail(MailAddress toAddress, string subject, string body,
        MailAddress fromAddress, MailAddress replyToAddress)
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
            await _queue.AddMessageWithBase64EncodingAsync(email);
            LogEmailQueued(toAddress.Address, subject);
        }
        catch (Exception ex)
        {
            LogEmailQueueFailed(toAddress.Address, subject, ex);
            throw;
        }
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var toAddress = new MailAddress(email);
        await QueueEmail(toAddress, subject, htmlMessage);
    }
}
