using AutoMapper;
using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Api.Controllers;

/// <summary>
/// Works with Message Templates for JosephGuadagno.NET Broadcasting
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
[Produces("application/json")]
public class MessageTemplatesController : ControllerBase
{
    private readonly IMessageTemplateManager _messageTemplateManager;
    private readonly ISocialMediaPlatformManager _socialMediaPlatformManager;
    private readonly ILogger<MessageTemplatesController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with Message Templates
    /// </summary>
    /// <param name="messageTemplateManager">The message template manager</param>
    /// <param name="socialMediaPlatformManager">The social media platform manager</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public MessageTemplatesController(IMessageTemplateManager messageTemplateManager,
        ISocialMediaPlatformManager socialMediaPlatformManager,
        ILogger<MessageTemplatesController> logger, IMapper mapper)
    {
        _messageTemplateManager = messageTemplateManager;
        _socialMediaPlatformManager = socialMediaPlatformManager;
        _logger = logger;
        _mapper = mapper;
    }


    /// <summary>
    /// Gets all message templates
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <param name="sortBy">The field to sort by (default: subject)</param>
    /// <param name="sortDescending">When true, sorts in descending order (default: true)</param>
    /// <param name="filter">Optional text filter applied to message template subjects</param>
    /// <returns>A paginated list of message templates</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<MessageTemplateResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<MessageTemplateResponse>>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "subject", bool sortDescending = true, string? filter = null)
    {
        if (page < 1) page = Pagination.DefaultPage;
        if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;
        
        PagedResult<MessageTemplate> result;
        if (User.IsSiteAdministrator())
        {
            result = await _messageTemplateManager.GetAllAsync(page, pageSize, sortBy, sortDescending, filter);
        }
        else
        {
            var ownerOid = User.GetOwnerOid();
            result = await _messageTemplateManager.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter);
        }

        var items = _mapper.Map<List<MessageTemplateResponse>>(result.Items);
        
        return new PagedResponse<MessageTemplateResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets a message template by platform and message type for the current user
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <returns>A <see cref="MessageTemplateResponse"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{platform}/{messageType}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplateResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplateResponse>> GetAsync(string platform, string messageType)
    {
        var socialMediaPlatform = await _socialMediaPlatformManager.GetByNameAsync(platform);
        if (socialMediaPlatform is null)
        {
            _logger.LogWarning("Social media platform not found: {Platform}", LogSanitizer.Sanitize(platform));
            return NotFound();
        }

        var ownerOid = User.IsSiteAdministrator()
            ? MessageTemplates.SystemOwnerEntraOid
            : User.GetOwnerOid();
        var template = await _messageTemplateManager.GetAsync(socialMediaPlatform.Id, messageType, ownerOid);
        if (template is null)
        {
            _logger.LogWarning("MessageTemplate not found for PlatformId={PlatformId}, MessageType={MessageType}, OwnerOid={OwnerOid}",
                socialMediaPlatform.Id, LogSanitizer.Sanitize(messageType), LogSanitizer.Sanitize(ownerOid));
            return NotFound();
        }

        return _mapper.Map<MessageTemplateResponse>(template);
    }

    /// <summary>
    /// Gets the system default template for a platform and message type
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <returns>A <see cref="MessageTemplateResponse"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("defaults/{platform}/{messageType}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplateResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplateResponse>> GetDefaultAsync(string platform, string messageType)
    {
        var socialMediaPlatform = await _socialMediaPlatformManager.GetByNameAsync(platform);
        if (socialMediaPlatform is null)
        {
            _logger.LogWarning("Social media platform not found: {Platform}", LogSanitizer.Sanitize(platform));
            return NotFound();
        }

        var template = await _messageTemplateManager.GetAsync(socialMediaPlatform.Id, messageType, MessageTemplates.SystemOwnerEntraOid);
        if (template is null)
        {
            return NotFound();
        }

        return _mapper.Map<MessageTemplateResponse>(template);
    }

    /// <summary>
    /// Gets all system default message templates
    /// </summary>
    /// <returns>A list of system default <see cref="MessageTemplateResponse"/> items</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("defaults")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MessageTemplateResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<MessageTemplateResponse>>> GetAllDefaultsAsync()
    {
        var templates = await _messageTemplateManager.GetAllDefaultsAsync();
        return Ok(_mapper.Map<List<MessageTemplateResponse>>(templates));
    }

    /// <summary>
    /// Creates a new user-owned message template (cloned or custom)
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <param name="request">The message template data</param>
    /// <returns>The created <see cref="MessageTemplateResponse"/></returns>
    /// <response code="201">If the item was created</response>
    /// <response code="400">If the model is invalid</response>
    /// <response code="404">If the platform was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPost("{platform}/{messageType}")]
    [IgnoreAntiforgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MessageTemplateResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplateResponse>> CreateAsync(string platform, string messageType,
        [FromBody] MessageTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var socialMediaPlatform = await _socialMediaPlatformManager.GetByNameAsync(platform);
        if (socialMediaPlatform is null)
        {
            _logger.LogWarning("Social media platform not found: {Platform}", LogSanitizer.Sanitize(platform));
            return NotFound();
        }

        var messageTemplate = _mapper.Map<MessageTemplate>(request);
        messageTemplate.SocialMediaPlatformId = socialMediaPlatform.Id;
        messageTemplate.MessageType = messageType;
        messageTemplate.CreatedByEntraOid = User.GetOwnerOid();

        var created = await _messageTemplateManager.CreateAsync(messageTemplate);
        if (created is null)
        {
            _logger.LogWarning("MessageTemplate create failed: PlatformId={PlatformId}, MessageType={MessageType}",
                socialMediaPlatform.Id, LogSanitizer.Sanitize(messageType));
            return BadRequest();
        }

        _logger.LogInformation("MessageTemplate created for Platform={Platform}, MessageType={MessageType}",
            LogSanitizer.Sanitize(platform), LogSanitizer.Sanitize(messageType));
        return CreatedAtAction(nameof(GetAsync), new { platform, messageType },
            _mapper.Map<MessageTemplateResponse>(created));
    }

    /// <summary>
    /// Updates a message template
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <param name="request">The updated message template data</param>
    /// <returns>The updated <see cref="MessageTemplateResponse"/></returns>
    /// <response code="200">If the item was updated successfully</response>
    /// <response code="400">If the route parameters are invalid</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{platform}/{messageType}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplateResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplateResponse>> UpdateAsync(string platform, string messageType,
        [FromBody] MessageTemplateRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var socialMediaPlatform = await _socialMediaPlatformManager.GetByNameAsync(platform);
        if (socialMediaPlatform is null)
        {
            _logger.LogWarning("Social media platform not found: {Platform}", LogSanitizer.Sanitize(platform));
            return NotFound();
        }

        var ownerOid = User.GetOwnerOid();
        var existing = await _messageTemplateManager.GetAsync(socialMediaPlatform.Id, messageType, ownerOid);
        if (existing is null)
        {
            _logger.LogWarning("MessageTemplate not found for update: PlatformId={PlatformId}, MessageType={MessageType}", socialMediaPlatform.Id, LogSanitizer.Sanitize(messageType));
            return NotFound();
        }

        var messageTemplate = _mapper.Map<MessageTemplate>(request);
        messageTemplate.SocialMediaPlatformId = socialMediaPlatform.Id;
        messageTemplate.MessageType = messageType;
        messageTemplate.CreatedByEntraOid = ownerOid;
        var updated = await _messageTemplateManager.UpdateAsync(messageTemplate);
        if (updated is null)
        {
            _logger.LogWarning("MessageTemplate update failed: PlatformId={PlatformId}, MessageType={MessageType}", socialMediaPlatform.Id, LogSanitizer.Sanitize(messageType));
            return NotFound();
        }

        _logger.LogInformation("MessageTemplate updated for Platform={Platform}, MessageType={MessageType}", LogSanitizer.Sanitize(platform), LogSanitizer.Sanitize(messageType));
        return _mapper.Map<MessageTemplateResponse>(updated);
    }
}
