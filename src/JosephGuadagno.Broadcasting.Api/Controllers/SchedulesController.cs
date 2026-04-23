using System.Security.Claims;
using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Handles the interactions with the scheduled items
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class SchedulesController: ControllerBase
{
    private readonly IScheduledItemManager _scheduledItemManager;
    private readonly IEngagementManager _engagementManager;
    private readonly ISyndicationFeedSourceManager _syndicationFeedSourceManager;
    private readonly IYouTubeSourceManager _youTubeSourceManager;
    private readonly ILogger<SchedulesController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// The constructor
    /// </summary>
    /// <param name="scheduledItemManager">The scheduled item manager</param>
    /// <param name="engagementManager">The engagement manager</param>
    /// <param name="syndicationFeedSourceManager">The syndication feed source manager</param>
    /// <param name="youTubeSourceManager">The YouTube source manager</param>
    /// <param name="logger">The logger to use</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public SchedulesController(
        IScheduledItemManager scheduledItemManager,
        IEngagementManager engagementManager,
        ISyndicationFeedSourceManager syndicationFeedSourceManager,
        IYouTubeSourceManager youTubeSourceManager,
        ILogger<SchedulesController> logger,
        IMapper mapper)
    {
        _scheduledItemManager = scheduledItemManager;
        _engagementManager = engagementManager;
        _syndicationFeedSourceManager = syndicationFeedSourceManager;
        _youTubeSourceManager = youTubeSourceManager;
        _logger = logger;
        _mapper = mapper;
    }

    private string GetOwnerOid()
    {
        return User.FindFirstValue(ApplicationClaimTypes.EntraObjectId)
            ?? throw new InvalidOperationException("Entra Object ID claim not found");
    }

    private bool IsSiteAdministrator()
    {
        return User.IsInRole(RoleNames.SiteAdministrator);
    }
    
    /// <summary>
    /// Returns all the scheduled items
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of scheduled items</returns>
    /// <response code="200">Upon success</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetScheduledItemsAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        PagedResult<ScheduledItem> result;
        if (IsSiteAdministrator())
        {
            result = await _scheduledItemManager.GetAllAsync(page, pageSize);
        }
        else
        {
            var ownerOid = GetOwnerOid();
            result = await _scheduledItemManager.GetAllAsync(ownerOid, page, pageSize);
        }

        var items = _mapper.Map<List<ScheduledItemResponse>>(result.Items);
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets a scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the scheduled item</param>
    /// <returns>A <see cref="ScheduledItem"/></returns>
    /// <response code="200">Upon a successful call</response>
    /// <response code="400">Returned if the request is invalid</response>
    /// <response code="404">Returned if an <see cref="ScheduledItem"/> was not found for the specified id</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{scheduledItemId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ActionName(nameof(GetScheduledItemAsync))]
    public async Task<ActionResult<ScheduledItemResponse>> GetScheduledItemAsync(int scheduledItemId)
    {
        var item = await _scheduledItemManager.GetAsync(scheduledItemId);
        if (item is null)
            return NotFound();

        if (!IsSiteAdministrator() && item.CreatedByEntraOid != GetOwnerOid())
        {
            return Forbid();
        }

        return Ok(_mapper.Map<ScheduledItemResponse>(item));
    }

    /// <summary>
    /// Creates a scheduled item
    /// </summary>
    /// <param name="request">The scheduled item data to create</param>
    /// <returns>The newly created scheduled item</returns>
    /// <response code="201">If the scheduled item was created</response>
    /// <response code="400">If the data provided failed validation</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(ScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ScheduledItemResponse>> CreateScheduledItemAsync(ScheduledItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateScheduledItemAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var scheduledItem = _mapper.Map<ScheduledItem>(request);
        scheduledItem.CreatedByEntraOid = GetOwnerOid();
        var result = await _scheduledItemManager.SaveAsync(scheduledItem);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("ScheduledItem created with Id {ScheduledItemId}", result.Value.Id);
            return CreatedAtAction(nameof(GetScheduledItemAsync), new { scheduledItemId = result.Value.Id },
                _mapper.Map<ScheduledItemResponse>(result.Value));
        }

        return Problem("Failed to create the scheduled item");
    }

    /// <summary>
    /// Updates an existing scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the scheduled item to update</param>
    /// <param name="request">The updated scheduled item data</param>
    /// <returns>The updated scheduled item</returns>
    /// <response code="200">If the scheduled item was updated</response>
    /// <response code="400">If the data provided failed validation or the id does not match</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{scheduledItemId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ScheduledItemResponse>> UpdateScheduledItemAsync(int scheduledItemId, ScheduledItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateScheduledItemAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var existing = await _scheduledItemManager.GetAsync(scheduledItemId);
        if (existing is null)
            return NotFound();

        if (!IsSiteAdministrator() && existing.CreatedByEntraOid != GetOwnerOid())
        {
            return Forbid();
        }

        var scheduledItem = _mapper.Map<ScheduledItem>(request);
        scheduledItem.Id = scheduledItemId;
        scheduledItem.CreatedByEntraOid = existing.CreatedByEntraOid;
        var result = await _scheduledItemManager.SaveAsync(scheduledItem);
        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("ScheduledItem updated with Id {ScheduledItemId}", result.Value.Id);
            return Ok(_mapper.Map<ScheduledItemResponse>(result.Value));
        }

        return Problem("Failed to update the scheduled item");
    }
    
    /// <summary>
    /// Deletes the scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the scheduled item</param>
    /// <returns>True, if the deletion was successful, otherwise false</returns>
    /// <response code="204">If the scheduled item was deleted</response>
    /// <response code="400">If the data provided failed validation</response>
    /// <response code="404">If a scheduled item with the specified identifier was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpDelete("{scheduledItemId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> DeleteScheduledItemAsync(int scheduledItemId)
    {
        var scheduledItem = await _scheduledItemManager.GetAsync(scheduledItemId);
        if (scheduledItem is null)
        {
            _logger.LogWarning("ScheduledItem {ScheduledItemId} not found for deletion", scheduledItemId);
            return new NotFoundResult();
        }

        if (!IsSiteAdministrator() && scheduledItem.CreatedByEntraOid != GetOwnerOid())
        {
            return Forbid();
        }
        
        var wasDeleted = await _scheduledItemManager.DeleteAsync(scheduledItemId);
        if (wasDeleted.IsSuccess)
        {
            _logger.LogInformation("ScheduledItem {ScheduledItemId} deleted successfully", scheduledItemId);
            return new NoContentResult();
        }
        _logger.LogWarning("ScheduledItem {ScheduledItemId} deletion failed", scheduledItemId);
        return new NotFoundResult();
    }
    
    /// <summary>
    /// Gets a list of unsent scheduled items
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of scheduled items that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are no items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("unsent")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetUnsentScheduledItemsAsync([FromQuery] int page = Pagination.DefaultPage, [FromQuery] int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        var result = await _scheduledItemManager.GetUnsentScheduledItemsAsync(page, pageSize);
        if (result.TotalCount == 0)
        {
            return NotFound();
        }

        var items = _mapper.Map<List<ScheduledItemResponse>>(result.Items);
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets a list of unsent scheduled items that should have been sent already
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of scheduled items that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are not items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("upcoming")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetScheduledItemsToSendAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        var result = await _scheduledItemManager.GetScheduledItemsToSendAsync(page, pageSize);
        if (result.TotalCount == 0)
        {
            return NotFound();
        }

        var items = _mapper.Map<List<ScheduledItemResponse>>(result.Items);
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }
    
    /// <summary>
    /// Gets a list of unsent scheduled items for a given calendar month
    /// </summary>
    /// <param name="year">The calendar year</param>
    /// <param name="month">The calendar month</param>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of scheduled items that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are not items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("calendar/{year:int}/{month:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetUpcomingScheduledItemsForCalendarMonthAsync(int year, int month, int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        var result = await _scheduledItemManager.GetScheduledItemsByCalendarMonthAsync(year, month, page, pageSize);
        if (result.TotalCount == 0)
        {
            return NotFound();
        }

        var items = _mapper.Map<List<ScheduledItemResponse>>(result.Items);
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets a list of orphaned scheduled items (items whose source no longer exists)
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of scheduled items that reference source items that no longer exist.</returns>
    /// <response code="200">Returned if there are orphaned scheduled items.</response>
    /// <response code="404">If there are no orphaned scheduled items</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("orphaned")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetOrphanedScheduledItemsAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        var result = await _scheduledItemManager.GetOrphanedScheduledItemsAsync(page, pageSize);
        if (result.TotalCount == 0)
        {
            return NotFound();
        }

        var items = _mapper.Map<List<ScheduledItemResponse>>(result.Items);
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Validates that a source item exists for the given type and primary key.
    /// Used by the Web project's AJAX validation.
    /// </summary>
    /// <param name="itemType">The type of item (Engagements, Talks, SyndicationFeedSources, YouTubeSources)</param>
    /// <param name="itemPrimaryKey">The primary key of the item to validate</param>
    /// <returns>A <see cref="SourceItemValidationResponse"/> with validation status and item details</returns>
    /// <response code="200">Upon success, returns validation result (IsValid may be false when item not found)</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("validate-source-item")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SourceItemValidationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SourceItemValidationResponse>> ValidateSourceItemAsync(
        [FromQuery] ScheduledItemType itemType,
        [FromQuery] int itemPrimaryKey)
    {
        if (itemPrimaryKey <= 0)
        {
            return BadRequest("itemPrimaryKey must be greater than 0");
        }

        try
        {
            return itemType switch
            {
                ScheduledItemType.Engagements => await ValidateEngagementAsync(itemPrimaryKey),
                ScheduledItemType.Talks => await ValidateTalkAsync(itemPrimaryKey),
                ScheduledItemType.SyndicationFeedSources => await ValidateSyndicationFeedSourceAsync(itemPrimaryKey),
                ScheduledItemType.YouTubeSources => await ValidateYouTubeSourceAsync(itemPrimaryKey),
                _ => BadRequest($"Unknown item type: {itemType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating source item type={ItemType} key={ItemPrimaryKey}",
                itemType, itemPrimaryKey);
            return Ok(new SourceItemValidationResponse
            {
                IsValid = false,
                ErrorMessage = "An error occurred while validating the item"
            });
        }
    }

    private async Task<ActionResult<SourceItemValidationResponse>> ValidateEngagementAsync(int id)
    {
        var engagement = await _engagementManager.GetAsync(id);
        if (engagement is null)
        {
            return Ok(new SourceItemValidationResponse { IsValid = false, ErrorMessage = "Item not found" });
        }

        return Ok(new SourceItemValidationResponse
        {
            IsValid = true,
            ItemTitle = engagement.Name,
            ItemDetails = $"{engagement.StartDateTime:d} – {engagement.EndDateTime:d}"
        });
    }

    private async Task<ActionResult<SourceItemValidationResponse>> ValidateTalkAsync(int talkId)
    {
        var talk = await _engagementManager.GetTalkAsync(talkId);
        if (talk is null)
        {
            return Ok(new SourceItemValidationResponse { IsValid = false, ErrorMessage = "Item not found" });
        }

        Engagement? engagement = null;
        if (talk.EngagementId > 0)
        {
            engagement = await _engagementManager.GetAsync(talk.EngagementId);
        }

        return Ok(new SourceItemValidationResponse
        {
            IsValid = true,
            ItemTitle = talk.Name,
            ItemDetails = engagement is not null ? engagement.Name : null
        });
    }

    private async Task<ActionResult<SourceItemValidationResponse>> ValidateSyndicationFeedSourceAsync(int id)
    {
        var source = await _syndicationFeedSourceManager.GetAsync(id);
        if (source is null)
        {
            return Ok(new SourceItemValidationResponse { IsValid = false, ErrorMessage = "Item not found" });
        }

        return Ok(new SourceItemValidationResponse
        {
            IsValid = true,
            ItemTitle = source.Title,
            ItemDetails = source.Author
        });
    }

    private async Task<ActionResult<SourceItemValidationResponse>> ValidateYouTubeSourceAsync(int id)
    {
        var source = await _youTubeSourceManager.GetAsync(id);
        if (source is null)
        {
            return Ok(new SourceItemValidationResponse { IsValid = false, ErrorMessage = "Item not found" });
        }

        return Ok(new SourceItemValidationResponse
        {
            IsValid = true,
            ItemTitle = source.Title,
            ItemDetails = source.Author
        });
    }
}
