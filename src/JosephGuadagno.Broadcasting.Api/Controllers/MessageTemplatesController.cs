using System.Security.Claims;
using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

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
    private readonly IMessageTemplateDataStore _messageTemplateDataStore;
    private readonly ISocialMediaPlatformManager _socialMediaPlatformManager;
    private readonly ILogger<MessageTemplatesController> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the interactions with Message Templates
    /// </summary>
    /// <param name="messageTemplateDataStore">The message template data store</param>
    /// <param name="socialMediaPlatformManager">The social media platform manager</param>
    /// <param name="logger">The logger</param>
    /// <param name="mapper">The AutoMapper instance</param>
    public MessageTemplatesController(IMessageTemplateDataStore messageTemplateDataStore,
        ISocialMediaPlatformManager socialMediaPlatformManager,
        ILogger<MessageTemplatesController> logger, IMapper mapper)
    {
        _messageTemplateDataStore = messageTemplateDataStore;
        _socialMediaPlatformManager = socialMediaPlatformManager;
        _logger = logger;
        _mapper = mapper;
    }

    private static string SanitizeForLog(string? value) =>
        value?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;

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
    /// Gets all message templates
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 25)</param>
    /// <returns>A paginated list of message templates</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<MessageTemplateResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<MessageTemplateResponse>>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
        
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.MessageTemplates.List, Domain.Scopes.MessageTemplates.All);

        PagedResult<MessageTemplate> result;
        if (IsSiteAdministrator())
        {
            result = await _messageTemplateDataStore.GetAllAsync(page, pageSize);
        }
        else
        {
            var ownerOid = GetOwnerOid();
            result = await _messageTemplateDataStore.GetAllAsync(ownerOid, page, pageSize);
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
    /// Gets a message template by platform and message type
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <returns>A <see cref="MessageTemplateResponse"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{platform}/{messageType}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplateResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplateResponse>> GetAsync(string platform, string messageType)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.MessageTemplates.View, Domain.Scopes.MessageTemplates.All);
        
        var socialMediaPlatform = await _socialMediaPlatformManager.GetByNameAsync(platform);
        if (socialMediaPlatform is null)
        {
            _logger.LogWarning("Social media platform not found: {Platform}", SanitizeForLog(platform));
            return NotFound();
        }
        
        var template = await _messageTemplateDataStore.GetAsync(socialMediaPlatform.Id, messageType);
        if (template is null)
        {
            _logger.LogWarning("MessageTemplate not found for PlatformId={PlatformId}, MessageType={MessageType}", socialMediaPlatform.Id, SanitizeForLog(messageType));
            return NotFound();
        }

        if (!IsSiteAdministrator() && template.CreatedByEntraOid != GetOwnerOid())
        {
            return Forbid();
        }

        return _mapper.Map<MessageTemplateResponse>(template);
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplateResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplateResponse>> UpdateAsync(string platform, string messageType,
        [FromBody] MessageTemplateRequest request)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.MessageTemplates.Modify, Domain.Scopes.MessageTemplates.All);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateAsync called with invalid model state");
            return BadRequest(ModelState);
        }

        var socialMediaPlatform = await _socialMediaPlatformManager.GetByNameAsync(platform);
        if (socialMediaPlatform is null)
        {
            _logger.LogWarning("Social media platform not found: {Platform}", SanitizeForLog(platform));
            return NotFound();
        }

        var existing = await _messageTemplateDataStore.GetAsync(socialMediaPlatform.Id, messageType);
        if (existing is null)
        {
            _logger.LogWarning("MessageTemplate not found for update: PlatformId={PlatformId}, MessageType={MessageType}", socialMediaPlatform.Id, SanitizeForLog(messageType));
            return NotFound();
        }

        if (!IsSiteAdministrator() && existing.CreatedByEntraOid != GetOwnerOid())
        {
            return Forbid();
        }

        var messageTemplate = _mapper.Map<MessageTemplate>(request);
        messageTemplate.SocialMediaPlatformId = socialMediaPlatform.Id;
        messageTemplate.MessageType = messageType;
        messageTemplate.CreatedByEntraOid = existing.CreatedByEntraOid;
        var updated = await _messageTemplateDataStore.UpdateAsync(messageTemplate);
        if (updated is null)
        {
            _logger.LogWarning("MessageTemplate update failed: PlatformId={PlatformId}, MessageType={MessageType}", socialMediaPlatform.Id, SanitizeForLog(messageType));
            return NotFound();
        }

        _logger.LogInformation("MessageTemplate updated for Platform={Platform}, MessageType={MessageType}", platform, messageType);
        return _mapper.Map<MessageTemplateResponse>(updated);
    }
}