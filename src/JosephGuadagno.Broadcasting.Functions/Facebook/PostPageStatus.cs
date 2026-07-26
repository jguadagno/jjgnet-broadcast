using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class PostPageStatus(IFacebookManager facebookManager, IUserPlatformFacebookSettingsManager facebookSettingsManager, IUserOAuthTokenManager userOAuthTokenManager, ILogger<PostPageStatus> logger)
{
    [Function(ConfigurationFunctionNames.FacebookPostPageStatus)]
    public async Task Run(
        [QueueTrigger(Queues.FacebookPostStatusToPage)]
        SocialMediaPublishRequest request)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.FacebookPostPageStatus, startedAt);

        if (string.IsNullOrEmpty(request.OwnerEntraOid))
        {
            logger.LogWarning("Facebook post status missing OwnerEntraOid. Skipping");
            return;
        }

        var ownerOid = request.OwnerEntraOid;
        var settings = await facebookSettingsManager.GetAsync(ownerOid);
        if (settings is null || !settings.IsEnabled)
        {
            logger.LogWarning("Facebook settings not found or not enabled for owner '{OwnerOid}'. Skipping",
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var oauthToken = await userOAuthTokenManager.GetByUserAndPlatformAsync(ownerOid, SocialMediaPlatformIds.Facebook);
        if (oauthToken is null)
        {
            logger.LogWarning("No OAuth token found for owner '{OwnerOid}' on Facebook. Skipping",
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        if (string.IsNullOrEmpty(settings.PageId))
        {
            logger.LogWarning("Facebook PageId not found for owner '{OwnerOid}'. Skipping",
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        request.AccessToken = oauthToken.AccessToken;
        request.AuthorId = settings.PageId;

        try
        {
            var postId = await facebookManager.DispatchAsync(request);
            if (!string.IsNullOrEmpty(postId))
            {
                var properties = new Dictionary<string, string>
                {
                    {"statusText", request.Text}, 
                    {"url", request.LinkUrl ?? string.Empty},
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
