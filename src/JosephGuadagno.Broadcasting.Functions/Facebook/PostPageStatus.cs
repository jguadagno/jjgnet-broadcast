using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Functions.Facebook.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class PostPageStatus
{
    private readonly ISettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostPageStatus> _logger;

    private const string StatusUrl = "https://graph.facebook.com/{page_id}/feed?message={message}&link={link}&access_token={access_token}";

    public PostPageStatus(ISettings settings, HttpClient httpClient, ILogger<PostPageStatus> logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
    }
        
    [FunctionName("facebook_post_status_to_page")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.FacebookPostStatusToPage)]
        Domain.Models.Messages.FacebookPostStatus facebookPostStatus)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.FacebookPostPageStatus, startedAt);
        
        var url = StatusUrl.Replace("{page_id}", _settings.FacebookPageId)
            .Replace("{message}", facebookPostStatus.StatusText)
            .Replace("{link}", facebookPostStatus.LinkUri)
            .Replace("{access_token}", _settings.FacebookPageAccessToken);

        var response = await _httpClient.PostAsync(url,null);
        try
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var postStatusResponse = JsonSerializer.Deserialize<PostStatusResponse>(content);

                if (response.IsSuccessStatusCode && postStatusResponse is not null)
                {
                    _logger.LogDebug("Successfully posted the status: '{Id}'", postStatusResponse.Id);
                }
                else
                {
                    if (postStatusResponse is null)
                    {
                        _logger.LogError("Failed to post status. Reason '{ReasonPhrase}'", response.ReasonPhrase);
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to post status.  Error Code: {Error.Code}, Subcode: {Error.ErrorSubcode}, Message: '{Error.Message}'",
                            postStatusResponse.Error.Code, postStatusResponse.Error.ErrorSubcode,
                            postStatusResponse.Error.Message);
                    }
                }
            }
            else
            {
                _logger.LogError("Failed to post status. Could not deserialize the response (Response was {StatusCode})", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to post status. Exception: {ExceptionMessage}", e.Message, response);
        }
    }
}