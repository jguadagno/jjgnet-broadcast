using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Bluesky;

public class SendPost(IBlueskyManager blueskyManager, TelemetryClient telemetryClient, ILogger<SendPost> logger)
{
    [Function(Constants.ConfigurationFunctionNames.BlueskyPostMessage)]
    public async Task Run(
        [QueueTrigger(Constants.Queues.BlueskyPostToSend)]
        string message)
    {
        try
        {
            var response = await blueskyManager.PostText(message);
            if (response is not null)
            {
                logger.LogDebug("Posting to bluesky: {message}", message);
                telemetryClient.TrackEvent(Constants.Metrics.RandomTweetSent, new Dictionary<string, string>
                {
                    {"message", message}, 
                    {"cid", response.Cid.ToString()}
                });
                return;
            }
            logger.LogDebug("Failed to post to Bluesky. Response was null");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to post to Bluesky. Exception Thrown: {e.Message}", e.Message);
        }
    }
}