using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Email;

public partial class SendEmail
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information,
        Message = "Email sent successfully to '{ToAddress}' with subject '{Subject}'. Operation ID: '{OperationId}'")]
    private partial void LogEmailSent(string toAddress, string subject, string operationId);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Error,
        Message = "Failed to send email to '{ToAddress}' with subject '{Subject}'")]
    private partial void LogEmailSendFailed(string toAddress, string subject, Exception ex);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Error,
        Message = "Failed to deserialize email queue message. Message preview: '{MessagePreview}'")]
    private partial void LogDeserializationFailed(string messagePreview);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Error,
        Message = "Email queue message is missing required fields. From: '{From}', To: '{To}', Subject: '{Subject}'")]
    private partial void LogRequiredFieldsMissing(string from, string to, string subject);
}
