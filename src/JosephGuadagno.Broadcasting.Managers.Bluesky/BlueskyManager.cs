using idunno.AtProto;
using idunno.AtProto.Repo;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using Microsoft.Extensions.Logging;
using X.Web.MetaExtractor;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky;

public class BlueskyManager(HttpClient httpClient, IBlueskySettings blueskySettings, ILogger<BlueskyManager> logger)
    : IBlueskyManager
{

    public async Task<CreateRecordResponse?> PostText(string postText)
    {
        return await Post(new PostBuilder(postText));
    }

    public async Task<CreateRecordResponse?> Post(PostBuilder postBuilder)
    {
        BlueskyAgent agent = new();

        var loginResult = await agent.Login(blueskySettings.BlueskyUserName, blueskySettings.BlueskyPassword);
        if (loginResult.Succeeded)
        {
            var response = await agent.Post(postBuilder);
            if (response.Succeeded)
            {
                return response.Result;
            }

            // Post Failed
            logger.LogError(
                "Bluesky Post failed! Status Code: {ResponseStatusCode}, Error Details {ResponseErrorDetail}",
                response.StatusCode, response.AtErrorDetail?.Message);
            return response.Result;
        }

        // Login Failed
        logger.LogError("Login failed. Status Code: {LoginResultStatusCode}, Error Details {LoginResultAtErrorDetail}",
            loginResult.StatusCode, loginResult.AtErrorDetail?.Message);
        return null;
    }

    public async Task<bool> DeletePost(StrongReference strongReference)
    {
        BlueskyAgent agent = new();

        var loginResult = await agent.Login(blueskySettings.BlueskyUserName, blueskySettings.BlueskyPassword);
        if (loginResult.Succeeded)
        {
            var response = await agent.DeletePost(strongReference);
            if (response.Succeeded)
            {
                logger.LogDebug("Bluesky Post successfully deleted! Cid: \'{StrongReferenceCid}\'",
                    strongReference.Cid);
                return true;
            }

            logger.LogWarning(
                "Failed to delete Bluesky Post! Status Code: {LoginResultStatusCode}, Message: \'{Message}\', Cid: {StrongReferenceCid}",
                loginResult.StatusCode, loginResult.AtErrorDetail?.Message, strongReference.Cid);
            return false;
        }

        logger.LogError("Failed to delete Bluesky Post! Login Failed! Status Code: {LoginResultStatusCode}, Message: '{Message}', {StrongReferenceCidId}", loginResult.StatusCode, loginResult.AtErrorDetail?.Message, strongReference.Cid);

        return false;
    }

    public async Task<EmbeddedExternal?> GetEmbeddedExternalRecord(string externalUrl)
    {
        if (string.IsNullOrEmpty(externalUrl))
        {
            return null;
        }

        BlueskyAgent agent = new();

        var loginResult = await agent.Login(blueskySettings.BlueskyUserName, blueskySettings.BlueskyPassword);
        if (!loginResult.Succeeded)
        {
            return null;
        }

        Uri page = new(externalUrl);

        Extractor metadataExtractor = new();
        var pageMetadata = await metadataExtractor.ExtractAsync(page);

        string title = pageMetadata.Title;
        string pageUri = pageMetadata.Url;
        string description = pageMetadata.Description;

        if (!string.IsNullOrEmpty(pageUri) && !string.IsNullOrEmpty(title))
        {
            // We have the minimum needed to embed a card.
            Blob? thumbnailBlob = null;

            // Now see if there's a thumbnail
            string? thumbnailUri = pageMetadata.MetaTags.Where(o => o.Key == "og:image").Select(o => o.Value)
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

        // If we made it here something failed.
        return null;
    }
}