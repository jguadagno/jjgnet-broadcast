using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Models.Facebook;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public class FacebookManager : IFacebookManager
{
    private const string PostToPageWithLinkUrl = "https://graph.facebook.com/{page_id}/feed?message={message}&link={link}&access_token={access_token}";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<FacebookManager> _logger;
    
    public FacebookManager(HttpClient httpClient, ILogger<FacebookManager> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<string> PostMessageAndLinkToPage(string pageId, string message, string link, string accessToken)
    {
        if (string.IsNullOrEmpty(pageId))
        {
            throw new ArgumentNullException(nameof(pageId));
        }
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }
        if (string.IsNullOrEmpty(link))
        {
            throw new ArgumentNullException(nameof(link));
        }
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentNullException(nameof(accessToken));
        }

        try
        {
            var url = PostToPageWithLinkUrl.Replace("{page_id}", pageId)
                .Replace("{message}", message)
                .Replace("{link}", link)
                .Replace("{access_token}", accessToken);
        
            var response = await _httpClient.PostAsync(url,null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var postStatusResponse = JsonSerializer.Deserialize<PostStatusResponse>(content);
                if (postStatusResponse is not null)
                {
                    // Need to make sure the response has the PageId, if not, it will have the FacebookPostError populated
                    if (!string.IsNullOrEmpty(postStatusResponse.Id))
                    {
                        // We should be good
                        return postStatusResponse.Id;
                    }

                    // This is an error.  We should use the FacebookPostError to log and throw the error
                    if (postStatusResponse.Error is not null)
                    {
                        _logger.LogError(
                            "Failed to post status. Message: '{Message}', Type: '{ErrorType}', Code: '{Code}', Sub Code: '{SubCode}', TraceId: '{TraceId}",
                            postStatusResponse.Error.Message,
                            postStatusResponse.Error.Type, postStatusResponse.Error.Code,
                            postStatusResponse.Error.SubCode, postStatusResponse.Error.FacebookTraceId);
                        // TODO: Turn into a custom exception (FacebookPostException)
                        throw new ApplicationException(
                            $"Failed to post status. Reason {postStatusResponse.Error.Message}");
                    }
                    
                    // If we made it here, there was an error but the response did not have the FacebookPostError populated
                    _logger.LogError("Failed to post status. Could not determine the reason. Response: {Response}", content);
                    throw new ApplicationException(
                        $"Failed to post status. Could not determine the reason. Response {content}");
                }
                
                _logger.LogError("Failed to post status. Could not deserialized the response. Response: {Response}", content);
                throw new ApplicationException(
                    $"Failed to post status. Could not deserialized the response. Response {content}");
                
            }
            
            _logger.LogError(
                "Failed to post status. Response status code was not successful. StatusCode: '{StatusCode}', ReasonPhrase: '{ReasonPhrase}'",
                response.StatusCode, response.ReasonPhrase);
            throw new ApplicationException(
                $"Failed to post status. Response status code was not successful. StatusCode: '{response.StatusCode}', ReasonPhrase: '{response.ReasonPhrase}'");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post status. Exception: {ExceptionMessage}",  ex.Message);
            throw;
        }
    }
}