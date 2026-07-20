using AutoMapper;
using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with the YouTube Sources of JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Tags("YouTube Items")]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class YouTubeItemsController : ControllerBase
{
    private readonly IYouTubeItemManager _youTubeItemManager;
    private readonly ILogger<YouTubeItemsController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with YouTube Sources
    /// </summary>
    /// <param name="youTubeItemManager"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public YouTubeItemsController(
        IYouTubeItemManager youTubeItemManager,
        ILogger<YouTubeItemsController> logger,
        IMapper mapper)
    {
        _youTubeItemManager = youTubeItemManager;
        _logger = logger;
        _mapper = mapper;
    }


    /// <summary>
    /// Gets all YouTube sources
    /// </summary>
    /// <returns>A list of YouTube sources</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<YouTubeItemResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<YouTubeItemResponse>>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        if (page < 1) page = Pagination.DefaultPage;
        if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;

        PagedResult<YouTubeItem> result;
        if (User.IsSiteAdministrator())
        {
            result = await _youTubeItemManager.GetAllAsync(page, pageSize, sortBy, sortDescending, filter);
        }
        else
        {
            var ownerOid = User.GetOwnerOid();
            result = await _youTubeItemManager.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter);
        }

        var items = _mapper.Map<List<YouTubeItemResponse>>(result.Items);
        return new PagedResponse<YouTubeItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Returns a YouTube source by its identifier
    /// </summary>
    /// <param name="id">The identifier of the YouTube source</param>
    /// <returns>A <see cref="YouTubeItem"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    /// <response code="403">If the current user does not own this resource</response>
    /// <response code="404">If the requested id was not found</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ActionName(nameof(GetYouTubeItemAsync))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(YouTubeItemResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<YouTubeItemResponse>> GetYouTubeItemAsync(int id)
    {
        var source = await _youTubeItemManager.GetAsync(id);
        if (source is null)
            return NotFound();

        if (!User.IsSiteAdministrator() && source.CreatedByEntraOid != User.GetOwnerOid())
            return Forbid();

        return Ok(_mapper.Map<YouTubeItemResponse>(source));
    }

    /// <summary>
    /// Creates a YouTube source
    /// </summary>
    /// <param name="request">The YouTube source data to create</param>
    /// <returns>The newly created YouTube source with the Url to view its details</returns>
    /// <response code="201">Returns the newly created YouTube source</response>
    /// <response code="400">If the request is null or there are data violations</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(YouTubeItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<YouTubeItemResponse>> CreateYouTubeItemAsync(YouTubeItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateYouTubeItemAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var source = _mapper.Map<YouTubeItem>(request);
        source.CreatedByEntraOid = User.GetOwnerOid();
        source.AddedOn = DateTimeOffset.UtcNow;
        source.LastUpdatedOn = DateTimeOffset.UtcNow;

        var result = await _youTubeItemManager.SaveAsync(source);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("YouTubeItem created with Id {YouTubeItemId}", result.Value.Id);
            return CreatedAtAction(nameof(GetYouTubeItemAsync), new { id = result.Value.Id },
                _mapper.Map<YouTubeItemResponse>(result.Value));
        }

        return Problem("Failed to create the YouTube source");
    }

    /// <summary>
    /// Deletes the specified YouTube source
    /// </summary>
    /// <param name="id">The primary identifier for the YouTube source</param>
    /// <returns></returns>
    /// <response code="204">If the item was deleted</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    /// <response code="403">If the current user does not own this resource</response>
    /// <response code="404">If the requested id was not found</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteYouTubeItemAsync(int id)
    {
        var source = await _youTubeItemManager.GetAsync(id);
        if (source is null)
        {
            _logger.LogWarning("YouTubeItem {YouTubeItemId} not found for deletion", id);
            return NotFound();
        }

        if (!User.IsSiteAdministrator() && source.CreatedByEntraOid != User.GetOwnerOid())
            return Forbid();

        var wasDeleted= await _youTubeItemManager.DeleteAsync(id);
        if (wasDeleted.IsSuccess)
        {
            _logger.LogInformation("YouTubeItem {YouTubeItemId} deleted successfully", id);
            return NoContent();
        }

        _logger.LogWarning("YouTubeItem {YouTubeItemId} deletion failed", id);
        return NotFound();
    }
}
