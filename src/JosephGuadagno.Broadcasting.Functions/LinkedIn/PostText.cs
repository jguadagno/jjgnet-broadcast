using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

public class PostText
{
    private readonly ILinkedInManager _linkedInManager;
    private readonly ILogger<PostText> _logger;
    
    public PostText(ILinkedInManager linkedInManager, ILogger<PostText> logger)
    {
        _linkedInManager = linkedInManager;
        _logger = logger;
    }
    
    [Function("linkedin_post_text")]
    public async Task Run(
        [QueueTrigger(Constants.Queues.LinkedInPostText)]
        LinkedInPostText linkedInPostText)
    {
        await _linkedInManager.PostShareText(linkedInPostText.AccessToken, linkedInPostText.AuthorId, linkedInPostText.Text);
    }
}