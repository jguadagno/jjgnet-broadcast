using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with the Engagements of JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Route("[controller]")]
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
    /// <returns>A list of engagements</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="400">If the request is poorly formatted</response>            
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(List<Engagement>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<List<Engagement>> GetEngagements()
    {
        _logger.LogDebug("Call to Engagements.GetEngagements");

        return await _engagementManager.GetAllAsync();
    }

    /// <summary>
    /// Returns an engagement by it's identifier
    /// </summary>
    /// <param name="id">The identifier of the engagement</param>
    /// <returns>An <see cref="Engagement"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>            
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(Engagement))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Engagement>> GetEngagement(int id)
    {
        _logger.LogDebug("Call to Engagements.GetEngagement(id)");

        var engagement = await _engagementManager.GetAsync(id);
        if (engagement is null)
        {
            return new NotFoundResult();
        }

        return engagement;
    }

    /// <summary>
    /// Saves an engagement
    /// </summary>
    /// <param name="engagement">An engagement</param>
    /// <returns>The engagement with the Url to view its details</returns>
    /// <response code="201">Returns the newly created item</response>
    /// <response code="400">If the item is null or there are data violations</response>            
    [HttpPost, HttpPut]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Engagement>> SaveEngagement(Engagement engagement)
    {
        var savedEngagement = await _engagementManager.SaveAsync(engagement);

        return CreatedAtAction(nameof(GetEngagement), new {id = savedEngagement.Id}, savedEngagement);
        
    }
    
    /// <summary>
    /// Deletes the specified contact
    /// </summary>
    /// <param name="id">The primary identifier for the engagement</param>
    /// <returns></returns>
    /// <response code="204">If the item was deleted</response>
    /// <response code="400">If the request is poorly formatted</response>            
    /// <response code="404">If the requested id was not found</response>            
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> DeleteContact(int id)
    {
        var wasDeleted = await _engagementManager.DeleteAsync(id);
        if (wasDeleted)
        {
            return new NoContentResult();
        }
        return new NotFoundResult();
    }
}