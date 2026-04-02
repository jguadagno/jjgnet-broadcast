using System.Text;
using System.Text.Json;
using Azure;
using Azure.Communication.Email;
using JosephGuadagno.Broadcasting.Domain.Constants;
using EmailMessage = Azure.Communication.Email.EmailMessage;
using EmailModel = JosephGuadagno.Broadcasting.Domain.Models.Messages.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Email;

public partial class SendEmail(EmailClient emailClient, ILogger<SendEmail> logger)
{
    [Function(ConfigurationFunctionNames.EmailSendEmail)]
    public async Task Run(
        [QueueTrigger(Queues.SendEmail)]
        string message,
        FunctionContext context)
    {
        var email = DeserializeMessage(message);
        if (email is null)
        {
            LogDeserializationFailed(message[..Math.Min(100, message.Length)]);
            return;
        }

        if (string.IsNullOrWhiteSpace(email.FromMailAddress) ||
            string.IsNullOrWhiteSpace(email.ToMailAddress) ||
            string.IsNullOrWhiteSpace(email.Subject))
        {
            LogRequiredFieldsMissing(
                email.FromMailAddress ?? "(empty)",
                email.ToMailAddress ?? "(empty)",
                email.Subject ?? "(empty)");
            return;
        }

        try
        {
            var emailContent = new EmailContent(email.Subject)
            {
                Html = email.Body
            };
            var emailMessage = new EmailMessage(email.FromMailAddress, email.ToMailAddress, emailContent);

            if (!string.IsNullOrWhiteSpace(email.ReplyToMailAddress))
            {
                emailMessage.ReplyTo.Add(new EmailAddress(email.ReplyToMailAddress, email.ReplyToDisplayName));
            }

            var operation = await emailClient.SendAsync(WaitUntil.Started, emailMessage);
            LogEmailSent(email.ToMailAddress, email.Subject, operation.Id);
        }
        catch (Exception ex)
        {
            LogEmailSendFailed(email.ToMailAddress, email.Subject, ex);
            throw;
        }
    }

    private static EmailModel? DeserializeMessage(string message)
    {
        // Try Base64-encoded JSON first (storage format used by EmailSender)
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(message));
            return JsonSerializer.Deserialize<EmailModel>(json);
        }
        catch
        {
            // Fall back to raw JSON
            try
            {
                return JsonSerializer.Deserialize<EmailModel>(message);
            }
            catch
            {
                return null;
            }
        }
    }
}
