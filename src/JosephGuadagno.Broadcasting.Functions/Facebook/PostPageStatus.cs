using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class PostPageStatus(IFacebookManager facebookManager, ILogger<PostPageStatus> logger)
{
    [Function(ConfigurationFunctionNames.FacebookPostPageStatus)]
    public async Task Run(
        [QueueTrigger(Queues.FacebookPostStatusToPage)]
        Domain.Models.Messages.FacebookPostStatus facebookPostStatus)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookPostPageStatus, startedAt);

        try
        {
            var pageId = await facebookManager.PostMessageAndLinkToPage(facebookPostStatus.StatusText, facebookPostStatus.LinkUri);

            if (!string.IsNullOrEmpty(pageId))
            {
                var properties = new Dictionary<string, string>
                {
                    {"statusText", facebookPostStatus.StatusText}, 
                    {"url", facebookPostStatus.LinkUri},
                };
                logger.LogCustomEvent(Metrics.FacebookPostPageStatus, properties);
            }
        }
        catch (FacebookPostException ex)
        {
            logger.LogError(ex, "Facebook API error posting status. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to post status. Exception: {ExceptionMessage}", e.Message);
            throw;
        }
    }
}