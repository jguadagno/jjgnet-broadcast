using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn;

public class LinkedInManager : ILinkedInManager
{

    private const string LinkedInPostUrl = "https://api.linkedin.com/v2/ugcPosts";
    private const string LinkedInUserUrl = "https://api.linkedin.com/v2/me";
    private const string LinkedInAssetUrl = "https://api.linkedin.com/v2/assets?action=registerUpload";
    
    private const string LinkedInAuthorUrn = "urn:li:person:{0}";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkedInManager> _logger;
    
    public LinkedInManager(HttpClient httpClient, ILogger<LinkedInManager> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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
    /// <exception cref="HttpRequestException"></exception>
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
        throw new HttpRequestException($"Failed to post status update to LinkedIn: LinkedIn Status Code: '{linkedInResponse.ServiceErrorCode}', LinkedIn Message: '{linkedInResponse.Message}'");
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
    /// <exception cref="HttpRequestException"></exception>
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
        throw new HttpRequestException(BuildLinkedInResponseErrorMessage(linkedInResponse));

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
    /// <exception cref="ApplicationException"></exception>
    /// <exception cref="HttpRequestException"></exception>
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
        var uploadUrl = uploadResponse.Value.UploadMechanism.MediaUploadHttpRequest.UploadUrl;
        var wasFileUploadSuccessful = await UploadImage(accessToken, uploadUrl, image);

        if (!wasFileUploadSuccessful)
        {
            throw new ApplicationException("Failed to upload image to LinkedIn");
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
        throw new HttpRequestException(BuildLinkedInResponseErrorMessage(linkedInResponse));
    }

    private async Task<T> ExecuteGetAsync<T>(string url, string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add ("Authorization", $"Bearer {accessToken}");
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException(
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
            throw new HttpRequestException(
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
            // TODO: Custom Exception
            throw new HttpRequestException(
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
            // TODO: Custom Exception
            throw new ApplicationException("Could not deserialize the response from LinkedIn");
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
            throw new HttpRequestException(
                $"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
        }

        return true;
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