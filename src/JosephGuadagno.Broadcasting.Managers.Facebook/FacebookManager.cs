using System.Text.Json;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Facebook.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Facebook;

public class FacebookManager : IFacebookManager
{
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<FacebookManager> _logger;
    private readonly IFacebookApplicationSettings _facebookApplicationSettings;
    
    public FacebookManager(HttpClient httpClient, IFacebookApplicationSettings facebookApplicationSettings, ILogger<FacebookManager> logger)
    {
        _httpClient = httpClient;
        _facebookApplicationSettings = facebookApplicationSettings;
        _logger = logger;
    }

    /// <summary>
    /// Returns the Graph API Root with the version
    /// </summary>
    public string GraphApiRoot => _facebookApplicationSettings.GraphApiRootUrl + "/" + _facebookApplicationSettings.GraphApiVersion + "/";

    /// <summary>
    /// Posts a message with a link to a Facebook Page
    /// </summary>
    /// <param name="message">The message/Facebook status to post</param>
    /// <param name="link">The link for the post</param>
    /// <returns>The id of the newly created status</returns>
    public async Task<string> PostMessageAndLinkToPage(string message, string link)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }
        if (string.IsNullOrEmpty(link))
        {
            throw new ArgumentNullException(nameof(link));
        }

        try
        {
            var postToPageWithLinkUrl = GraphApiRoot + "{page_id}/feed?message={message}&link={link}&access_token={access_token}";
            
            var url = postToPageWithLinkUrl.Replace("{page_id}", _facebookApplicationSettings.PageId)
                .Replace("{message}",  System.Web.HttpUtility.UrlEncode(message))
                .Replace("{link}", link)
                .Replace("{access_token}", _facebookApplicationSettings.PageAccessToken);
        
            _logger.LogTrace("Url: `{Url}`", url);
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
                        _logger.LogDebug("Successfully posted status. Id: '{Id}'", postStatusResponse.Id);
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

    /// <summary>
    /// Refreshes the token
    /// </summary>
    /// <param name="tokenToRefresh">The token to refresh</param>
    /// <returns>Information about the <see cref="TokenInfo">token</see></returns>
    public async Task<TokenInfo> RefreshToken(string tokenToRefresh)
    {
        if (string.IsNullOrEmpty(tokenToRefresh))
        {
            throw new ArgumentNullException(nameof(tokenToRefresh));
        }

        try
        {
            var refreshTokenUrl = GraphApiRoot + "oauth/access_token?grant_type=fb_exchange_token&client_id={client_id}&client_secret={client_secret}&fb_exchange_token={fb_exchange_token}&set_token_expires_in_60_days=true";

            var url = refreshTokenUrl.Replace("{client_id}", _facebookApplicationSettings.AppId)
                .Replace("{client_secret}", _facebookApplicationSettings.AppSecret)
                .Replace("{fb_exchange_token}", tokenToRefresh);

            _logger.LogTrace("Url: `{Url}`", url);
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var statusResponse = JsonSerializer.Deserialize<TokenResponse>(content);
                if (statusResponse is not null)
                {
                    // Convert the response to a TokenInfo
                    var tokenInfo = new TokenInfo
                    {
                        AccessToken = statusResponse.AccessToken,
                        TokenType = statusResponse.TokenType,
                        ExpiresOn = DateTime.UtcNow.AddSeconds(statusResponse.ExpiresIn)
                    };
                    return tokenInfo;
                }
                
                _logger.LogError("Failed to refresh the token. Could not deserialized the response. Response: {Response}", content);
                throw new ApplicationException(
                    $"Failed to refresh the token. Could not deserialized the response. Response {content}");
            }
            
            _logger.LogError(
                "Failed to refresh the token. Response status code was not successful. StatusCode: '{StatusCode}', ReasonPhrase: '{ReasonPhrase}'",
                response.StatusCode, response.ReasonPhrase);
            throw new ApplicationException(
                $"Failed to refresh the token. Response status code was not successful. StatusCode: '{response.StatusCode}', ReasonPhrase: '{response.ReasonPhrase}'");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed refresh the token. Exception: {ExceptionMessage}", ex.Message);
            throw;
        }
    }
}