using idunno.Bluesky;
using idunno.Bluesky.RichText;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class SendPost(IBlueskyManager blueskyManager,ILogger<SendPost> logger)
{
    [Function(ConfigurationFunctionNames.BlueskyPostMessage)]
    public async Task Run(
        [QueueTrigger(Queues.BlueskyPostToSend)]
        BlueskyPostMessage blueskyPostMessage)
    {
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
                
                // Get the OpenGraph info to embed; use explicit ImageUrl as thumbnail override when available
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
                // No ShortenedUrl, but Url + ImageUrl are both set — embed with custom thumbnail
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
            
            var response = await blueskyManager.Post(postBuilder);
            if (response is not null)
            {
                logger.LogDebug("Posting to bluesky: {Text}", postBuilder.Text);
                var properties = new Dictionary<string, string>
                {
                    {"message", postBuilder.Text?? string.Empty},
                    {"cid", response.Cid.ToString()}
                };
                logger.LogCustomEvent(Metrics.BlueskyPostSent, properties);
                return;
            }
            logger.LogError("Failed to post to Bluesky. Response was null");
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