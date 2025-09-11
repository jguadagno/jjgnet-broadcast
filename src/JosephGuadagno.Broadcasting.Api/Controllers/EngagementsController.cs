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
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.List);
        #endif
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
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.View);
        #endif        
        var engagement = await _engagementManager.GetAsync(engagementId);

        return engagement;
    }

    /// <summary>
    /// Saves an engagement
    /// </summary>
    /// <param name="engagement">The engagement to save</param>
    /// <returns>The engagement with the Url to view its details</returns>
    /// <response code="200">Returns if the engagement was updated</response>
    /// <response code="201">Returns the newly created talk</response>
    /// <response code="400">If the talk is null or there are data violations</response>     
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>       
    [HttpPost, HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Engagement>> SaveEngagementAsync(Engagement engagement)
    {
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Modify);
        #endif       
        var savedEngagement = await _engagementManager.SaveAsync(engagement);
        if (savedEngagement != null)
        {
            return CreatedAtAction(nameof(GetEngagementAsync), new { engagementId = savedEngagement.Id },
                savedEngagement);
        }

        return Problem("Failed to save the engagement");
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
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Delete);
        #endif        
        var wasDeleted = await _engagementManager.DeleteAsync(engagementId);
        if (wasDeleted)
        {
            return new NoContentResult();
        }
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
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.List);
        #endif
        
        return await _engagementManager.GetTalksForEngagementAsync(engagementId);
    }
    
    /// <summary>
    /// Saves a talk
    /// </summary>
    /// <param name="talk">The talk to save</param>
    /// <returns>The talk with the Url to view its details</returns>
    /// <response code="200">Returns if the talk was successfully updated</response>
    /// <response code="201">Returns the newly created talk</response>
    /// <response code="400">If the data provided is null or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>      
    [HttpPost("{engagementId:int}/talks/")]
    [HttpPut("{engagementId:int}/talks/")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Talk))]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Talk))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Talk>> SaveTalkAsync(Talk talk)
    {
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Modify);
        #endif       
        
        var savedTalk = await _engagementManager.SaveTalkAsync(talk);
        if (savedTalk != null)
        {
            return CreatedAtAction(nameof(GetTalkAsync), new { engagementId = talk.EngagementId, talkId = talk.Id },
                savedTalk);
        }

        return Problem("Failed to save the talk");
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
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.View);
        #endif        
        
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
        #if DEBUG
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.All);
        #else
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Delete);
        #endif
        var wasDeleted =  await _engagementManager.RemoveTalkFromEngagementAsync(talkId);
        
        if (wasDeleted)
        {
            return new NoContentResult();
        }
        return new NotFoundResult();
    }
}