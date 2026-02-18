using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SpeakingEngagement;

public class LoadAllSpeakingEngagements(
    ISpeakingEngagementsReader speakerEngagementsReader,
    IEngagementManager engagementManager,
    IFeedCheckManager feedCheckManager,
    ILogger<LoadAllSpeakingEngagements> logger,
    TelemetryClient telemetryClient)
{
    [Function(ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadAll)]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req,
        string checkFrom)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadAll, startedAt);

        // Check for the date to check from
        var dateToCheckFrom = DateTime.MinValue;

        if (!string.IsNullOrEmpty(checkFrom))
        {
            var parsed = DateTime.TryParse(checkFrom, out var dateFrom);
            if (parsed)
            {
                dateToCheckFrom = dateFrom;
            }
        }

        try
        {
            logger.LogDebug("Getting all items from speaking engagements from '{DateToCheckFrom}'", dateToCheckFrom);
            var newItems = await speakerEngagementsReader.GetAll(dateToCheckFrom);

            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                logger.LogDebug("No speaking engagements were found in the speaking engagement feed");
                return new OkObjectResult("0 speaking engagements were found");
            }

            // Save the new items
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // attempt to save the item
                try
                {
                    var engagement = await engagementManager.SaveAsync(item);
                    var properties = new Dictionary<string, string>
                    {
                        { "Id", engagement.Id.ToString() },
                        { "Url", engagement.Url },
                        { "Name", engagement.Name },
                        { "StartDateTime", engagement.StartDateTime.ToString("o") },
                        { "EndDateTime", engagement.EndDateTime.ToString("o") }
                    };
                    telemetryClient.TrackEvent(Metrics.SpeakingEngagementAddedOrUpdated, properties);
                    savedCount++;
                }
                catch (Exception e)
                {
                    logger.LogError(e,
                        "Failed to save the engagement with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                        item.Id, item.Url, e);
                }
            }

            // Save the last checked value
            var feedCheck =
                await feedCheckManager.GetByNameAsync(ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew) ??
                new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.Now
                };
            var latestAdded = newItems.Max(item => item.CreatedOn);
            var latestUpdated = newItems.Max(item => item.LastUpdatedOn);
            feedCheck.LastItemAddedOrUpdated = latestUpdated > latestAdded
                ? latestUpdated
                : latestAdded;

            await feedCheckManager.SaveAsync(feedCheck);

            // Return
            logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} speaking engagements(s)", savedCount, newItems.Count);
            return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} speaking engagements(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load all speaking engagements. Exception: {ExceptionMessage}", exception.Message);
            return new BadRequestObjectResult(exception.Message);
        }
    }
}