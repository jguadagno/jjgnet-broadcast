using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.Facebook;

public class PostPageStatus
{
    private readonly IFacebookManager _facebookManager;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<PostPageStatus> _logger;

    public PostPageStatus(IFacebookManager facebookManager, TelemetryClient telemetryClient, ILogger<PostPageStatus> logger)
    {
        _facebookManager = facebookManager;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
        
    [Function("facebook_post_status_to_page")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.FacebookPostStatusToPage)]
        Domain.Models.Messages.FacebookPostStatus facebookPostStatus)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.FacebookPostPageStatus, startedAt);

        try
        {
            var pageId = await _facebookManager.PostMessageAndLinkToPage(facebookPostStatus.StatusText, facebookPostStatus.LinkUri);

            if (!string.IsNullOrEmpty(pageId))
            {
                _telemetryClient.TrackEvent(Constants.Metrics.FacebookPostPageStatus, new Dictionary<string, string>
                {
                    {"statusText", facebookPostStatus.StatusText}, 
                    {"url", facebookPostStatus.LinkUri},
                });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to post status. Exception: {ExceptionMessage}", e.Message);
        }
    }
}