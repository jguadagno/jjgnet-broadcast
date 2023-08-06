using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class PostPageStatus
{
    private readonly ISettings _settings;
    private readonly IFacebookManager _facebookManager;
    private readonly ILogger<PostPageStatus> _logger;

    private const string StatusUrl = "https://graph.facebook.com/{page_id}/feed?message={message}&link={link}&access_token={access_token}";

    public PostPageStatus(IFacebookManager facebookManager, ISettings settings, ILogger<PostPageStatus> logger)
    {
        _facebookManager = facebookManager;
        _settings = settings;
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

        try
        {
            _logger.LogTrace("Posting to Facebook Page: PageId: `{SettingsFacebookPageId}\', Token: `{SettingsFacebookPageAccessToken}\'", _settings.FacebookPageId, _settings.FacebookPageAccessToken);
            var pageId = await _facebookManager.PostMessageAndLinkToPage(_settings.FacebookPageId, facebookPostStatus.StatusText, facebookPostStatus.LinkUri, _settings.FacebookPageAccessToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to post status. Exception: {ExceptionMessage}", e.Message);
        }
    }
}