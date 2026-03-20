using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Handles the interactions with the scheduled items
/// </summary>
[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class SchedulesController: ControllerBase
{
    private readonly IScheduledItemManager _scheduledItemManager;
    private readonly ILogger<SchedulesController> _logger;

    /// <summary>
    /// The constructor
    /// </summary>
    /// <param name="scheduledItemManager">The scheduled item manager</param>
    /// <param name="logger">The logger to use</param>
    public SchedulesController(IScheduledItemManager scheduledItemManager, ILogger<SchedulesController> logger)
    {
        _scheduledItemManager = scheduledItemManager;
        _logger = logger;
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetScheduledItemsAsync(int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.List);
        
        var allItems = await _scheduledItemManager.GetAllAsync();
        var totalCount = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ActionName(nameof(GetScheduledItemAsync))]
    public async Task<ActionResult<ScheduledItemResponse>> GetScheduledItemAsync(int scheduledItemId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.View);
        
        var item = await _scheduledItemManager.GetAsync(scheduledItemId);
        return ToResponse(item);
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
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(ScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ScheduledItemResponse>> CreateScheduledItemAsync(ScheduledItemRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.Modify);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateScheduledItemAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var scheduledItem = ToModel(request);
        var savedScheduledItem = await _scheduledItemManager.SaveAsync(scheduledItem);
        if (savedScheduledItem != null)
        {
            _logger.LogInformation("ScheduledItem created with Id {ScheduledItemId}", savedScheduledItem.Id);
            return CreatedAtAction(nameof(GetScheduledItemAsync), new { scheduledItemId = savedScheduledItem.Id },
                ToResponse(savedScheduledItem));
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
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ScheduledItemResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ScheduledItemResponse>> UpdateScheduledItemAsync(int scheduledItemId, ScheduledItemRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.Modify);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateScheduledItemAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var scheduledItem = ToModel(request, scheduledItemId);
        var savedScheduledItem = await _scheduledItemManager.SaveAsync(scheduledItem);
        if (savedScheduledItem != null)
        {
            _logger.LogInformation("ScheduledItem updated with Id {ScheduledItemId}", savedScheduledItem.Id);
            return Ok(ToResponse(savedScheduledItem));
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> DeleteScheduledItemAsync(int scheduledItemId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.Delete);
        
        var wasDeleted = await _scheduledItemManager.DeleteAsync(scheduledItemId);
        if (wasDeleted)
        {
            _logger.LogInformation("ScheduledItem {ScheduledItemId} deleted successfully", scheduledItemId);
            return new NoContentResult();
        }
        _logger.LogWarning("ScheduledItem {ScheduledItemId} not found for deletion", scheduledItemId);
        return new NotFoundResult();
    }
    
    /// <summary>
    /// Gets a list of unsent scheduled items
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of scheduled items that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are not items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("unsent")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetUnsentScheduledItemsAsync(int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.UnsentScheduled);
        
        var allItems = await _scheduledItemManager.GetUnsentScheduledItemsAsync();
        if (allItems.Count == 0)
        {
            return NotFound();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetScheduledItemsToSendAsync(int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.ScheduledToSend);
        
        var allItems = await _scheduledItemManager.GetScheduledItemsToSendAsync();
        if (allItems.Count == 0)
        {
            return NotFound();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetUpcomingScheduledItemsForCalendarMonthAsync(int year, int month, int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.UpcomingScheduled);
        
        var allItems = await _scheduledItemManager.GetScheduledItemsByCalendarMonthAsync(year, month);
        if (allItems.Count == 0)
        {
            return NotFound();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<ScheduledItemResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ScheduledItemResponse>>> GetOrphanedScheduledItemsAsync(int page = 1, int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);

        var allItems = await _scheduledItemManager.GetOrphanedScheduledItemsAsync();
        if (allItems.Count == 0)
        {
            return NotFound();
        }

        var totalCount = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToList();
        
        return new PagedResponse<ScheduledItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static ScheduledItemResponse ToResponse(ScheduledItem s) => new()
    {
        Id = s.Id,
        ItemType = s.ItemType,
        ItemTableName = s.ItemTableName,
        ItemPrimaryKey = s.ItemPrimaryKey,
        Message = s.Message,
        ImageUrl = s.ImageUrl,
        MessageSentOn = s.MessageSentOn,
        MessageSent = s.MessageSent,
        SendOnDateTime = s.SendOnDateTime
    };

    private static ScheduledItem ToModel(ScheduledItemRequest r, int id = 0) => new()
    {
        Id = id,
        ItemType = r.ItemType,
        ItemPrimaryKey = r.ItemPrimaryKey,
        Message = r.Message,
        ImageUrl = r.ImageUrl,
        SendOnDateTime = r.SendOnDateTime
    };
}
