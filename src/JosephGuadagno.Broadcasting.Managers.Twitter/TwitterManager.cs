using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;
using LinqToTwitter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Managers.Twitter;

public class TwitterManager : ITwitterManager
{
    private readonly TwitterContext _twitterContext;
    private readonly ILogger<TwitterManager> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    public TwitterManager(TwitterContext twitterContext, ILogger<TwitterManager> logger)
        : this(twitterContext, logger, null)
    {
    }

    public TwitterManager(
        TwitterContext twitterContext,
        ILogger<TwitterManager> logger,
        IServiceScopeFactory? serviceScopeFactory)
    {
        _twitterContext = twitterContext;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
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

        if (_serviceScopeFactory is null)
        {
            throw new InvalidOperationException(
                "ComposeMessageAsync requires an IServiceScopeFactory-backed TwitterManager instance.");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var socialMediaPlatformManager = serviceProvider.GetRequiredService<ISocialMediaPlatformManager>();
        var twitterPlatform =
            await socialMediaPlatformManager.GetByNameAsync(MessageTemplates.Platforms.Twitter, cancellationToken);
        if (twitterPlatform is null)
        {
            return scheduledItem.Message;
        }

        var messageTemplateDataStore = serviceProvider.GetRequiredService<IMessageTemplateDataStore>();
        var messageTemplate = await messageTemplateDataStore.GetAsync(
            twitterPlatform.Id,
            GetMessageType(scheduledItem.ItemType),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(messageTemplate?.Template))
        {
            return scheduledItem.Message;
        }

        var renderedMessage = await TryRenderTemplateAsync(
            serviceProvider,
            scheduledItem,
            messageTemplate.Template,
            cancellationToken);

        return renderedMessage ?? scheduledItem.Message;
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
        IServiceProvider serviceProvider,
        ScheduledItem scheduledItem,
        string templateContent,
        CancellationToken cancellationToken)
    {
        try
        {
            string title = string.Empty;
            string url = string.Empty;
            string description = string.Empty;
            string tags = string.Empty;

            switch (scheduledItem.ItemType)
            {
                case ScheduledItemType.SyndicationFeedSources:
                    var syndicationFeedSourceManager =
                        serviceProvider.GetRequiredService<ISyndicationFeedSourceManager>();
                    var feed = await syndicationFeedSourceManager.GetAsync(
                        scheduledItem.ItemPrimaryKey,
                        cancellationToken);
                    title = feed.Title;
                    url = feed.ShortenedUrl ?? feed.Url;
                    tags = feed.Tags?.Count > 0 ? string.Join(",", feed.Tags) : string.Empty;
                    break;
                case ScheduledItemType.YouTubeSources:
                    var youTubeSourceManager = serviceProvider.GetRequiredService<IYouTubeSourceManager>();
                    var youTubeSource = await youTubeSourceManager.GetAsync(
                        scheduledItem.ItemPrimaryKey,
                        cancellationToken);
                    title = youTubeSource.Title;
                    url = youTubeSource.ShortenedUrl ?? youTubeSource.Url;
                    tags = youTubeSource.Tags?.Count > 0 ? string.Join(",", youTubeSource.Tags) : string.Empty;
                    break;
                case ScheduledItemType.Engagements:
                    var engagementManager = serviceProvider.GetRequiredService<IEngagementManager>();
                    var engagement = await engagementManager.GetAsync(
                        scheduledItem.ItemPrimaryKey,
                        cancellationToken);
                    title = engagement.Name;
                    url = engagement.Url;
                    description = engagement.Comments ?? string.Empty;
                    break;
                case ScheduledItemType.Talks:
                    var talkManager = serviceProvider.GetRequiredService<IEngagementManager>();
                    var talk = await talkManager.GetTalkAsync(
                        scheduledItem.ItemPrimaryKey,
                        cancellationToken);
                    title = talk.Name;
                    url = talk.UrlForTalk ?? string.Empty;
                    description = talk.Comments ?? string.Empty;
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

