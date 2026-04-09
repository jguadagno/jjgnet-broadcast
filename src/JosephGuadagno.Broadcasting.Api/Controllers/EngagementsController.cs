using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
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
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
[IgnoreAntiforgeryToken]
public class EngagementsController: ControllerBase
{
    private readonly IEngagementManager _engagementManager;
    private readonly IEngagementSocialMediaPlatformDataStore _engagementSocialMediaPlatformDataStore;
    private readonly ILogger<EngagementsController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with Engagements
    /// </summary>
    /// <param name="engagementManager"></param>
    /// <param name="engagementSocialMediaPlatformDataStore"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public EngagementsController(
        IEngagementManager engagementManager,
        IEngagementSocialMediaPlatformDataStore engagementSocialMediaPlatformDataStore,
        ILogger<EngagementsController> logger,
        IMapper mapper)
    {
        _engagementManager = engagementManager;
        _engagementSocialMediaPlatformDataStore = engagementSocialMediaPlatformDataStore;
        _logger = logger;
        _mapper = mapper;
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
    public async Task<ActionResult<PagedResponse<EngagementResponse>>> GetEngagementsAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.List, Domain.Scopes.Engagements.All);

        var result = await _engagementManager.GetAllAsync(page, pageSize);
        var items = _mapper.Map<List<EngagementResponse>>(result.Items);
        
        return new PagedResponse<EngagementResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
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
        return Ok(_mapper.Map<EngagementResponse>(engagement));
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

        var engagement = _mapper.Map<Engagement>(request);
        var result = await _engagementManager.SaveAsync(engagement);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("Engagement created with Id {EngagementId}", result.Value.Id);
            return CreatedAtAction(nameof(GetEngagementAsync), new { engagementId = result.Value.Id },
                _mapper.Map<EngagementResponse>(result.Value));
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

        var engagement = _mapper.Map<Engagement>(request);
        engagement.Id = engagementId;
        var result = await _engagementManager.SaveAsync(engagement);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("Engagement updated with Id {EngagementId}", result.Value.Id);
            return Ok(_mapper.Map<EngagementResponse>(result.Value));
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
        if (wasDeleted.IsSuccess)
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
    public async Task<ActionResult<PagedResponse<TalkResponse>>> GetTalksForEngagementAsync(int engagementId, int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Talks.List, Domain.Scopes.Talks.All);
        var result = await _engagementManager.GetTalksForEngagementAsync(engagementId, page, pageSize);
        var items = _mapper.Map<List<TalkResponse>>(result.Items);
        
        return new PagedResponse<TalkResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
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

        var talk = _mapper.Map<Talk>(request);
        talk.EngagementId = engagementId;
        var result = await _engagementManager.SaveTalkAsync(talk);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("Talk created with Id {TalkId} for Engagement {EngagementId}", result.Value.Id, engagementId);
            return CreatedAtAction(nameof(GetTalkAsync), new { engagementId = engagementId, talkId = result.Value.Id },
                _mapper.Map<TalkResponse>(result.Value));
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

        var talk = _mapper.Map<Talk>(request);
        talk.EngagementId = engagementId;
        talk.Id = talkId;
        var result = await _engagementManager.SaveTalkAsync(talk);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("Talk updated with Id {TalkId} for Engagement {EngagementId}", result.Value.Id, engagementId);
            return Ok(_mapper.Map<TalkResponse>(result.Value));
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
        return Ok(_mapper.Map<TalkResponse>(talk));
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
        
        if (wasDeleted.IsSuccess)
        {
            _logger.LogInformation("Talk {TalkId} deleted from Engagement {EngagementId}", talkId, engagementId);
            return new NoContentResult();
        }
        _logger.LogWarning("Talk {TalkId} not found for deletion in Engagement {EngagementId}", talkId, engagementId);
        return new NotFoundResult();
    }

    // ==================================================================    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{engagementId:int}/platforms")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EngagementSocialMediaPlatformResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EngagementSocialMediaPlatformResponse>>> GetPlatformsForEngagementAsync(int engagementId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.View, Domain.Scopes.Engagements.All);

        var platforms = await _engagementSocialMediaPlatformDataStore.GetByEngagementIdAsync(engagementId);
        return Ok(_mapper.Map<List<EngagementSocialMediaPlatformResponse>>(platforms));
    }

    /// <summary>
    /// Adds a social media platform to an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="request">The platform association data</param>
    /// <returns>The newly created platform association</returns>
    /// <response code="201">Returns the newly created platform association</response>
    /// <response code="400">If the request is invalid or the platform could not be added</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost("{engagementId:int}/platforms")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EngagementSocialMediaPlatformResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EngagementSocialMediaPlatformResponse>> AddPlatformToEngagementAsync(
        int engagementId,
        EngagementSocialMediaPlatformRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Modify, Domain.Scopes.Engagements.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("AddPlatformToEngagementAsync called with invalid model state for engagement {EngagementId}", engagementId);
            return BadRequest(ModelState);
        }

        var esmp = _mapper.Map<EngagementSocialMediaPlatform>(request);
        esmp.EngagementId = engagementId;

        var result = await _engagementSocialMediaPlatformDataStore.AddAsync(esmp);
        if (result is null)
        {
            _logger.LogWarning("Failed to add platform {PlatformId} to engagement {EngagementId}", request.SocialMediaPlatformId, engagementId);
            return BadRequest("Failed to add platform to engagement");
        }

        _logger.LogInformation("Platform {PlatformId} added to engagement {EngagementId}", result.SocialMediaPlatformId, engagementId);
        return CreatedAtAction(
            nameof(GetPlatformsForEngagementAsync),
            new { engagementId },
            _mapper.Map<EngagementSocialMediaPlatformResponse>(result));
    }

    /// <summary>
    /// Removes a social media platform from an engagement
    /// </summary>
    /// <param name="engagementId">The identifier of the engagement</param>
    /// <param name="platformId">The identifier of the social media platform to remove</param>
    /// <returns></returns>
    /// <response code="204">If the platform was successfully removed</response>
    /// <response code="404">If the association was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpDelete("{engagementId:int}/platforms/{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemovePlatformFromEngagementAsync(int engagementId, int platformId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Engagements.Delete, Domain.Scopes.Engagements.All);

        var deleted = await _engagementSocialMediaPlatformDataStore.DeleteAsync(engagementId, platformId);
        if (!deleted)
        {
            _logger.LogWarning("Platform {PlatformId} not found on engagement {EngagementId} for deletion", platformId, engagementId);
            return NotFound();
        }

        _logger.LogInformation("Platform {PlatformId} removed from engagement {EngagementId}", platformId, engagementId);
        return NoContent();
    }
}