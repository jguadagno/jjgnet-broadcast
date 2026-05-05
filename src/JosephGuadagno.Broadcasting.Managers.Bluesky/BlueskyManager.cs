using idunno.AtProto;
using idunno.AtProto.Repo;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using idunno.Bluesky.RichText;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using X.Web.MetaExtractor;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky;

public class BlueskyManager : IBlueskyManager
{
    private readonly HttpClient _httpClient;
    private readonly IBlueskySettings _blueskySettings;
    private readonly ILogger<BlueskyManager> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    private BlueskyAgent? _agent;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    public BlueskyManager(HttpClient httpClient, IBlueskySettings blueskySettings, ILogger<BlueskyManager> logger)
        : this(httpClient, blueskySettings, logger, null)
    {
    }

    public BlueskyManager(
        HttpClient httpClient,
        IBlueskySettings blueskySettings,
        ILogger<BlueskyManager> logger,
        IServiceScopeFactory? serviceScopeFactory)
    {
        _httpClient = httpClient;
        _blueskySettings = blueskySettings;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }


    private async Task<BlueskyAgent> EnsureAuthenticatedAsync()
    {
        if (_agent?.IsAuthenticated == true)
            return _agent;

        await _loginLock.WaitAsync();
        try
        {
            // Double-check after acquiring the lock
            if (_agent?.IsAuthenticated == true)
                return _agent;

            _agent ??= new BlueskyAgent();
            var loginResult = await _agent.Login(_blueskySettings.BlueskyUserName!, _blueskySettings.BlueskyPassword!);
            if (loginResult.Succeeded)
                return _agent;

            _logger.LogError("Login failed. Status Code: {LoginResultStatusCode}, Error Details {LoginResultAtErrorDetail}",
                loginResult.StatusCode, loginResult.AtErrorDetail?.Message);
            throw new BlueskyPostException(
                "Bluesky login failed.",
                (int?)loginResult.StatusCode,
                loginResult.AtErrorDetail?.Message);
        }
        finally
        {
            _loginLock.Release();
        }
    }

    public async Task<CreateRecordResult?> PostText(string postText)
    {
        return await Post(new PostBuilder(postText));
    }

    public async Task<string?> PublishAsync(SocialMediaPublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);

        var postBuilder = new PostBuilder(request.Text);

        if (!string.IsNullOrWhiteSpace(request.ShortenedUrl) && !string.IsNullOrWhiteSpace(request.LinkUrl))
        {
            if (!request.Text.EndsWith(' '))
            {
                postBuilder.Append(" ");
            }

            postBuilder.Append(new Link(request.ShortenedUrl, request.ShortenedUrl));

            var embeddedExternalRecord = !string.IsNullOrEmpty(request.ImageUrl)
                ? await GetEmbeddedExternalRecordWithThumbnail(request.LinkUrl, request.ImageUrl)
                : await GetEmbeddedExternalRecord(request.LinkUrl);

            if (embeddedExternalRecord is not null)
            {
                postBuilder.EmbedRecord(embeddedExternalRecord);
            }
        }
        else if (!string.IsNullOrEmpty(request.ImageUrl) && !string.IsNullOrEmpty(request.LinkUrl))
        {
            var embeddedExternalRecord =
                await GetEmbeddedExternalRecordWithThumbnail(request.LinkUrl, request.ImageUrl);
            if (embeddedExternalRecord is not null)
            {
                postBuilder.EmbedRecord(embeddedExternalRecord);
            }
        }

        if (request.Hashtags is not null)
        {
            foreach (var hashtag in request.Hashtags)
            {
                postBuilder.Append(" ");
                postBuilder.Append(new HashTag(hashtag));
            }
        }

