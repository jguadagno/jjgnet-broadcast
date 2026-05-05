using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
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

    public async Task<string?> PublishAsync(SocialMediaPublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);

        if (!string.IsNullOrEmpty(request.ImageUrl))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.LinkUrl);
            return await PostMessageLinkAndPictureToPage(request.Text, request.LinkUrl!, request.ImageUrl);
        }

        if (!string.IsNullOrEmpty(request.LinkUrl))
        {
            return await PostMessageAndLinkToPage(request.Text, request.LinkUrl);
        }

        return await PostMessageToPage(request.Text);
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

        return await PostMessageInternalAsync(message, link);
    }

    /// <inheritdoc />
    public async Task<string> PostMessageLinkAndPictureToPage(string message, string link, string picture)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(link))
            throw new ArgumentNullException(nameof(link));
        if (string.IsNullOrEmpty(picture))
            throw new ArgumentNullException(nameof(picture));

        return await PostMessageInternalAsync(message, link, picture);
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

            _logger.LogTrace("Url: `{Url}`", RedactSensitiveQueryParams(url));
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
                
                _logger.LogError("Failed to refresh the token. Could not deserialize the response. Response length: {Length} bytes.", content?.Length ?? 0);
                throw new FacebookPostException(
                    $"Failed to refresh the token. Could not deserialize the response. Response length: {content?.Length ?? 0} bytes.");
            }
            
            _logger.LogError(
                "Failed to refresh the token. Response status code was not successful. StatusCode: '{StatusCode}', ReasonPhrase: '{ReasonPhrase}'",
                response.StatusCode, response.ReasonPhrase);
            throw new FacebookPostException(
                $"Failed to refresh the token. Response status code was not successful. StatusCode: '{response.StatusCode}', ReasonPhrase: '{response.ReasonPhrase}'");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed refresh the token. Exception: {ExceptionMessage}", ex.Message);
            throw;
        }
    }

    private static readonly Regex SensitiveQueryParamPattern =
        new(@"(access_token|client_secret|fb_exchange_token)=[^&]*", RegexOptions.Compiled);

    private async Task<string> PostMessageToPage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        return await PostMessageInternalAsync(message);
    }

    private async Task<string> PostMessageInternalAsync(string message, string? link = null, string? picture = null)
    {
        try
        {
            var urlBuilder = new StringBuilder(GraphApiRoot)
                .Append(_facebookApplicationSettings.PageId)
                .Append("/feed?message=")
                .Append(System.Web.HttpUtility.UrlEncode(message));

            if (!string.IsNullOrEmpty(link))
            {
                urlBuilder.Append("&link=").Append(link);
            }

            if (!string.IsNullOrEmpty(picture))
            {
                urlBuilder.Append("&picture=").Append(System.Web.HttpUtility.UrlEncode(picture));
            }

            urlBuilder.Append("&access_token=").Append(_facebookApplicationSettings.PageAccessToken);

            var url = urlBuilder.ToString();

            _logger.LogTrace("Url: `{Url}`", RedactSensitiveQueryParams(url));
            var response = await _httpClient.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var postStatusResponse = JsonSerializer.Deserialize<PostStatusResponse>(content);
                if (postStatusResponse is not null)
                {
                    if (!string.IsNullOrEmpty(postStatusResponse.Id))
                    {
                        _logger.LogDebug("Successfully posted status. Id: '{Id}'", postStatusResponse.Id);
                        return postStatusResponse.Id;
                    }

                    if (postStatusResponse.Error is not null)
                    {
                        _logger.LogError(
                            "Failed to post status. Message: '{Message}', Type: '{ErrorType}', Code: '{Code}', Sub Code: '{SubCode}', TraceId: '{TraceId}",
                            postStatusResponse.Error.Message,
                            postStatusResponse.Error.Type,
                            postStatusResponse.Error.Code,
                            postStatusResponse.Error.SubCode,
                            postStatusResponse.Error.FacebookTraceId);
                        throw new FacebookPostException($"Failed to post status. Reason {postStatusResponse.Error.Message}");
                    }

                    _logger.LogError("Failed to post status. Could not determine the reason. Response: {Response}", content);
                    throw new FacebookPostException($"Failed to post status. Could not determine the reason. Response {content}");
                }

                _logger.LogError("Failed to post status. Could not deserialized the response. Response: {Response}", content);
                throw new FacebookPostException($"Failed to post status. Could not deserialized the response. Response {content}");
            }

            _logger.LogError(
                "Failed to post status. Response status code was not successful. StatusCode: '{StatusCode}', ReasonPhrase: '{ReasonPhrase}'",
                response.StatusCode,
                response.ReasonPhrase);
            throw new FacebookPostException(
                $"Failed to post status. Response status code was not successful. StatusCode: '{response.StatusCode}', ReasonPhrase: '{response.ReasonPhrase}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post status. Exception: {ExceptionMessage}", ex.Message);
            throw;
        }
    }

    private static string RedactSensitiveQueryParams(string url) =>
        SensitiveQueryParamPattern.Replace(url, "$1=***REDACTED***");
}
