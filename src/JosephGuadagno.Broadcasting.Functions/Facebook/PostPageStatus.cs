using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook
{
    public class PostPageStatus
    {
        private readonly ISettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PostPageStatus> _logger;
        
        private readonly string _statusUrl = "https://graph.facebook.com/{page_id}/feed?message={message}&link={link}&access_token={access_token}";
        
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
            var url = _statusUrl.Replace("{page_id}", _settings.FacebookPageId)
                .Replace("{message}", facebookPostStatus.StatusText)
                .Replace("{link}", facebookPostStatus.LinkUri)
                .Replace("{access_token}", _settings.FacebookPageAccessToken);

            var response = await _httpClient.PostAsync(url,null);

            if (response != null)
            {
                var content = await response.Content.ReadAsStringAsync();
                var postStatusResponse =
                    JsonSerializer.Deserialize<Models.PostStatusResponse>(content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogDebug($"Successfully posted the status: '{postStatusResponse.Id}'");
                }
                else
                {
                    _logger.LogError(
                        $"Failed to post status.  Error Code: {postStatusResponse.Error.Code}, Subcode: {postStatusResponse.Error.ErrorSubcode}, Message: '{postStatusResponse.Error.Message}'");
                }
            }
            else
            {
                _logger.LogError("Failed to post status. Could not deserialize the response.");
            }
        }
    }
}
