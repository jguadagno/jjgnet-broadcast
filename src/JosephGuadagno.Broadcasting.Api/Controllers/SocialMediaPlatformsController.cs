using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with Social Media Platforms for JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class SocialMediaPlatformsController : ControllerBase
{
    private readonly ISocialMediaPlatformManager _socialMediaPlatformManager;
    private readonly ILogger<SocialMediaPlatformsController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with Social Media Platforms
    /// </summary>
    public SocialMediaPlatformsController(
        ISocialMediaPlatformManager socialMediaPlatformManager,
        ILogger<SocialMediaPlatformsController> logger,
        IMapper mapper)
    {
        _socialMediaPlatformManager = socialMediaPlatformManager;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all active social media platforms
    /// </summary>
    /// <returns>A list of active social media platforms</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SocialMediaPlatformResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SocialMediaPlatformResponse>>> GetAllAsync()
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.SocialMediaPlatforms.List, Domain.Scopes.SocialMediaPlatforms.All);

        var platforms = await _socialMediaPlatformManager.GetAllAsync();
        var response = _mapper.Map<List<SocialMediaPlatformResponse>>(platforms);
        return Ok(response);
    }

    /// <summary>
    /// Gets a social media platform by ID
    /// </summary>
    /// <param name="id">The platform ID</param>
    /// <returns>A <see cref="SocialMediaPlatformResponse"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SocialMediaPlatformResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SocialMediaPlatformResponse>> GetAsync(int id)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.SocialMediaPlatforms.View, Domain.Scopes.SocialMediaPlatforms.All);

        var platform = await _socialMediaPlatformManager.GetByIdAsync(id);
        if (platform is null)
        {
            _logger.LogWarning("Social media platform with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(_mapper.Map<SocialMediaPlatformResponse>(platform));
    }

    /// <summary>
    /// Creates a new social media platform
    /// </summary>
    /// <param name="request">The platform data to create</param>
    /// <returns>The newly created platform</returns>
    /// <response code="201">If the item was created successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SocialMediaPlatformResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SocialMediaPlatformResponse>> CreateAsync([FromBody] SocialMediaPlatformRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.SocialMediaPlatforms.Add, Domain.Scopes.SocialMediaPlatforms.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var platform = _mapper.Map<SocialMediaPlatform>(request);
        var created = await _socialMediaPlatformManager.AddAsync(platform);
        if (created is null)
        {
            _logger.LogError("Failed to create social media platform: {Name}", request.Name);
            return BadRequest("Failed to create social media platform");
        }

        _logger.LogInformation("Created social media platform with ID {Id}: {Name}", created.Id, created.Name);
        var response = _mapper.Map<SocialMediaPlatformResponse>(created);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, response);
    }

    /// <summary>
    /// Updates an existing social media platform
    /// </summary>
    /// <param name="id">The platform ID</param>
    /// <param name="request">The updated platform data</param>
    /// <returns>The updated platform</returns>
    /// <response code="200">If the item was updated successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SocialMediaPlatformResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SocialMediaPlatformResponse>> UpdateAsync(int id, [FromBody] SocialMediaPlatformRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.SocialMediaPlatforms.Modify, Domain.Scopes.SocialMediaPlatforms.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var platform = _mapper.Map<SocialMediaPlatform>(request);
        platform.Id = id;
        var updated = await _socialMediaPlatformManager.UpdateAsync(platform);
        if (updated is null)
        {
            _logger.LogWarning("Social media platform with ID {Id} not found for update", id);
            return NotFound();
        }

        _logger.LogInformation("Updated social media platform with ID {Id}: {Name}", id, updated.Name);
        return Ok(_mapper.Map<SocialMediaPlatformResponse>(updated));
    }

    /// <summary>
    /// Soft deletes a social media platform (sets IsActive = false)
    /// </summary>
    /// <param name="id">The platform ID</param>
    /// <returns>No content</returns>
    /// <response code="204">If the item was deleted successfully</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.SocialMediaPlatforms.Delete, Domain.Scopes.SocialMediaPlatforms.All);

        var deleted = await _socialMediaPlatformManager.DeleteAsync(id);
        if (!deleted)
        {
            _logger.LogWarning("Social media platform with ID {Id} not found for deletion", id);
            return NotFound();
        }

        _logger.LogInformation("Soft deleted social media platform with ID {Id}", id);
        return NoContent();
    }
}