        var response = await Post(postBuilder);
        return response?.Cid.ToString();
    }

    public async Task<CreateRecordResult?> Post(PostBuilder postBuilder)
    {
        var agent = await EnsureAuthenticatedAsync();

        var response = await agent.Post(postBuilder);
        if (response.Succeeded)
        {
            return response.Result;
        }

        // On auth failure, clear the cached session and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _agent = null;
            agent = await EnsureAuthenticatedAsync();
            response = await agent.Post(postBuilder);
            if (response.Succeeded)
                return response.Result;
        }

        // Post Failed
        _logger.LogError(
            "Bluesky Post failed! Status Code: {ResponseStatusCode}, Error Details {ResponseErrorDetail}",
            response.StatusCode, response.AtErrorDetail?.Message);
        throw new BlueskyPostException(
            $"Bluesky post failed.",
            (int?)response.StatusCode,
            response.AtErrorDetail?.Message);
    }

    public async Task<bool> DeletePost(StrongReference strongReference)
    {
        BlueskyAgent agent;
        try
        {
            agent = await EnsureAuthenticatedAsync();
        }
        catch (BlueskyPostException ex)
        {
            _logger.LogError(ex, "Failed to delete Bluesky Post! Login failed. Cid: '{StrongReferenceCid}'", strongReference.Cid);
            return false;
        }

        var response = await agent.DeletePost(strongReference);
        if (response.Succeeded)
        {
            _logger.LogDebug("Bluesky Post successfully deleted! Cid: \'{StrongReferenceCid}\'",
                strongReference.Cid);
            return true;
        }

        // On auth failure, clear the cached session and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _agent = null;
            try
            {
                agent = await EnsureAuthenticatedAsync();
                response = await agent.DeletePost(strongReference);
                if (response.Succeeded)
                {
                    _logger.LogDebug("Bluesky Post successfully deleted after re-auth! Cid: '{StrongReferenceCid}'", strongReference.Cid);
                    return true;
                }
            }
            catch (BlueskyPostException ex)
            {
                _logger.LogError(ex, "Failed to delete Bluesky Post! Re-auth failed. Cid: '{StrongReferenceCid}'", strongReference.Cid);
                return false;
            }
        }

        _logger.LogWarning(
            "Failed to delete Bluesky Post! Status Code: {ResponseStatusCode}, Message: \'{Message}\', Cid: {StrongReferenceCid}",
            response.StatusCode, response.AtErrorDetail?.Message, strongReference.Cid);
        return false;
    }

    public async Task<EmbeddedExternal?> GetEmbeddedExternalRecord(string? externalUrl)
    {
        if (string.IsNullOrEmpty(externalUrl))
        {
            return null;
        }

        BlueskyAgent agent;
        try
        {
            agent = await EnsureAuthenticatedAsync();
        }
        catch (BlueskyPostException)
        {
            return null;
        }

        Uri page = new(externalUrl);

        Extractor metadataExtractor = new();
        var pageMetadata = await metadataExtractor.Extract(page, CancellationToken.None);

        string title = pageMetadata.Title;
        string? pageUri = pageMetadata.Source?.Url.ToString();
        string description = pageMetadata.Description;

        if (!string.IsNullOrEmpty(pageUri) && !string.IsNullOrEmpty(title))
        {
            // We have the minimum needed to embed a card.
            Blob? thumbnailBlob = null;

            // Now see if there's a thumbnail
            string? thumbnailUri = pageMetadata.Metadata.Where(o => o.Key == "og:image").Select(o => o.Value)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(thumbnailUri))
            {
                // Try to grab the image, then upload it as a blob.
                try
                {
                    

                    using HttpResponseMessage response = await _httpClient.GetAsync(thumbnailUri);
                    response.EnsureSuccessStatusCode();

                    var responseBody =
                        await response.Content.ReadAsByteArrayAsync();

                    if (response.Content.Headers.ContentType is not null &&
                        response.Content.Headers.ContentType.MediaType is not null)
                    {
                        AtProtoHttpResult<Blob> uploadResult = await
                            agent.UploadBlob(responseBody, response.Content.Headers.ContentType.MediaType);

                        if (uploadResult.Succeeded)
                        {
                            thumbnailBlob = uploadResult.Result;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                } // Ignore any exceptions from trying to get the thumbnail and upload the image.

                EmbeddedExternal embeddedExternal = new(pageUri, title, description, thumbnailBlob);
                return embeddedExternal;
            }
        }

        // If we made it here, something failed.
        return null;
    }

    public async Task<EmbeddedExternal?> GetEmbeddedExternalRecordWithThumbnail(string externalUrl, string thumbnailImageUrl)
    {
        if (string.IsNullOrEmpty(externalUrl))
            return null;

        BlueskyAgent agent;
        try
        {
            agent = await EnsureAuthenticatedAsync();
        }
        catch (BlueskyPostException)
        {
            return null;
        }

        Uri page = new(externalUrl);
        Extractor metadataExtractor = new();
        var pageMetadata = await metadataExtractor.Extract(page, CancellationToken.None);

        string title = pageMetadata.Title;
        string? pageUri = pageMetadata.Source?.Url.ToString();
        string description = pageMetadata.Description;

        if (!string.IsNullOrEmpty(pageUri) && !string.IsNullOrEmpty(title))
        {
            Blob? thumbnailBlob = null;

            if (!string.IsNullOrEmpty(thumbnailImageUrl))
            {
                try
                {
                    using HttpResponseMessage response = await _httpClient.GetAsync(thumbnailImageUrl);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsByteArrayAsync();
                    if (response.Content.Headers.ContentType?.MediaType is not null)
                    {
                        AtProtoHttpResult<Blob> uploadResult = await
                            agent.UploadBlob(responseBody, response.Content.Headers.ContentType.MediaType);
                        if (uploadResult.Succeeded)
                            thumbnailBlob = uploadResult.Result;
                    }
                }
                catch (HttpRequestException)
                {
                } // Ignore any exceptions from trying to get the thumbnail.
            }

            return new EmbeddedExternal(pageUri, title, description, thumbnailBlob);
        }

        return null;
    }

    public async Task<string> ComposeMessageAsync(
        ScheduledItem scheduledItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduledItem);

        if (_serviceScopeFactory is null)
        {
            throw new InvalidOperationException(
                "ComposeMessageAsync requires an IServiceScopeFactory-backed BlueskyManager instance.");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var socialMediaPlatformManager = serviceProvider.GetRequiredService<ISocialMediaPlatformManager>();
        var blueskyPlatform =
            await socialMediaPlatformManager.GetByNameAsync(MessageTemplates.Platforms.Bluesky, cancellationToken);
        if (blueskyPlatform is null)
        {
            return scheduledItem.Message;
        }

        var messageTemplateDataStore = serviceProvider.GetRequiredService<IMessageTemplateDataStore>();
        var messageTemplate = await messageTemplateDataStore.GetAsync(
            blueskyPlatform.Id,
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
            scriptObject.Import(new { title, url, description, tags, image_url = scheduledItem.ImageUrl });
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            var rendered = await template.RenderAsync(context);
            return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scriban template rendering failed for Bluesky scheduled item {Id}", scheduledItem.Id);
            return null;
        }
    }
}