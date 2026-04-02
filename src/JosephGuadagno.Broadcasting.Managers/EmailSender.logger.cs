using System;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public partial class EmailSender
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "Email queued successfully to '{ToAddress}' with subject '{Subject}'")]
    private partial void LogEmailQueued(string toAddress, string subject);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Error,
        Message = "Failed to queue email to '{ToAddress}' with subject '{Subject}'")]
    private partial void LogEmailQueueFailed(string toAddress, string subject, Exception ex);
}
