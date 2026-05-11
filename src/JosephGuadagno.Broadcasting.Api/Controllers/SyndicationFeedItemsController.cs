using AutoMapper;
using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with the Syndication Feed Sources of JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class SyndicationFeedItemsController : ControllerBase
{
    private readonly ISyndicationFeedItemManager _SyndicationFeedItemManager;
    private readonly ILogger<SyndicationFeedItemsController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with Syndication Feed Sources
    /// </summary>
    /// <param name="SyndicationFeedItemManager"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public SyndicationFeedItemsController(
        ISyndicationFeedItemManager SyndicationFeedItemManager,
        ILogger<SyndicationFeedItemsController> logger,
        IMapper mapper)
    {
        _SyndicationFeedItemManager = SyndicationFeedItemManager;
        _logger = logger;
        _mapper = mapper;
    }


    /// <summary>
    /// Gets all the syndication feed sources
    /// </summary>
    /// <returns>A list of syndication feed sources</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="400">If the request is poorly formatted</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<SyndicationFeedItemResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<SyndicationFeedItemResponse>>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        if (page < 1) page = Pagination.DefaultPage;
        if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;

        PagedResult<SyndicationFeedItem> result;
        if (User.IsSiteAdministrator())
        {
            result = await _SyndicationFeedItemManager.GetAllAsync(page, pageSize, sortBy, sortDescending, filter);
        }
        else
        {
            var ownerOid = User.GetOwnerOid();
            result = await _SyndicationFeedItemManager.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter);
        }

        var items = _mapper.Map<List<SyndicationFeedItemResponse>>(result.Items);
        return new PagedResponse<SyndicationFeedItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Returns a syndication feed source by its identifier
    /// </summary>
    /// <param name="id">The identifier of the syndication feed source</param>
    /// <returns>A <see cref="SyndicationFeedItem"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="400">If the request is poorly formatted</response>
    /// <response code="403">If the current user does not own this resource</response>
    /// <response code="404">If the requested id was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ActionName(nameof(GetSyndicationFeedItemAsync))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SyndicationFeedItemResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyndicationFeedItemResponse>> GetSyndicationFeedItemAsync(int id)
    {
        var source = await _SyndicationFeedItemManager.GetAsync(id);
        if (source is null)
            return NotFound();

        if (!User.IsSiteAdministrator() && source.CreatedByEntraOid != User.GetOwnerOid())
        {
            return Forbid();
        }

        return Ok(_mapper.Map<SyndicationFeedItemResponse>(source));
    }

    /// <summary>
    /// Creates a syndication feed source
    /// </summary>
    /// <param name="request">The syndication feed source data to create</param>
    /// <returns>The newly created syndication feed source with the Url to view its details</returns>
    /// <response code="201">Returns the newly created syndication feed source</response>
    /// <response code="400">If the request is null or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SyndicationFeedItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyndicationFeedItemResponse>> CreateSyndicationFeedItemAsync(SyndicationFeedItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateSyndicationFeedItemAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var source = _mapper.Map<SyndicationFeedItem>(request);
        source.CreatedByEntraOid = User.GetOwnerOid();
        source.AddedOn = DateTimeOffset.UtcNow;
        source.LastUpdatedOn = DateTimeOffset.UtcNow;

        var result = await _SyndicationFeedItemManager.SaveAsync(source);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("SyndicationFeedItem created with Id {SourceId}", result.Value.Id);
            return CreatedAtAction(nameof(GetSyndicationFeedItemAsync), new { id = result.Value.Id },
                _mapper.Map<SyndicationFeedItemResponse>(result.Value));
        }

        return Problem("Failed to create the syndication feed source");
    }

    /// <summary>
    /// Deletes the specified syndication feed source
    /// </summary>
    /// <param name="id">The primary identifier for the syndication feed source</param>
    /// <returns></returns>
    /// <response code="204">If the item was deleted</response>
    /// <response code="400">If the request is poorly formatted</response>
    /// <response code="403">If the current user does not own this resource</response>
    /// <response code="404">If the requested id was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteSyndicationFeedItemAsync(int id)
    {
        var source = await _SyndicationFeedItemManager.GetAsync(id);
        if (source is null)
        {
            _logger.LogWarning("SyndicationFeedItem {SourceId} not found for deletion", id);
            return NotFound();
        }

        if (!User.IsSiteAdministrator() && source.CreatedByEntraOid != User.GetOwnerOid())
        {
            return Forbid();
        }

        var wasDeleted= await _SyndicationFeedItemManager.DeleteAsync(id);
        if (wasDeleted.IsSuccess)
        {
            _logger.LogInformation("SyndicationFeedItem {SourceId} deleted successfully", id);
            return NoContent();
        }

        _logger.LogWarning("SyndicationFeedItem {SourceId} deletion failed", id);
        return NotFound();
    }
}
