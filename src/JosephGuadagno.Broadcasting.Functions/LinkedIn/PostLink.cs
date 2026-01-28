using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostLink
{
    private readonly ILinkedInManager _linkedInManager;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<PostLink> _logger;
    
    public PostLink(ILinkedInManager linkedInManager, TelemetryClient telemetryClient, ILogger<PostLink> logger)
    {
        _linkedInManager = linkedInManager;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    [Function(Constants.ConfigurationFunctionNames.LinkedInPostLink)]
    public async Task Run(
        [QueueTrigger(Constants.Queues.LinkedInPostLink)]
        LinkedInPostLink linkedInPostLink)
    {
        var linkedInShareId = await _linkedInManager.PostShareTextAndLink(linkedInPostLink.AccessToken, linkedInPostLink.AuthorId,
            linkedInPostLink.Text, linkedInPostLink.LinkUrl, linkedInPostLink.Title, linkedInPostLink.Description);
        
        if (!string.IsNullOrEmpty(linkedInShareId))
        {
            _telemetryClient.TrackEvent(Constants.Metrics.LinkedInPostLink, new Dictionary<string, string>
            {
                {"linkedInShareId", linkedInShareId},
                
                {"title", linkedInPostLink.Title},
                {"text", linkedInPostLink.Text}, 
                {"url", linkedInPostLink.LinkUrl}
            });
        }
    }
}