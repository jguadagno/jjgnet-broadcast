using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostLink
{
    private readonly ILinkedInManager _linkedInManager;
    private readonly ILogger<PostLink> _logger;
    
    public PostLink(ILinkedInManager linkedInManager, ILogger<PostLink> logger)
    {
        _linkedInManager = linkedInManager;
        _logger = logger;
    }
    
    [FunctionName("linkedin_post_link")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.LinkedInPostLink)]
        LinkedInPostLink linkedInPostLink)
    {
        await _linkedInManager.PostShareTextAndLink(linkedInPostLink.AccessToken, linkedInPostLink.AuthorId,
            linkedInPostLink.Text, linkedInPostLink.LinkUrl, linkedInPostLink.Title, linkedInPostLink.Description);
    }
}