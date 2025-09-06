using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostText
{
    private readonly ILinkedInManager _linkedInManager;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<PostText> _logger;
    
    public PostText(ILinkedInManager linkedInManager, TelemetryClient telemetryClient, ILogger<PostText> logger)
    {
        _linkedInManager = linkedInManager;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    [Function("linkedin_post_text")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.LinkedInPostText)]
        LinkedInPostText linkedInPostText)
    {
        var linkedInShareId = await _linkedInManager.PostShareText(linkedInPostText.AccessToken, linkedInPostText.AuthorId, linkedInPostText.Text);
        
        if (!string.IsNullOrEmpty(linkedInShareId))
        {
            _telemetryClient.TrackEvent(Constants.Metrics.LinkedInPostText, new Dictionary<string, string>
            {
                {"linkedInShareId", linkedInShareId},
                {"text", linkedInPostText.Text}
            });
        }
    }
}