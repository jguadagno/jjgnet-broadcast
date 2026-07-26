using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostLink(ILinkedInManager linkedInManager, IUserOAuthTokenManager userOAuthTokenManager, IUserPlatformLinkedInSettingsManager linkedInSettingsManager, ILogger<PostLink> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInPostLink)]
    public async Task Run(
        [QueueTrigger(Queues.LinkedInPostLink)]
        SocialMediaPublishRequest request)
    {
        if (string.IsNullOrEmpty(request.OwnerEntraOid))
        {
            logger.LogWarning("LinkedIn post missing OwnerEntraOid. Skipping");
            return;
        }

        var settings = await linkedInSettingsManager.GetAsync(request.OwnerEntraOid);
        if (settings is null || !settings.IsEnabled)
        {
            logger.LogWarning("LinkedIn settings not found or not enabled for owner '{OwnerOid}'. Skipping",
                LogSanitizer.Sanitize(request.OwnerEntraOid));
            return;
        }

        var oauthToken = await userOAuthTokenManager.GetByUserAndPlatformAsync(
            request.OwnerEntraOid, SocialMediaPlatformIds.LinkedIn);
        if (oauthToken is null)
        {
            logger.LogWarning("No OAuth token found for owner '{OwnerOid}' on LinkedIn. Skipping",
                LogSanitizer.Sanitize(request.OwnerEntraOid));
            return;
        }

        request.AuthorId = settings.AuthorId;
        request.AccessToken = oauthToken.AccessToken;

        try
        {
            var linkedInShareId = await linkedInManager.DispatchAsync(request);
            if (!string.IsNullOrEmpty(linkedInShareId))
            {
                var properties = new Dictionary<string, string>
                {
                    {"linkedInShareId", linkedInShareId},
                    {"title", request.Title ?? string.Empty},
                    {"text", request.Text}, 
                    {"url", request.LinkUrl ?? string.Empty}
                };
                logger.LogCustomEvent(Metrics.LinkedInPostLink, properties);
            }
        }
        catch (LinkedInPostException ex)
        {
            logger.LogError(ex, "LinkedIn API error posting link. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post link to LinkedIn. Exception: {ExceptionMessage}", ex.Message);
            throw;
        }
    }
}
