using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace JosephGuadagno.Broadcasting.Functions.Collectors.SpeakingEngagement;

public class LoadNewSpeakingEngagements(
    ISpeakingEngagementsReader speakerEngagementsReader,
    IEngagementManager engagementManager,
    IFeedCheckManager feedCheckManager,
    ILogger<LoadNewSpeakingEngagements> logger)
{
    private static readonly ResiliencePipeline SavePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();

    [Function(ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew)]
    public async Task<IActionResult> RunAsync(
        [TimerTrigger("%collectors_speaking_engagements_load_new_speaking_engagements_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew, startedAt);

        try
        {
            var feedCheck = await feedCheckManager.GetByNameAsync(
                ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew
            ) ?? new FeedCheck
            {
                Name = ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew,
                LastCheckedFeed = startedAt,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue
            };

            // Check for new items
            logger.LogDebug("Getting all items from speaking engagements from '{DateToCheckFrom}'", feedCheck.LastItemAddedOrUpdated);
            var newItems = await speakerEngagementsReader.GetAll(feedCheck.LastItemAddedOrUpdated);

            // If there is nothing new, save the last checked value and exit
            if (newItems == null || newItems.Count == 0)
            {
                feedCheck.LastCheckedFeed = startedAt;
                await feedCheckManager.SaveAsync(feedCheck);
                logger.LogDebug("No speaking engagements found in the speaking engagements feed");
                return new OkObjectResult("0 speaking engagements were found");
            }

            // Save the new items to EngagementRepository
            var savedCount = 0;
            foreach (var item in newItems)
            {
                // Skip if the engagement already exists (Name + Url + StartDateTime.Year is the natural key)
                var existingEngagement = await engagementManager.GetByNameAndUrlAndYearAsync(
                    item.Name, item.Url, item.StartDateTime.Year);
                if (existingEngagement != null)
                {
                    logger.LogDebug(
                        "Skipping duplicate speaking engagement '{Name}' ({Url}, {Year})",
                        item.Name, item.Url, item.StartDateTime.Year);
                    continue;
                }

                // attempt to save the item
                try
                {
                    var saveResult = await SavePipeline.ExecuteAsync(
                        async ct => await engagementManager.SaveAsync(item));
                    if (!saveResult.IsSuccess || saveResult.Value is null)
                    {
                        logger.LogError("Failed to save the engagement with the id of: '{Id}' Url:'{Url}'. Error: {Error}",
                            item.Id, item.Url, saveResult.ErrorMessage);
                        continue;
                    }
                    var engagement = saveResult.Value;
                    var properties = new Dictionary<string, string>
                    {
                        { "Id", engagement.Id.ToString() },
                        { "Url", engagement.Url },
                        { "Name", engagement.Name },
                        { "StartDateTime", engagement.StartDateTime.ToString("o") },
                        { "EndDateTime", engagement.EndDateTime.ToString("o") }
                    };
                    logger.LogCustomEvent(Metrics.SpeakingEngagementAddedOrUpdated, properties);
                    savedCount++;
                }
                catch (Exception e)
                {
                    logger.LogError(e,
                        "Failed to save the engagement with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                        item.Id, item.Url, e);
                    continue;
                }
            }

            // Publish the events

            // Save the last checked value
            feedCheck.LastCheckedFeed = startedAt;
            feedCheck.LastUpdatedOn = DateTimeOffset.UtcNow;
            var latestAdded = newItems.Max(item => item.CreatedOn);
            var latestUpdated = newItems.Max(item => item.LastUpdatedOn);
            feedCheck.LastItemAddedOrUpdated = latestUpdated > latestAdded
                ? latestUpdated.UtcDateTime
                : latestAdded.UtcDateTime;

            await feedCheckManager.SaveAsync(feedCheck);

            // Return
            logger.LogInformation("Loaded {SavedCount} of {TotalPostsCount} speaking engagements(s)", savedCount, newItems.Count);
            return new OkObjectResult($"Loaded {savedCount} of {newItems.Count} speaking engagements(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while loading new speaking engagements");
            return new BadRequestObjectResult($"An error occurred while loading new speaking engagements: {exception.Message}");
        }
    }
}