using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class SendPost(IBlueskyManager blueskyManager, IUserPublisherBlueskySettingsManager blueskySettingsManager, ILogger<SendPost> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyPostMessage)]
    public async Task Run(
        [QueueTrigger(Queues.BlueskyPostToSend)]
        SocialMediaPublishRequest request)
    {
        if (string.IsNullOrEmpty(request.OwnerEntraOid))
        {
            logger.LogWarning("Bluesky post message missing OwnerEntraOid. Skipping.");
            return;
        }

        var ownerOid = request.OwnerEntraOid;
        var settings = await blueskySettingsManager.GetAsync(ownerOid);
        if (settings is null || !settings.IsEnabled || string.IsNullOrEmpty(settings.UserName))
        {
            logger.LogWarning("Bluesky settings not found or not enabled for owner '{OwnerOid}'. Skipping.",
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        var appPassword = await blueskySettingsManager.GetAppPasswordAsync(ownerOid);
        if (string.IsNullOrEmpty(appPassword))
        {
            logger.LogWarning("Bluesky app password not found for owner '{OwnerOid}'. Skipping.",
                LogSanitizer.Sanitize(ownerOid));
            return;
        }

        request.AuthorId = settings.UserName;
        request.AccessToken = appPassword;

        try
        {
            logger.LogDebug("Bluesky Post Received '{Text}'", request.Text);
            var cid = await blueskyManager.PublishAsync(request);
            if (cid is not null)
            {
                var properties = new Dictionary<string, string>
                {
                    {"message", request.Text},
                    {"cid", cid}
                };
                logger.LogCustomEvent(Metrics.BlueskyPostSent, properties);
            }
        }
        catch (BlueskyPostException ex)
        {
            logger.LogError(ex, "Bluesky API error posting message. Code: {ApiErrorCode}, Message: {ApiErrorMessage}",
                ex.ApiErrorCode, ex.ApiErrorMessage);
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to post to Bluesky. Exception Thrown: {Message}", e.Message);
            throw;
        }
    }
}