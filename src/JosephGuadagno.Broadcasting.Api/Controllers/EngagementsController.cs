using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with the Engagements of JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
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
    /// <response code="401">If the current user was unauthorized to access this endpoint</response> 
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(List<Engagement>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<Engagement>>> GetEngagementsAsync()
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        // HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.List);
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
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>             
    [HttpGet("{engagementId:int}")]
    [ActionName(nameof(GetEngagementAsync))]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Engagement>> GetEngagementAsync(int engagementId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        // HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.View);

        var engagement = await _engagementManager.GetAsync(engagementId);
        return engagement;
    }

    /// <summary>
    /// Creates an engagement
    /// </summary>
    /// <param name="engagement">The engagement to create</param>
    /// <returns>The newly created engagement with the Url to view its details</returns>
    /// <response code="201">Returns the newly created engagement</response>
    /// <response code="400">If the engagement is null or there are data violations</response>     
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>       
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Engagement>> CreateEngagementAsync(Engagement engagement)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Modify);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateEngagementAsync called with invalid model state");
            return BadRequest(ModelState);    
        }
        
        var savedEngagement = await _engagementManager.SaveAsync(engagement);
        if (savedEngagement != null)
        {
            _logger.LogInformation("Engagement created with Id {EngagementId}", savedEngagement.Id);
            return CreatedAtAction(nameof(GetEngagementAsync), new { engagementId = savedEngagement.Id },
                savedEngagement);
        }

        return Problem("Failed to create the engagement");
    }

    /// <summary>
    /// Updates an existing engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to update</param>
    /// <param name="engagement">The updated engagement data</param>
    /// <returns>The updated engagement</returns>
    /// <response code="200">Returns the updated engagement</response>
    /// <response code="400">If the engagement is null, the id does not match, or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{engagementId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Engagement>> UpdateEngagementAsync(int engagementId, Engagement engagement)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Modify);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateEngagementAsync called with invalid model state");
            return BadRequest(ModelState);    
        }

        if (engagementId != engagement.Id)
        {
            return BadRequest("Route id must match the engagement Id.");
        }
        
        var savedEngagement = await _engagementManager.SaveAsync(engagement);
        if (savedEngagement != null)
        {
            _logger.LogInformation("Engagement updated with Id {EngagementId}", savedEngagement.Id);
            return Ok(savedEngagement);
        }

        return Problem("Failed to update the engagement");
    }
    
    /// <summary>
    /// Deletes the specified engagement
    /// </summary>
    /// <param name="engagementId">The primary identifier for the engagement</param>
    /// <returns></returns>
    /// <response code="204">If the item was deleted</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>            
    [HttpDelete("{engagementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> DeleteEngagementAsync(int engagementId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Delete);
        
        var wasDeleted = await _engagementManager.DeleteAsync(engagementId);
        if (wasDeleted)
        {
            _logger.LogInformation("Engagement {EngagementId} deleted successfully", engagementId);
            return new NoContentResult();
        }
        _logger.LogWarning("Engagement {EngagementId} not found for deletion", engagementId);
        return new NotFoundResult();
    }
    
    /// <summary>
    /// Gets the talks for a given engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <returns>A List&lt;<see cref="Talk"/>&gt;s</returns>
    /// <response code="200">Upon success</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{engagementId:int}/talks")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(List<Talk>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<Talk>>> GetTalksForEngagementAsync(int engagementId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.List);
        
        return await _engagementManager.GetTalksForEngagementAsync(engagementId);
    }
    
    /// <summary>
    /// Creates a talk for an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talk">The talk to create</param>
    /// <returns>The newly created talk with the Url to view its details</returns>
    /// <response code="201">Returns the newly created talk</response>
    /// <response code="400">If the data provided is null or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>      
    [HttpPost("{engagementId:int}/talks")]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Talk))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Talk>> CreateTalkAsync(int engagementId, Talk talk)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Modify);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateTalkAsync called with invalid model state");
            return BadRequest(ModelState);
        }
        
        var savedTalk = await _engagementManager.SaveTalkAsync(talk);
        if (savedTalk != null)
        {
            _logger.LogInformation("Talk created with Id {TalkId} for Engagement {EngagementId}", savedTalk.Id, engagementId);
            return CreatedAtAction(nameof(GetTalkAsync), new { engagementId = engagementId, talkId = savedTalk.Id },
                savedTalk);
        }

        return Problem("Failed to create the talk");
    }

    /// <summary>
    /// Updates an existing talk for an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk to update</param>
    /// <param name="talk">The updated talk data</param>
    /// <returns>The updated talk</returns>
    /// <response code="200">Returns the updated talk</response>
    /// <response code="400">If the data provided is null, the id does not match, or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{engagementId:int}/talks/{talkId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Talk))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Talk>> UpdateTalkAsync(int engagementId, int talkId, Talk talk)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Modify);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateTalkAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        if (talkId != talk.Id)
        {
            return BadRequest("Route id must match the talk Id.");
        }
        
        var savedTalk = await _engagementManager.SaveTalkAsync(talk);
        if (savedTalk != null)
        {
            _logger.LogInformation("Talk updated with Id {TalkId} for Engagement {EngagementId}", savedTalk.Id, engagementId);
            return Ok(savedTalk);
        }

        return Problem("Failed to update the talk");
    }
    
    /// <summary>
    /// Returns a talk by it's identifier
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk</param>
    /// <returns>An <see cref="Talk"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>   
    [HttpGet("{engagementId:int}/talks/{talkId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Talk))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ActionName(nameof(GetTalkAsync))]
    public async Task<Talk> GetTalkAsync(int engagementId, int talkId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.View);
        
        return await _engagementManager.GetTalkAsync(talkId);
    }
    
    /// <summary>
    /// Deletes the specified talk
    /// </summary>
    /// <param name="engagementId">The primary identifier for the engagement</param>
    /// <param name="talkId">The primary identifier for the talk</param>
    /// <returns></returns>
    /// <response code="204">If the item was deleted</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>   
    [HttpDelete("{engagementId:int}/talks/{talkId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> DeleteTalkAsync(int engagementId, int talkId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Delete);
        
        var wasDeleted =  await _engagementManager.RemoveTalkFromEngagementAsync(talkId);
        
        if (wasDeleted)
        {
            _logger.LogInformation("Talk {TalkId} deleted from Engagement {EngagementId}", talkId, engagementId);
            return new NoContentResult();
        }
        _logger.LogWarning("Talk {TalkId} not found for deletion in Engagement {EngagementId}", talkId, engagementId);
        return new NotFoundResult();
    }
}