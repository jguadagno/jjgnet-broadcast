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
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt;s</returns>
    /// <response code="200">Upon success</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ScheduledItem>>> GetScheduledItemsAsync()
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.List);
        
        return await _scheduledItemManager.GetAllAsync();
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ActionName(nameof(GetScheduledItemAsync))]
    public async Task<ActionResult<ScheduledItem>> GetScheduledItemAsync(int scheduledItemId)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.View);
        
        return await _scheduledItemManager.GetAsync(scheduledItemId);
    }

    /// <summary>
    /// Saves a scheduled item
    /// </summary>
    /// <param name="scheduledItem">The scheduled item</param>
    /// <returns></returns>
    /// <remarks>If the <see cref="ScheduledItem.Id"/> is 0, the scheduled item will be updated.</remarks>
    /// <response code="200">If the scheduled item was updated</response>
    /// <response code="201">If the scheduled item was created</response>
    /// <response code="400">If the data provided failed validation</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost, HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ScheduledItem>> SaveScheduledItemAsync(ScheduledItem scheduledItem)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.Modify);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var savedScheduledItem = await _scheduledItemManager.SaveAsync(scheduledItem);
        if (savedScheduledItem != null)
        {
            return CreatedAtAction(nameof(GetScheduledItemAsync), new { scheduledItemId = savedScheduledItem.Id },
                savedScheduledItem);
        }

        return Problem("Failed to save the scheduled item");
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
            return new NoContentResult();
        }
        return new NotFoundResult();
    }
    
    /// <summary>
    /// Gets a list of unsent scheduled items
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt; that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are not items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("unsent")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ScheduledItem>>> GetUnsentScheduledItemsAsync()
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.UnsentScheduled);
        
        var items = await _scheduledItemManager.GetUnsentScheduledItemsAsync();
        if (items.Count == 0)
        {
            return NotFound();
        }

        return items;
    }

    /// <summary>
    /// Gets a list of unsent scheduled items that should have been sent already
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt; that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are not items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ScheduledItem>>> GetScheduledItemsToSendAsync()
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.ScheduledToSend);
        
        var items = await _scheduledItemManager.GetScheduledItemsToSendAsync();
        if (items.Count == 0)
        {
            return NotFound();
        }

        return items;
    }
    
    /// <summary>
    /// Gets a list of unsent scheduled items for a given calendar month
    /// </summary>
    /// <returns>A List&lt;<see cref="ScheduledItem"/>&gt; that have not yet been sent.</returns>
    /// <response code="200">Returned if there are unscheduled items that need to be sent.</response>
    /// <response code="404">If there are not items that need to be sent</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("calendar/{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ScheduledItem>>> GetUpcomingScheduledItemsForCalendarMonthAsync(int year, int month)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.All);
        //HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.Schedules.UpcomingScheduled);
        
        var items = await _scheduledItemManager.GetScheduledItemsByCalendarMonthAsync(year, month);
        if (items.Count == 0)
        {
            return NotFound();
        }

        return items;
    }
}