using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using idunno.Bluesky;
using idunno.Bluesky.RichText;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class SendPost(IBlueskyManager blueskyManager, TelemetryClient telemetryClient, ILogger<SendPost> logger)
{
    [Function(Constants.ConfigurationFunctionNames.BlueskyPostMessage)]
    public async Task Run(
        [QueueTrigger(Constants.Queues.BlueskyPostToSend)]
        BlueskyPostMessage blueskyPostMessage)
    {
        if (blueskyPostMessage is null)
        {
            logger.LogInformation("BlueskyPostMessage is null");
            return;
        }
        try
        {
            logger.LogDebug("Bluesky Post Received '{Text}'", blueskyPostMessage.Text);
            var postBuilder = new PostBuilder(blueskyPostMessage.Text);
            if (blueskyPostMessage.Url is not null)
            {
                postBuilder.Append(new Link(blueskyPostMessage.Url, blueskyPostMessage.Url));
            }

            if (blueskyPostMessage.Hashtags is not null && blueskyPostMessage.Hashtags.Count > 0)
            {
                foreach (var hashtag in blueskyPostMessage.Hashtags)
                {
                    postBuilder.Append(new HashTag(hashtag));
                }
            }
            
            var response = await blueskyManager.Post(postBuilder);
            if (response is not null)
            {
                logger.LogDebug("Posting to bluesky: {Text}", postBuilder.Text);
                telemetryClient.TrackEvent(Constants.Metrics.BlueskyPostSent, new Dictionary<string, string>
                {
                    {"message", postBuilder.Text},
                    {"cid", response.Cid.ToString()}
                });
                return;
            }
            logger.LogError("Failed to post to Bluesky. Response was null");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to post to Bluesky. Exception Thrown: {e.Message}", e.Message);
        }
    }
}