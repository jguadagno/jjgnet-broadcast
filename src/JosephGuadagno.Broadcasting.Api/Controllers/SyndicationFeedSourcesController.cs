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
public class SyndicationFeedSourcesController : ControllerBase
{
    private readonly ISyndicationFeedSourceManager _syndicationFeedSourceManager;
    private readonly ILogger<SyndicationFeedSourcesController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with Syndication Feed Sources
    /// </summary>
    /// <param name="syndicationFeedSourceManager"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    public SyndicationFeedSourcesController(
        ISyndicationFeedSourceManager syndicationFeedSourceManager,
        ILogger<SyndicationFeedSourcesController> logger,
        IMapper mapper)
    {
        _syndicationFeedSourceManager = syndicationFeedSourceManager;
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<SyndicationFeedSourceResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<SyndicationFeedSourceResponse>>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        if (page < 1) page = Pagination.DefaultPage;
        if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;

        List<SyndicationFeedSource> sources;
        if (User.IsSiteAdministrator())
        {
            // TODO(morpheus): replace with GetAllAsync(page, pageSize, sortBy, sortDescending, filter) when paged overload is available
            sources = await _syndicationFeedSourceManager.GetAllAsync();
        }
        else
        {
            var ownerOid = User.GetOwnerOid();
            // TODO(morpheus): replace with GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter) when paged overload is available
            sources = await _syndicationFeedSourceManager.GetAllAsync(ownerOid);
        }

        var items = _mapper.Map<List<SyndicationFeedSourceResponse>>(sources);
        return new PagedResponse<SyndicationFeedSourceResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = sources.Count
        };
    }

    /// <summary>
    /// Returns a syndication feed source by its identifier
    /// </summary>
    /// <param name="id">The identifier of the syndication feed source</param>
    /// <returns>A <see cref="SyndicationFeedSource"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="400">If the request is poorly formatted</response>
    /// <response code="403">If the current user does not own this resource</response>
    /// <response code="404">If the requested id was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ActionName(nameof(GetSyndicationFeedSourceAsync))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SyndicationFeedSourceResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyndicationFeedSourceResponse>> GetSyndicationFeedSourceAsync(int id)
    {
        var source = await _syndicationFeedSourceManager.GetAsync(id);
        if (source is null)
            return NotFound();

        if (!User.IsSiteAdministrator() && source.CreatedByEntraOid != User.GetOwnerOid())
        {
            return Forbid();
        }

        return Ok(_mapper.Map<SyndicationFeedSourceResponse>(source));
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
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SyndicationFeedSourceResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyndicationFeedSourceResponse>> CreateSyndicationFeedSourceAsync(SyndicationFeedSourceRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateSyndicationFeedSourceAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var source = _mapper.Map<SyndicationFeedSource>(request);
        source.CreatedByEntraOid = User.GetOwnerOid();
        source.AddedOn = DateTimeOffset.UtcNow;
        source.LastUpdatedOn = DateTimeOffset.UtcNow;

        var result = await _syndicationFeedSourceManager.SaveAsync(source);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("SyndicationFeedSource created with Id {SourceId}", result.Value.Id);
            return CreatedAtAction(nameof(GetSyndicationFeedSourceAsync), new { id = result.Value.Id },
                _mapper.Map<SyndicationFeedSourceResponse>(result.Value));
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
    public async Task<ActionResult> DeleteSyndicationFeedSourceAsync(int id)
    {
        var source = await _syndicationFeedSourceManager.GetAsync(id);
        if (source is null)
        {
            _logger.LogWarning("SyndicationFeedSource {SourceId} not found for deletion", id);
            return NotFound();
        }

        if (!User.IsSiteAdministrator() && source.CreatedByEntraOid != User.GetOwnerOid())
        {
            return Forbid();
        }

        var wasDeleted= await _syndicationFeedSourceManager.DeleteAsync(id);
        if (wasDeleted.IsSuccess)
        {
            _logger.LogInformation("SyndicationFeedSource {SourceId} deleted successfully", id);
            return NoContent();
        }

        _logger.LogWarning("SyndicationFeedSource {SourceId} deletion failed", id);
        return NotFound();
    }
}
