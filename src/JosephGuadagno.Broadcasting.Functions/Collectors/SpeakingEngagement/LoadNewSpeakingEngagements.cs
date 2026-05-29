using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Services;
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
    IUserCollectorSpeakingEngagementManager userCollectorSpeakingEngagementManager,
    IFeedCheckManager feedCheckManager,
    ICollectorEventDistributor collectorEventDispatcher,
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
            var configs = await userCollectorSpeakingEngagementManager.GetAllActiveAsync();
            if (configs.Count == 0)
            {
                logger.LogDebug("No active speaking engagement configurations found");
                return new OkObjectResult("No active speaking engagement configurations found");
            }

            var totalSavedCount = 0;
            var totalFoundCount = 0;

            foreach (var config in configs)
            {
                var feedCheck = await feedCheckManager.GetByNameAsync(
                    ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew, config.CreatedByEntraOid
                ) ?? new FeedCheck
                {
                    Name = ConfigurationFunctionNames.CollectorsSpeakingEngagementsLoadNew,
                    LastCheckedFeed = startedAt,
                    LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                    EntraOId = config.CreatedByEntraOid
                };

                logger.LogDebug("Getting speaking engagements from '{FileUrl}' for owner '{OwnerOid}' since '{LastItemAddedOrUpdated}'",
                    config.SpeakingEngagementsFile, config.CreatedByEntraOid, feedCheck.LastItemAddedOrUpdated);

                var newItems = await speakerEngagementsReader.GetAll(config.SpeakingEngagementsFile, feedCheck.LastItemAddedOrUpdated);

                if (newItems.Count == 0)
                {
                    feedCheck.LastCheckedFeed = startedAt;
                    await feedCheckManager.SaveAsync(feedCheck);
                    logger.LogDebug("No new speaking engagements for owner '{OwnerOid}'", config.CreatedByEntraOid);
                    continue;
                }

                totalFoundCount += newItems.Count;

                var savedCount = 0;
                foreach (var item in newItems)
                {
                    if (!await engagementManager.IsEngagementUniqueToUser(
                        item.Name, item.Url, item.StartDateTime.Year, config.CreatedByEntraOid))
                    {
                        logger.LogDebug(
                            "Skipping duplicate speaking engagement '{Name}' ({Url}, {Year}) for owner '{OwnerOid}'",
                            item.Name, item.Url, item.StartDateTime.Year, config.CreatedByEntraOid);
                        continue;
                    }

                    try
                    {
                        var saveResult = await SavePipeline.ExecuteAsync(
                            async ct => await engagementManager.SaveAsync(item, ct));
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
                        await collectorEventDispatcher.DispatchSpeakingEngagementAsync(engagement, config.CreatedByEntraOid);
                        savedCount++;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e,
                            "Failed to save the engagement with the id of: '{Id}' Url:'{Url}'. Exception: {ExceptionMessage}",
                            item.Id, item.Url, e);
                    }
                }

                feedCheck.LastCheckedFeed = startedAt;
                feedCheck.LastUpdatedOn = DateTimeOffset.UtcNow;
                feedCheck.EntraOId = config.CreatedByEntraOid;
                var latestAdded = newItems.Max(item => item.CreatedOn);
                var latestUpdated = newItems.Max(item => item.LastUpdatedOn);
                feedCheck.LastItemAddedOrUpdated = latestUpdated > latestAdded
                    ? latestUpdated.UtcDateTime
                    : latestAdded.UtcDateTime;

                await feedCheckManager.SaveAsync(feedCheck);

                totalSavedCount += savedCount;
                logger.LogInformation("Loaded {SavedCount} of {TotalCount} speaking engagement(s) for owner '{OwnerOid}'",
                    savedCount, newItems.Count, config.CreatedByEntraOid);
            }

            return new OkObjectResult($"Loaded {totalSavedCount} of {totalFoundCount} speaking engagement(s) across {configs.Count} configuration(s)");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while loading new speaking engagements");
            return new BadRequestObjectResult($"An error occurred while loading new speaking engagements: {exception.Message}");
        }
    }
}