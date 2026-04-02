using JosephGuadagno.Broadcasting.Domain.Constants;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Email;

/// <summary>
/// Handles messages that have been moved to the send-email-poison queue after exceeding the maximum retry count.
/// Logs the failed message and suppresses further retries.
/// </summary>
public class SendEmailPoison(ILogger<SendEmailPoison> logger)
{
    [Function(ConfigurationFunctionNames.EmailSendEmailPoison)]
    public Task Run(
        [QueueTrigger(Queues.SendEmailPoison)]
        string message,
        FunctionContext context)
    {
        logger.LogError(
            "SendEmail poison message received. This message failed after maximum retries and will not be retried. Message: '{Message}'",
            message[..Math.Min(500, message.Length)]);

        return Task.CompletedTask;
    }
}
