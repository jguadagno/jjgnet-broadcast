using idunno.Bluesky;
using idunno.Bluesky.RichText;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class SendPost(IBlueskyManager blueskyManager, IUserPublisherBlueskySettingsManager blueskySettingsManager, ILogger<SendPost> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyPostMessage)]
    public async Task Run(
        [QueueTrigger(Queues.BlueskyPostToSend)]
        BlueskyPostMessage blueskyPostMessage)
    {
        if (string.IsNullOrEmpty(blueskyPostMessage.CreatedByEntraOid))
        {
            logger.LogWarning("Bluesky post message missing CreatedByEntraOid. Skipping.");
            return;
        }

        var ownerOid = blueskyPostMessage.CreatedByEntraOid;
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

        try
        {
            logger.LogDebug("Bluesky Post Received '{Text}'", blueskyPostMessage.Text);
            var postBuilder = new PostBuilder(blueskyPostMessage.Text);
            
            if (!string.IsNullOrWhiteSpace(blueskyPostMessage.ShortenedUrl) && !string.IsNullOrWhiteSpace(blueskyPostMessage.Url))
            {
                if (!blueskyPostMessage.Text.EndsWith(' '))
                {
                    postBuilder.Append(" ");
                }
                postBuilder.Append(new Link(blueskyPostMessage.ShortenedUrl, blueskyPostMessage.ShortenedUrl));
                
                var embeddedExternalRecord = !string.IsNullOrEmpty(blueskyPostMessage.ImageUrl)
                    ? await blueskyManager.GetEmbeddedExternalRecordWithThumbnail(blueskyPostMessage.Url, blueskyPostMessage.ImageUrl)
                    : await blueskyManager.GetEmbeddedExternalRecord(blueskyPostMessage.Url);
                if (embeddedExternalRecord != null)
                {
                    postBuilder.EmbedRecord(embeddedExternalRecord);
                }
            }
            else if (!string.IsNullOrEmpty(blueskyPostMessage.ImageUrl) && !string.IsNullOrEmpty(blueskyPostMessage.Url))
            {
                var embeddedExternalRecord = await blueskyManager.GetEmbeddedExternalRecordWithThumbnail(
                    blueskyPostMessage.Url, blueskyPostMessage.ImageUrl);
                if (embeddedExternalRecord != null)
                {
                    postBuilder.EmbedRecord(embeddedExternalRecord);
                }
            }

            if (blueskyPostMessage.Hashtags is not null && blueskyPostMessage.Hashtags.Count > 0)
            {
                foreach (var hashtag in blueskyPostMessage.Hashtags)
                {
                    postBuilder.Append(" ");
                    postBuilder.Append(new HashTag(hashtag));
                }
            }

            var agent = new BlueskyAgent();
            var loginResult = await agent.Login(settings.UserName, appPassword);
            if (!loginResult.Succeeded)
            {
                logger.LogError("Failed to log in to Bluesky for owner '{OwnerOid}'. StatusCode: {StatusCode}",
                    LogSanitizer.Sanitize(blueskyPostMessage.CreatedByEntraOid), loginResult.StatusCode);
                return;
            }

            var response = await agent.Post(postBuilder);
            if (response.Succeeded && response.Result is not null)
            {
                logger.LogDebug("Posting to bluesky: {Text}", postBuilder.Text);
                var properties = new Dictionary<string, string>
                {
                    {"message", postBuilder.Text ?? string.Empty},
                    {"cid", response.Result.Cid.ToString()}
                };
                logger.LogCustomEvent(Metrics.BlueskyPostSent, properties);
                return;
            }
            logger.LogError("Failed to post to Bluesky. StatusCode: {StatusCode}", response.StatusCode);
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