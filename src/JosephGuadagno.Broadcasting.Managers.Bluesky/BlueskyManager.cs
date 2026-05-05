using idunno.AtProto;
using idunno.AtProto.Repo;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using idunno.Bluesky.RichText;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging;
using X.Web.MetaExtractor;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky;

public class BlueskyManager(HttpClient httpClient, IBlueskySettings blueskySettings, ILogger<BlueskyManager> logger)
    : IBlueskyManager
{
    private BlueskyAgent? _agent;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

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
            var loginResult = await _agent.Login(blueskySettings.BlueskyUserName!, blueskySettings.BlueskyPassword!);
            if (loginResult.Succeeded)
                return _agent;

            logger.LogError("Login failed. Status Code: {LoginResultStatusCode}, Error Details {LoginResultAtErrorDetail}",
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
        logger.LogError(
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
            logger.LogError(ex, "Failed to delete Bluesky Post! Login failed. Cid: '{StrongReferenceCid}'", strongReference.Cid);
            return false;
        }

        var response = await agent.DeletePost(strongReference);
        if (response.Succeeded)
        {
            logger.LogDebug("Bluesky Post successfully deleted! Cid: \'{StrongReferenceCid}\'",
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
                    logger.LogDebug("Bluesky Post successfully deleted after re-auth! Cid: '{StrongReferenceCid}'", strongReference.Cid);
                    return true;
                }
            }
            catch (BlueskyPostException ex)
            {
                logger.LogError(ex, "Failed to delete Bluesky Post! Re-auth failed. Cid: '{StrongReferenceCid}'", strongReference.Cid);
                return false;
            }
        }

        logger.LogWarning(
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
                    var downloadHttpClient = httpClient;

                    using HttpResponseMessage response = await downloadHttpClient.GetAsync(thumbnailUri);
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
                    using HttpResponseMessage response = await httpClient.GetAsync(thumbnailImageUrl);
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
}
