using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class PostPageStatus(IFacebookManager facebookManager, IUserPublisherSettingManager userPublisherSettingManager, ILogger<PostPageStatus> logger)
{
    [Function(ConfigurationFunctionNames.FacebookPostPageStatus)]
    public async Task Run(
        [QueueTrigger(Queues.FacebookPostStatusToPage)]
        Domain.Models.Messages.FacebookPostStatus facebookPostStatus)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookPostPageStatus, startedAt);

        if (string.IsNullOrEmpty(facebookPostStatus.CreatedByEntraOid))
        {
            logger.LogWarning("Facebook post status missing CreatedByEntraOid. Skipping.");
            return;
        }

        var credentials = await userPublisherSettingManager.GetCredentialsAsync(
            facebookPostStatus.CreatedByEntraOid,
            SocialMediaPlatformIds.Facebook);

        var pageAccessToken = credentials.GetValueOrDefault("PageAccessToken")
            ?? credentials.GetValueOrDefault("LongLivedAccessToken");

        if (string.IsNullOrEmpty(pageAccessToken))
        {
            logger.LogWarning("Facebook credentials (PageAccessToken) not found for owner '{OwnerOid}'. Skipping.",
                LogSanitizer.Sanitize(facebookPostStatus.CreatedByEntraOid));
            return;
        }

        if (!credentials.TryGetValue("PageId", out var pageId) || string.IsNullOrEmpty(pageId))
        {
            logger.LogWarning("Facebook PageId not found for owner '{OwnerOid}'. Skipping.",
                LogSanitizer.Sanitize(facebookPostStatus.CreatedByEntraOid));
            return;
        }

        try
        {
            string? postId;
            if (!string.IsNullOrEmpty(facebookPostStatus.ImageUrl))
                postId = await facebookManager.PostMessageLinkAndPictureToPage(
                    facebookPostStatus.StatusText, facebookPostStatus.LinkUri, facebookPostStatus.ImageUrl, pageId, pageAccessToken);
            else
                postId = await facebookManager.PostMessageAndLinkToPage(
                    facebookPostStatus.StatusText, facebookPostStatus.LinkUri, pageId, pageAccessToken);

            if (!string.IsNullOrEmpty(postId))
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