using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn;

public class LinkedInManager : ILinkedInManager
{

    private const string LinkedInPostUrl = "https://api.linkedin.com/v2/ugcPosts";
    private const string LinkedInUserUrl = "https://api.linkedin.com/v2/me";
    private const string LinkedInAssetUrl = "https://api.linkedin.com/v2/assets?action=registerUpload";
    
    private const string LinkedInAuthorUrn = "urn:li:person:{0}";

    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkedInManager> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    
    public LinkedInManager(HttpClient httpClient, ILogger<LinkedInManager> logger)
        : this(httpClient, logger, null)
    {
    }

    public LinkedInManager(
        HttpClient httpClient,
        ILogger<LinkedInManager> logger,
        IServiceScopeFactory? serviceScopeFactory)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<string?> PublishAsync(SocialMediaPublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AccessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AuthorId);

        if (request.ImageBytes is { Length: > 0 })
        {
            return await PostShareTextAndImage(
                request.AccessToken!,
                request.AuthorId!,
                request.Text,
                request.ImageBytes,
                request.Title,
                request.Description);
        }

        if (!string.IsNullOrEmpty(request.ImageUrl))
        {
            var imageResponse = await _httpClient.GetAsync(request.ImageUrl);
            if (imageResponse.StatusCode == HttpStatusCode.OK)
            {
                var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
                return await PostShareTextAndImage(
                    request.AccessToken!,
                    request.AuthorId!,
                    request.Text,
                    imageBytes,
                    request.Title,
                    request.Description);
            }

            if (!string.IsNullOrEmpty(request.LinkUrl))
            {
                return await PostShareTextAndLink(
                    request.AccessToken!,
                    request.AuthorId!,
                    request.Text,
                    request.LinkUrl,
                    request.Title,
                    request.Description);
            }

            throw new LinkedInPostException(
                $"Unable to get the image from the url. Status Code: {imageResponse.StatusCode}");
        }

        if (!string.IsNullOrEmpty(request.LinkUrl))
        {
            return await PostShareTextAndLink(
                request.AccessToken!,
                request.AuthorId!,
                request.Text,
                request.LinkUrl,
                request.Title,
                request.Description);
        }

