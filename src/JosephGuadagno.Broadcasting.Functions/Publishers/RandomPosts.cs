using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Publishers;

public class RandomPosts
{
    private readonly ISyndicationFeedReader _syndicationFeedReader;
    private readonly ILogger<RandomPosts> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IRandomPostSettings _randomPostSettings;

    public RandomPosts(ISyndicationFeedReader syndicationFeedReader,
        IRandomPostSettings randomPostSettings,
        ILogger<RandomPosts> logger,
        TelemetryClient telemetryClient)
    {
        _syndicationFeedReader = syndicationFeedReader;
        _randomPostSettings = randomPostSettings;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }
        
    [FunctionName("publishers_random_posts")]
    public Task RunAsync(
        [TimerTrigger("0 0 9,16 * * *")] TimerInfo myTimer,
        [Queue(Constants.Queues.TwitterTweetsToSend)] ICollector<string> outboundMessages)
    {
        // 0 */2 * * * *
        // 0 0 9,16 * * *
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.PublishersRandomPosts, startedAt);

        // Get the feed items
        // Check for the from date
        var cutoffDate = DateTime.MinValue;
        if (_randomPostSettings.CutoffDate != DateTime.MinValue)
        {
            cutoffDate = _randomPostSettings.CutoffDate;
        }

        _logger.LogDebug("Getting all items from feed from '{CutoffDate:u}'", cutoffDate);
        var randomSyndicationItem = _syndicationFeedReader.GetRandomSyndicationItem(cutoffDate, _randomPostSettings.ExcludedCategories);

        // If there is nothing new, save the last checked value and exit
        if (randomSyndicationItem == null)
        {
            _logger.LogDebug("Could not find a random post from feed since '{CutoffDate:u}'", cutoffDate);
            return Task.CompletedTask;
        }

        // Build the tweet
        var hashtags = HashTagList(randomSyndicationItem.Categories);
        var status =
            $"ICYMI: ({randomSyndicationItem.PublishDate.Date.ToShortDateString()}): \"{randomSyndicationItem.Title.Text}.\" RTs and feedback are always appreciated! {randomSyndicationItem.Links[0].Uri} {hashtags}";
            
        // Post the message to the Queue
        outboundMessages.Add(status);
            
        // Return
        _telemetryClient.TrackEvent(Constants.Metrics.RandomTweetSent, new Dictionary<string, string>
        {
            {"title", randomSyndicationItem.Title.Text}, 
            {"tweet", status}
        });
        _logger.LogDebug("Picked a random post {Title}", randomSyndicationItem.Title.Text);
        return Task.CompletedTask;
    }
        
    private static string HashTagList(Collection<SyndicationCategory> categories)
    {
        if (categories is null || categories.Count == 0)
        {
            return "#dotnet #csharp #dotnetcore";
        }

        var hashTagCategories = categories.Where(c => !c.Name.Contains("Articles"));

        return hashTagCategories.Aggregate("",
            (current, category) => current + $" #{category.Name.Replace(" ", "").Replace(".", "")}");
    }
}