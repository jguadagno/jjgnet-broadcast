using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Managers.Twitter;

public class TwitterManager : ITwitterManager
{
    private readonly TwitterContext _twitterContext;
    private readonly ILogger<TwitterManager> _logger;
    private readonly ISocialMediaPlatformManager _socialMediaPlatformManager;
    private readonly IMessageTemplateDataStore _messageTemplateDataStore;
    private readonly ISyndicationFeedSourceManager _syndicationFeedSourceManager;
    private readonly IYouTubeSourceManager _youTubeSourceManager;
    private readonly IEngagementManager _engagementManager;

    public TwitterManager(
        TwitterContext twitterContext,
        ILogger<TwitterManager> logger,
        ISocialMediaPlatformManager socialMediaPlatformManager,
        IMessageTemplateDataStore messageTemplateDataStore,
        ISyndicationFeedSourceManager syndicationFeedSourceManager,
        IYouTubeSourceManager youTubeSourceManager,
        IEngagementManager engagementManager)
    {
        _twitterContext = twitterContext;
        _logger = logger;
        _socialMediaPlatformManager = socialMediaPlatformManager;
        _messageTemplateDataStore = messageTemplateDataStore;
        _syndicationFeedSourceManager = syndicationFeedSourceManager;
        _youTubeSourceManager = youTubeSourceManager;
        _engagementManager = engagementManager;
    }

    public Task<string?> PublishAsync(SocialMediaPublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);
        return SendTweetAsync(request.Text);
    }

    public async Task<string?> SendTweetAsync(string tweetText)
    {
        try
        {
            var tweet = await TweetAsync(tweetText);
            if (tweet is null)
            {
                _logger.LogError("Failed to send the tweet: '{TweetText}'.", tweetText);
                throw new TwitterPostException($"Failed to send tweet: '{tweetText}'.");
            }

            _logger.LogDebug("Tweet sent successfully. Id: '{TweetId}'", tweet.ID);
            return tweet.ID;
        }
        catch (TwitterPostException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send the tweet: '{TweetText}'. Exception: '{ExceptionMessage}'", tweetText, ex.Message);
            throw new TwitterPostException($"Failed to send tweet: '{tweetText}'.", ex);
        }
    }

    public async Task<string> ComposeMessageAsync(
        ScheduledItem scheduledItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduledItem);

        var twitterPlatform = await _socialMediaPlatformManager.GetByNameAsync(
            MessageTemplates.Platforms.Twitter, cancellationToken);

        if (twitterPlatform is null)
        {
            return scheduledItem.Message;
        }

        var messageTemplate = await _messageTemplateDataStore.GetAsync(
            twitterPlatform.Id,
            GetMessageType(scheduledItem.ItemType),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(messageTemplate?.Template))
        {
            return scheduledItem.Message;
        }

        var rendered = await TryRenderTemplateAsync(scheduledItem, messageTemplate.Template, cancellationToken);
        return rendered ?? scheduledItem.Message;
    }

    protected virtual async Task<Tweet?> TweetAsync(string tweetText)
    {
        return await _twitterContext.TweetAsync(tweetText);
    }

    private static string GetMessageType(ScheduledItemType itemType) => itemType switch
    {
        ScheduledItemType.Engagements => MessageTemplates.MessageTypes.NewSpeakingEngagement,
        ScheduledItemType.Talks => MessageTemplates.MessageTypes.ScheduledItem,
        ScheduledItemType.SyndicationFeedSources => MessageTemplates.MessageTypes.NewSyndicationFeedItem,
        ScheduledItemType.YouTubeSources => MessageTemplates.MessageTypes.NewYouTubeItem,
        _ => MessageTemplates.MessageTypes.RandomPost
    };

    private async Task<string?> TryRenderTemplateAsync(
        ScheduledItem scheduledItem,
        string templateContent,
        CancellationToken cancellationToken)
    {
        try
        {
            string title;
            string url;
            string description;
            string tags;

            switch (scheduledItem.ItemType)
            {
                case ScheduledItemType.SyndicationFeedSources:
                    var feed = await _syndicationFeedSourceManager.GetAsync(
                        scheduledItem.ItemPrimaryKey, cancellationToken);
                    title = feed.Title;
                    url = feed.ShortenedUrl ?? feed.Url;
                    description = string.Empty;
                    tags = feed.Tags.Count > 0 ? string.Join(",", feed.Tags) : string.Empty;
                    break;

                case ScheduledItemType.YouTubeSources:
                    var video = await _youTubeSourceManager.GetAsync(
                        scheduledItem.ItemPrimaryKey, cancellationToken);
                    title = video.Title;
                    url = video.ShortenedUrl ?? video.Url;
                    description = string.Empty;
                    tags = video.Tags.Count > 0 ? string.Join(",", video.Tags) : string.Empty;
                    break;

                case ScheduledItemType.Engagements:
                    var engagement = await _engagementManager.GetAsync(
                        scheduledItem.ItemPrimaryKey, cancellationToken);
                    title = engagement.Name;
                    url = engagement.Url;
                    description = engagement.Comments ?? string.Empty;
                    tags = string.Empty;
                    break;

                case ScheduledItemType.Talks:
                    var talk = await _engagementManager.GetTalkAsync(
                        scheduledItem.ItemPrimaryKey, cancellationToken);
                    title = talk.Name;
                    url = talk.UrlForTalk;
                    description = talk.Comments ?? string.Empty;
                    tags = string.Empty;
                    break;

                default:
                    return null;
            }

            var template = Template.Parse(templateContent);
            var scriptObject = new ScriptObject();
            scriptObject.Import(new
            {
                title,
                url,
                description,
                tags,
                image_url = scheduledItem.ImageUrl
            });
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            var rendered = await template.RenderAsync(context);
            return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scriban template rendering failed for Twitter scheduled item {Id}", scheduledItem.Id);
            return null;
        }
    }
}