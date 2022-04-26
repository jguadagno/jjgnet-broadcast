using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with the Engagements of JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Route("[controller]")]
public class EngagementsController: ControllerBase
{
    private readonly IEngagementManager _engagementManager;
    private readonly ILogger<EngagementsController> _logger;

    /// <summary>
    /// Handles the interactions with Engagements
    /// </summary>
    /// <param name="engagementManager"></param>
    /// <param name="logger"></param>
    public EngagementsController(IEngagementManager engagementManager, ILogger<EngagementsController> logger)
    {
        _engagementManager = engagementManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets all the engagements
    /// </summary>
    /// <returns>A list of engagements</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="400">If the request is poorly formatted</response>            
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(List<Engagement>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Engagement>>> GetEngagementsAsync()
    {
        _logger.LogDebug("Call to Engagements.GetEngagements");

        return await _engagementManager.GetAllAsync();
    }

    /// <summary>
    /// Returns an engagement by it's identifier
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <returns>An <see cref="Engagement"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>            
    [HttpGet("{engagementId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Engagement>> GetEngagementAsync(int engagementId)
    {
        var engagement = await _engagementManager.GetAsync(engagementId);
        if (engagement is null)
        {
            return new NotFoundResult();
        }

        return engagement;
    }

    /// <summary>
    /// Saves an engagement
    /// </summary>
    /// <param name="engagement">An engagement</param>
    /// <returns>The engagement with the Url to view its details</returns>
    /// <response code="201">Returns the newly created item</response>
    /// <response code="400">If the item is null or there are data violations</response>            
    [HttpPost, HttpPut]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Engagement>> SaveEngagementAsync(Engagement engagement)
    {
        var savedEngagement = await _engagementManager.SaveAsync(engagement); 
        return CreatedAtRoute(nameof(GetEngagementAsync), new {engagementId = savedEngagement.Id}, savedEngagement);
    }
    
    /// <summary>
    /// Deletes the specified contact
    /// </summary>
    /// <param name="engagementId">The primary identifier for the engagement</param>
    /// <returns></returns>
    /// <response code="204">If the item was deleted</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>            
    [HttpDelete("{engagementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> DeleteContactAsync(int engagementId)
    {
        var wasDeleted = await _engagementManager.DeleteAsync(engagementId);
        if (wasDeleted)
        {
            return new NoContentResult();
        }
        return new NotFoundResult();
    }
    
    [HttpGet("{engagementId:int}/talks")]
    public async Task<ActionResult<List<Talk>>> GetTalksForEngagementAsync(int engagementId)
    {
        return await _engagementManager.GetTalksForEngagementAsync(engagementId);
    }
    
    [HttpPost("{engagementId:int}/talks/{talkId:int}")]
    [HttpPut("{engagementId:int}/talks/{talkId:int}")]
    public async Task<ActionResult<Talk>> SaveTalkAsync(Talk talk)
    {
        var savedTalk = await _engagementManager.SaveTalkAsync(talk);
        return CreatedAtRoute(nameof(GetTalkAsync), new { engagementId = talk.EngagementId, talkId = talk.Id }, savedTalk);
    }
    
    [HttpGet("{engagementId:int}/talks/{talkId:int}")]
    public async Task<Talk> GetTalkAsync(int engagementId, int talkId)
    {
        return await _engagementManager.GetTalkAsync(talkId);
    }
    
    // /engagements/{id}/talks/{id} DELETE
    [HttpDelete("{engagementId:int}/talks/{talkId:int}")]
    public async Task<ActionResult<bool>> DeleteTalkAsync(int engagementId, int talkId)
    {
        var wasDeleted =  await _engagementManager.RemoveTalkFromEngagementAsync(talkId);
        
        if (wasDeleted)
        {
            return new NoContentResult();
        }
        return new NotFoundResult();
    }
}