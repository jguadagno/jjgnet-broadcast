using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostLink(ILinkedInManager linkedInManager, ILogger<PostLink> logger)
{
    [Function(ConfigurationFunctionNames.LinkedInPostLink)]
    public async Task Run(
        [QueueTrigger(Queues.LinkedInPostLink)]
        LinkedInPostLink linkedInPostLink)
    {
        var linkedInShareId = await linkedInManager.PostShareTextAndLink(linkedInPostLink.AccessToken, linkedInPostLink.AuthorId,
            linkedInPostLink.Text, linkedInPostLink.LinkUrl, linkedInPostLink.Title, linkedInPostLink.Description);
        
        if (!string.IsNullOrEmpty(linkedInShareId))
        {
            var properties = new Dictionary<string, string>
            {
                {"linkedInShareId", linkedInShareId},
                {"title", linkedInPostLink.Title},
                {"text", linkedInPostLink.Text}, 
                {"url", linkedInPostLink.LinkUrl}
            };
            logger.LogCustomEvent(Metrics.LinkedInPostLink, properties);
        }
    }
}