        return await PostShareText(request.AccessToken!, request.AuthorId!, request.Text);
    }

    public async Task<string> ComposeMessageAsync(
        ScheduledItem scheduledItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scheduledItem);

        if (_serviceScopeFactory is null)
        {
            throw new InvalidOperationException(
                "ComposeMessageAsync requires an IServiceScopeFactory-backed LinkedInManager instance.");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var socialMediaPlatformManager = serviceProvider.GetRequiredService<ISocialMediaPlatformManager>();
        var linkedInPlatform =
            await socialMediaPlatformManager.GetByNameAsync(MessageTemplates.Platforms.LinkedIn, cancellationToken);
        if (linkedInPlatform is null)
        {
            return scheduledItem.Message;
        }

        var messageTemplateDataStore = serviceProvider.GetRequiredService<IMessageTemplateDataStore>();
        var messageTemplate = await messageTemplateDataStore.GetAsync(
            linkedInPlatform.Id,
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

    /// <summary>
    /// Gets the current user's profile based on the access token
    /// </summary>
    /// <param name="accessToken">The access token to use for the call</param>
    /// <returns>A <see cref="LinkedInUser"/> upon success</returns>
    /// <remarks>
    /// Based on documentation: https://learn.microsoft.com/en-us/linkedin/shared/integrations/people/profile-api?context=linkedin%2Fconsumer%2Fcontext#retrieve-current-members-profile
    ///  
    /// Calling this method requires one of the following permissions:
    /// | Permission      |	Description |
    /// | r_liteprofile	  | Required to retrieve name and photo for the authenticated user. Please review Lite Profile Fields. |
    /// | r_basicprofile  |	Required to retrieve name, photo, headline, and vanity name for the authenticated user. Please review Basic Profile Fields. Note that the v2 r_basicprofile permission grants only a subset of fields provided in v1. |
    /// | r_compliance	  | [Private permission] Required to retrieve your activity for compliance monitoring and archiving. This is a private permission and access is granted to select developers. |
    /// Details: https://learn.microsoft.com/en-us/linkedin/shared/integrations/people/profile-api?context=linkedin%2Fconsumer%2Fcontext#permissions
    /// </remarks>
    public async Task<LinkedInUser> GetMyLinkedInUserProfile(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        
        return await ExecuteGetAsync<LinkedInUser>(LinkedInUserUrl, accessToken);
    }
    
    /// <summary>
    /// Shares a text only post to LinkedIn
    /// </summary>
    /// <remarks>Based on the API https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/share-on-linkedin?context=linkedin%2Fconsumer%2Fcontext#create-a-text-share</remarks>
    /// <param name="accessToken">The access token to use</param>
    /// <param name="authorId">The LinkedIn URN of the person authoring this share</param>
    /// <param name="postText">The text of the share</param>
    /// <returns>A string with the LinkedIn share id</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="LinkedInPostException"></exception>
    public async Task<string> PostShareText(string accessToken, string authorId, string postText)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        if (string.IsNullOrEmpty(authorId))
        {
            throw new ArgumentNullException(nameof(authorId));
        }
        if (string.IsNullOrEmpty(postText))
        {
            throw new ArgumentNullException(nameof(postText));
        }
        
        var shareRequest = new ShareRequest
        {
            Author = string.Format(LinkedInAuthorUrn, authorId),
            Visibility = new Visibility { VisibilityEnum = VisibilityEnum.Anyone },
            SpecificContent = new SpecificContent
            {
                ShareContent = new ShareContent
                {
                    ShareCommentary = new TextProperties()
                    {
                        Text = postText
                    },
                    ShareMediaCategoryEnum = ShareMediaCategoryEnum.None
                }
            }
        };
        
        var linkedInResponse = await CallPostShareUrl(accessToken, shareRequest);
        if (linkedInResponse is { IsSuccess: true, Id: not null })
        {
            return linkedInResponse.Id;
        }
        throw new LinkedInPostException("Failed to post status update to LinkedIn",
            linkedInResponse.ServiceErrorCode, linkedInResponse.Message);
    }
    
    /// <summary>
    /// Post a text and link to LinkedIn
    /// </summary>
    /// <param name="accessToken">The access token to use</param>
    /// <param name="authorId">The LinkedIn URN of the person authoring this share</param>
    /// <param name="postText">The text of the share</param>
    /// <param name="link">The link to share. Note, there is no validation of this link</param>
    /// <param name="linkTitle">Optional, title of the link</param>
    /// <param name="linkDescription">Optional, description of the link</param>
    /// <returns>A string with the LinkedIn share id</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="LinkedInPostException"></exception>
    public async Task<string> PostShareTextAndLink(string accessToken, string authorId, string postText, string link, string? linkTitle = null, string? linkDescription = null)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        if (string.IsNullOrEmpty(authorId))
        {
            throw new ArgumentNullException(nameof(authorId));
        }
        if (string.IsNullOrEmpty(postText))
        {
            throw new ArgumentNullException(nameof(postText));
        }
        if (string.IsNullOrEmpty(link))
        {
            throw new ArgumentNullException(nameof(link));
        }
        
        var shareRequest = new ShareRequest
        {
            Author = string.Format(LinkedInAuthorUrn, authorId),
            Visibility = new Visibility { VisibilityEnum = VisibilityEnum.Anyone },
            SpecificContent = new SpecificContent
            {
                ShareContent = new ShareContent
                {
                    ShareCommentary = new TextProperties()
                    {
                        Text = postText
                    },
                    ShareMediaCategoryEnum = ShareMediaCategoryEnum.Article
                }
            }
        };
        var media = new Media{OriginalUrl = link};
        if (!string.IsNullOrEmpty(linkDescription))
        {
            media.Description = new TextProperties {Text = linkDescription};
        }
        if (!string.IsNullOrEmpty(linkTitle))
        {
            media.Title = new TextProperties {Text = linkTitle};
        }
        shareRequest.SpecificContent.ShareContent.Media = new[] { media };
        
        var linkedInResponse = await CallPostShareUrl(accessToken, shareRequest);
        if (linkedInResponse is { IsSuccess: true, Id: not null })
        {
            return linkedInResponse.Id;
        }
        throw new LinkedInPostException(BuildLinkedInResponseErrorMessage(linkedInResponse));

    }
    
    /// <summary>
    /// Shares text and an image to LinkedIn
    /// </summary>
    /// <param name="accessToken">The access token to use</param>
    /// <param name="authorId">The LinkedIn URN of the person authoring this share</param>
    /// <param name="postText">The text of the share</param>
    /// <param name="image">The byte array for the image</param>
    /// <param name="imageTitle">Optional, title of the image</param>
    /// <param name="imageDescription">Optional, description of the image</param>
    /// <returns>A string with the LinkedIn share id</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="LinkedInPostException"></exception>
    public async Task<string> PostShareTextAndImage(string accessToken, string authorId, string postText, byte[] image, string? imageTitle = null, string? imageDescription = null)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        if (string.IsNullOrEmpty(authorId))
        {
            throw new ArgumentNullException(nameof(authorId));
        }
        if (string.IsNullOrEmpty(postText))
        {
            throw new ArgumentNullException(nameof(postText));
        }
        if (image == null || image.Length == 0)
        {
            throw new ArgumentNullException(nameof(image));
        }
        
        // Call the Register Image endpoint to get the Asset URN
        var uploadResponse = await GetUploadResponse(accessToken, authorId);
        
        // Upload the image
        var uploadUrl = uploadResponse.Value!.UploadMechanism!.MediaUploadHttpRequest!.UploadUrl!;
        var wasFileUploadSuccessful = await UploadImage(accessToken, uploadUrl, image);

        if (!wasFileUploadSuccessful)
        {
            throw new LinkedInPostException("Failed to upload image to LinkedIn");
        }
        
        // Send the image via PostShare
        var shareRequest = new ShareRequest
        {
            Author = string.Format(LinkedInAuthorUrn, authorId),
            Visibility = new Visibility { VisibilityEnum = VisibilityEnum.Anyone },
            SpecificContent = new SpecificContent
            {
                ShareContent = new ShareContent
                {
                    ShareCommentary = new TextProperties()
                    {
                        Text = postText
                    },
                    ShareMediaCategoryEnum = ShareMediaCategoryEnum.Image
                }
            }
        };
        
        var media = new Media{MediaUrn = uploadResponse.Value.Asset};
        
        if (!string.IsNullOrEmpty(imageDescription))
        {
            media.Description = new TextProperties {Text = imageDescription};
        }
        if (!string.IsNullOrEmpty(imageTitle))
        {
            media.Title = new TextProperties {Text = imageTitle};
        }
        shareRequest.SpecificContent.ShareContent.Media = new[] { media };
        
        var linkedInResponse = await CallPostShareUrl(accessToken, shareRequest);
        if (linkedInResponse is { IsSuccess: true, Id: not null })
        {
            return linkedInResponse.Id;
        }
        throw new LinkedInPostException(BuildLinkedInResponseErrorMessage(linkedInResponse));
    }

    public async Task<LinkedInTokenInfo> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken, string accessTokenUrl)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));
        if (string.IsNullOrEmpty(clientSecret))
            throw new ArgumentNullException(nameof(clientSecret));
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentNullException(nameof(refreshToken));
        if (string.IsNullOrEmpty(accessTokenUrl))
            throw new ArgumentNullException(nameof(accessTokenUrl));

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        });

        var response = await _httpClient.PostAsync(accessTokenUrl, formContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new LinkedInPostException(
                $"LinkedIn token refresh failed with status {response.StatusCode}: {content}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var tokenResponse = JsonSerializer.Deserialize<LinkedInRefreshResponse>(content, options);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new LinkedInPostException(
                $"Unable to deserialize the token refresh response from LinkedIn: {content}");

        var now = DateTime.UtcNow;
        return new LinkedInTokenInfo
        {
            AccessToken = tokenResponse.AccessToken,
            ExpiresOn = now.AddSeconds(tokenResponse.ExpiresIn),
            RefreshToken = tokenResponse.RefreshToken,
            RefreshTokenExpiresOn = tokenResponse.RefreshTokenExpiresIn.HasValue
                ? now.AddSeconds(tokenResponse.RefreshTokenExpiresIn.Value)
                : null
        };
    }

    
    private async Task<T> ExecuteGetAsync<T>(string url, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (_httpClient.DefaultRequestHeaders.Authorization is not null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new LinkedInPostException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
            
        // Parse the Results
        var content = await response.Content.ReadAsStringAsync();
                
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        var results = JsonSerializer.Deserialize<T>(content, options);

        if (results == null)
        {
            throw new LinkedInPostException(
                $"Unable to deserialize the response from the HttpResponseMessage: {content}.");
        }

        return results;
    }

    private async Task<ShareResponse> CallPostShareUrl(string accessToken, ShareRequest shareRequest)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, LinkedInPostUrl);
        requestMessage.Headers.Add("Authorization", $"Bearer {accessToken}");
        requestMessage.Headers.Add ("X-Restli-Protocol-Version", "2.0.0");
       
        JsonSerializerOptions jsonSerializationOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var jsonRequest = JsonSerializer.Serialize(shareRequest, jsonSerializationOptions);  
        var jsonContent = new StringContent(jsonRequest, null, "application/json");
        requestMessage.Content = jsonContent;
        
        var response = await _httpClient.SendAsync(requestMessage);
        
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var linkedInResponse = JsonSerializer.Deserialize<ShareResponse>(content, options);
        
        if (linkedInResponse == null)
        {
            throw new LinkedInPostException(
                $"Unable to deserialize the response from the HttpResponseMessage: {content}.");
        }

        return linkedInResponse;
    }

    private async Task<UploadRegistrationResponse> GetUploadResponse(string accessToken, string authorId)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        if (string.IsNullOrEmpty(authorId))
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        var uploadRequest = new UploadRegistrationRequest
        {
            RegisterUploadRequest = new RegisterUploadRequest
            {
                Owner = string.Format(LinkedInAuthorUrn, authorId)
            }
        };
        
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, LinkedInAssetUrl);
        requestMessage.Headers.Add("Authorization", $"Bearer {accessToken}");
        requestMessage.Headers.Add ("X-Restli-Protocol-Version", "2.0.0");
        
        JsonSerializerOptions jsonSerializationOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        
        var jsonRequest = JsonSerializer.Serialize(uploadRequest, jsonSerializationOptions);  
        var jsonContent = new StringContent(jsonRequest, null, "application/json");
        requestMessage.Content = jsonContent;
        
        var response = await _httpClient.SendAsync(requestMessage);
        
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var uploadResponse = JsonSerializer.Deserialize<UploadRegistrationResponse>(content, options);

        if (uploadResponse == null)
        {
            throw new LinkedInPostException("Could not deserialize the response from LinkedIn");
        }
        return uploadResponse;
    }

    private async Task<bool> UploadImage(string accessToken, string uploadUrl, byte[] image)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }
        if (string.IsNullOrEmpty(uploadUrl))
        {
            throw new ArgumentNullException(nameof(uploadUrl));
        }
        if (image == null || image.Length == 0)
        {
            throw new ArgumentNullException(nameof(image));
        }
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        requestMessage.Headers.Add("Authorization", $"Bearer {accessToken}");
        requestMessage.Headers.Add ("X-Restli-Protocol-Version", "2.0.0");
        
        requestMessage.Content = new ByteArrayContent(image);
        
        var response = await _httpClient.SendAsync(requestMessage);
        
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new LinkedInPostException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        return true;
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
            _logger.LogWarning(ex, "Scriban template rendering failed for LinkedIn scheduled item {Id}", scheduledItem.Id);
            return null;
        }
    }

    private string BuildLinkedInResponseErrorMessage(ShareResponse shareResponse)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Failed to post status update to LinkedIn. ");
        stringBuilder.AppendLine($"LinkedIn Status Code: '{shareResponse.Status}', ");
        stringBuilder.AppendLine($"LinkedIn Service Error Code: '{shareResponse.ServiceErrorCode}', ");
        stringBuilder.AppendLine($"LinkedIn Message: '{shareResponse.Message}'. ");
        return stringBuilder.ToString();
    }
}
