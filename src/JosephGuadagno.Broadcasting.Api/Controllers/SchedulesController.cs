using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Handles the interactions with the scheduled items
/// </summary>
[ApiController]
[Route("[controller]")]
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
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    public async Task<ActionResult<List<ScheduledItem>>> GetScheduledItemsAsync()
    {
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
    [HttpGet("{scheduledItemId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ActionName(nameof(GetScheduledItemAsync))]
    public async Task<ActionResult<ScheduledItem>> GetScheduledItemAsync(int scheduledItemId)
    {
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
    [HttpPost, HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduledItem>> SaveScheduledItemAsync(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await _scheduledItemManager.SaveAsync(scheduledItem);
        if (scheduledItem.Id == 0)
        {
            return CreatedAtAction(nameof(GetScheduledItemAsync), new { scheduledItemId = savedScheduledItem.Id },
                savedScheduledItem);
        }

        return Ok();
    }
    
    /// <summary>
    /// Deletes the scheduled item
    /// </summary>
    /// <param name="scheduledItemId">The identifier of the scheduled item</param>
    /// <returns>True, if the deletion was successful, otherwise false</returns>
    /// <response code="204">If the scheduled item was deleted</response>
    /// <response code="400">If the data provided failed validation</response>
    /// <response code="404">If a scheduled item with the specified identifier was not found</response>
    [HttpDelete("{scheduledItemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> DeleteScheduledItemAsync(int scheduledItemId)
    {
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
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ScheduledItem>>> GetUpcomingScheduledItemsAsync()
    {
        var items = await _scheduledItemManager.GetUpcomingScheduledItemsAsync();
        if (items is null || items.Count == 0)
        {
            return NotFound();
        }

        return items;
    }
}