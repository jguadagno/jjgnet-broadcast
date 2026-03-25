using JosephGuadagno.Broadcasting.Api.Dtos;
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
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of engagements</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="400">If the request is poorly formatted</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response> 
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(PagedResponse<EngagementResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<EngagementResponse>>> GetEngagementsAsync(int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.List, Domain.Scopes.Engagements.All);

        // TODO: Move paging to the data store
        var allEngagements = await _engagementManager.GetAllAsync();
        var totalCount = allEngagements.Count;
        var items = allEngagements
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<EngagementResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(EngagementResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EngagementResponse>> GetEngagementAsync(int engagementId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.View, Domain.Scopes.Engagements.All);

        var engagement = await _engagementManager.GetAsync(engagementId);
        if (engagement is null)
            return NotFound();
        return Ok(ToResponse(engagement));
    }

    /// <summary>
    /// Creates an engagement
    /// </summary>
    /// <param name="request">The engagement data to create</param>
    /// <returns>The newly created engagement with the Url to view its details</returns>
    /// <response code="201">Returns the newly created engagement</response>
    /// <response code="400">If the engagement is null or there are data violations</response>     
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>       
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(EngagementResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EngagementResponse>> CreateEngagementAsync(EngagementRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Modify, Domain.Scopes.Engagements.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateEngagementAsync called with invalid model state");
            return BadRequest(ModelState);    
        }

        var engagement = ToModel(request);
        var savedEngagement = await _engagementManager.SaveAsync(engagement);
        if (savedEngagement != null)
        {
            _logger.LogInformation("Engagement created with Id {EngagementId}", savedEngagement.Id);
            return CreatedAtAction(nameof(GetEngagementAsync), new { engagementId = savedEngagement.Id },
                ToResponse(savedEngagement));
        }

        return Problem("Failed to create the engagement");
    }

    /// <summary>
    /// Updates an existing engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement to update</param>
    /// <param name="request">The updated engagement data</param>
    /// <returns>The updated engagement</returns>
    /// <response code="200">Returns the updated engagement</response>
    /// <response code="400">If the engagement is null, the id does not match, or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{engagementId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(EngagementResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EngagementResponse>> UpdateEngagementAsync(int engagementId, EngagementRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Modify, Domain.Scopes.Engagements.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateEngagementAsync called with invalid model state");
            return BadRequest(ModelState);    
        }

        var engagement = ToModel(request, engagementId);
        var savedEngagement = await _engagementManager.SaveAsync(engagement);
        if (savedEngagement != null)
        {
            _logger.LogInformation("Engagement updated with Id {EngagementId}", savedEngagement.Id);
            return Ok(ToResponse(savedEngagement));
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
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Delete, Domain.Scopes.Engagements.All);
        
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
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of talks</returns>
    /// <response code="200">Upon success</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{engagementId:int}/talks")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(PagedResponse<TalkResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<TalkResponse>>> GetTalksForEngagementAsync(int engagementId, int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.List, Domain.Scopes.Talks.All);
        // TODO: Move paging to the data store
        var allTalks = await _engagementManager.GetTalksForEngagementAsync(engagementId);
        var totalCount = allTalks.Count;
        var items = allTalks
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<TalkResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    
    /// <summary>
    /// Creates a talk for an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="request">The talk data to create</param>
    /// <returns>The newly created talk with the Url to view its details</returns>
    /// <response code="201">Returns the newly created talk</response>
    /// <response code="400">If the data provided is null or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>      
    [HttpPost("{engagementId:int}/talks")]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(TalkResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TalkResponse>> CreateTalkAsync(int engagementId, TalkRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Modify, Domain.Scopes.Talks.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateTalkAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var talk = ToModel(request, engagementId);
        var savedTalk = await _engagementManager.SaveTalkAsync(talk);
        if (savedTalk != null)
        {
            _logger.LogInformation("Talk created with Id {TalkId} for Engagement {EngagementId}", savedTalk.Id, engagementId);
            return CreatedAtAction(nameof(GetTalkAsync), new { engagementId = engagementId, talkId = savedTalk.Id },
                ToResponse(savedTalk));
        }

        return Problem("Failed to create the talk");
    }

    /// <summary>
    /// Updates an existing talk for an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="talkId">The identifier of the talk to update</param>
    /// <param name="request">The updated talk data</param>
    /// <returns>The updated talk</returns>
    /// <response code="200">Returns the updated talk</response>
    /// <response code="400">If the data provided is null, the id does not match, or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{engagementId:int}/talks/{talkId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(TalkResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TalkResponse>> UpdateTalkAsync(int engagementId, int talkId, TalkRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Modify, Domain.Scopes.Talks.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateTalkAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var talk = ToModel(request, engagementId, talkId);
        var savedTalk = await _engagementManager.SaveTalkAsync(talk);
        if (savedTalk != null)
        {
            _logger.LogInformation("Talk updated with Id {TalkId} for Engagement {EngagementId}", savedTalk.Id, engagementId);
            return Ok(ToResponse(savedTalk));
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
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(TalkResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ActionName(nameof(GetTalkAsync))]
    public async Task<ActionResult<TalkResponse>> GetTalkAsync(int engagementId, int talkId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.View, Domain.Scopes.Talks.All);
        
        var talk = await _engagementManager.GetTalkAsync(talkId);
        if (talk is null)
            return NotFound();
        return Ok(ToResponse(talk));
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
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.Delete, Domain.Scopes.Talks.All);
        
        var wasDeleted =  await _engagementManager.RemoveTalkFromEngagementAsync(talkId);
        
        if (wasDeleted)
        {
            _logger.LogInformation("Talk {TalkId} deleted from Engagement {EngagementId}", talkId, engagementId);
            return new NoContentResult();
        }
        _logger.LogWarning("Talk {TalkId} not found for deletion in Engagement {EngagementId}", talkId, engagementId);
        return new NotFoundResult();
    }

    // TODO: Move to a Automapper profile
    private static EngagementResponse ToResponse(Engagement e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Url = e.Url,
        StartDateTime = e.StartDateTime,
        EndDateTime = e.EndDateTime,
        TimeZoneId = e.TimeZoneId,
        Comments = e.Comments,
        Talks = e.Talks?.Select(ToResponse).ToList(),
        CreatedOn = e.CreatedOn,
        LastUpdatedOn = e.LastUpdatedOn
    };

    // TODO: Move to a Automapper profile
    private static Engagement ToModel(EngagementRequest r, int id = 0) => new()
    {
        Id = id,
        Name = r.Name,
        Url = r.Url,
        StartDateTime = r.StartDateTime,
        EndDateTime = r.EndDateTime,
        TimeZoneId = r.TimeZoneId,
        Comments = r.Comments
    };

    // TODO: Move to a Automapper profile
    private static TalkResponse ToResponse(Talk t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        UrlForConferenceTalk = t.UrlForConferenceTalk,
        UrlForTalk = t.UrlForTalk,
        StartDateTime = t.StartDateTime,
        EndDateTime = t.EndDateTime,
        TalkLocation = t.TalkLocation,
        Comments = t.Comments,
        EngagementId = t.EngagementId
    };

    // TODO: Move to a Automapper profile
    private static Talk ToModel(TalkRequest r, int engagementId, int id = 0) => new()
    {
        Id = id,
        Name = r.Name,
        UrlForConferenceTalk = r.UrlForConferenceTalk,
        UrlForTalk = r.UrlForTalk,
        StartDateTime = r.StartDateTime,
        EndDateTime = r.EndDateTime,
        TalkLocation = r.TalkLocation,
        Comments = r.Comments,
        EngagementId = engagementId
    };
}