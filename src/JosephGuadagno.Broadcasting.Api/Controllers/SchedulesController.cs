using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SchedulesController: ControllerBase
{
    private readonly IScheduledItemManager _scheduledItemManager;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(IScheduledItemManager scheduledItemManager, ILogger<SchedulesController> logger)
    {
        _scheduledItemManager = scheduledItemManager;
        _logger = logger;
    }
    
    // GetAll
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    public async Task<ActionResult<List<ScheduledItem>>> GetScheduledItemsAsync()
    {
        return await _scheduledItemManager.GetAllAsync();
    }

    // Get
    [HttpGet("{scheduledItemId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ActionName(nameof(GetScheduledItemAsync))]
    public async Task<ActionResult<ScheduledItem>> GetScheduledItemAsync(int scheduledItemId)
    {
        return await _scheduledItemManager.GetAsync(scheduledItemId);
    }

    // Save
    [HttpPost, HttpPut]
    [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(ScheduledItem))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduledItem>> SaveScheduledItemAsync(ScheduledItem scheduledItem)
    {
        var savedScheduledItem = await _scheduledItemManager.SaveAsync(scheduledItem); 
        return CreatedAtAction(nameof(GetScheduledItemAsync), new {scheduledItemId = savedScheduledItem.Id}, savedScheduledItem);
    }
    
    // Delete
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
    
    // Upcoming
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduledItem>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ScheduledItem>>> GetUpcomingScheduledItemsAsync()
    {
        return await _scheduledItemManager.GetUpcomingScheduledItemsAsync();
    }
}