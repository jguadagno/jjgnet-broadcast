using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostText(ILinkedInManager linkedInManager, ILogger<PostText> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInPostText)]
    public async Task Run(
        [QueueTrigger(Queues.LinkedInPostText)]
        LinkedInPostText linkedInPostText)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInPostText, startedAt);

        try
        {
            var linkedInShareId = await linkedInManager.PostShareText(linkedInPostText.AccessToken, linkedInPostText.AuthorId, linkedInPostText.Text);

            if (!string.IsNullOrEmpty(linkedInShareId))
            {
                var properties = new Dictionary<string, string>
                {
                    {"linkedInShareId", linkedInShareId},
                    {"text", linkedInPostText.Text}
                };
                logger.LogCustomEvent(Metrics.LinkedInPostText, properties);
            }
        }
        catch (LinkedInPostException ex)
        {
            logger.LogError(ex, "LinkedIn API error posting text. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to post LinkedIn text. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}