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
[Route("[controller]")]
[Produces("application/json")]
public class MessageTemplatesController : ControllerBase
{
    private readonly IMessageTemplateDataStore _messageTemplateDataStore;
    private readonly ILogger<MessageTemplatesController> _logger;

    /// <summary>
    /// Handles the interactions with Message Templates
    /// </summary>
    /// <param name="messageTemplateDataStore">The message template data store</param>
    /// <param name="logger">The logger</param>
    public MessageTemplatesController(IMessageTemplateDataStore messageTemplateDataStore,
        ILogger<MessageTemplatesController> logger)
    {
        _messageTemplateDataStore = messageTemplateDataStore;
        _logger = logger;
    }

    /// <summary>
    /// Gets all message templates
    /// </summary>
    /// <returns>A list of message templates</returns>
    /// <response code="200">If the call was successful</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MessageTemplate>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MessageTemplate>>> GetAllAsync()
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.MessageTemplates.All);
        return await _messageTemplateDataStore.GetAllAsync();
    }

    /// <summary>
    /// Gets a message template by platform and message type
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <returns>A <see cref="MessageTemplate"/></returns>
    /// <response code="200">If the item was found</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpGet("{platform}/{messageType}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplate))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplate>> GetAsync(string platform, string messageType)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.MessageTemplates.All);
        var template = await _messageTemplateDataStore.GetAsync(platform, messageType);
        if (template is null)
        {
            _logger.LogWarning("MessageTemplate not found for Platform={Platform}, MessageType={MessageType}", platform, messageType);
            return NotFound();
        }
        return template;
    }

    /// <summary>
    /// Updates a message template
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    /// <param name="messageTemplate">The updated message template</param>
    /// <returns>The updated <see cref="MessageTemplate"/></returns>
    /// <response code="200">If the item was updated successfully</response>
    /// <response code="400">If the route parameters do not match the body</response>
    /// <response code="404">If the item was not found</response>
    /// <response code="401">If the current user was unauthorized to access this endpoint</response>
    [HttpPut("{platform}/{messageType}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageTemplate))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageTemplate>> UpdateAsync(string platform, string messageType,
        [FromBody] MessageTemplate messageTemplate)
    {
        HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.MessageTemplates.All);

        if (!platform.Equals(messageTemplate.Platform, StringComparison.OrdinalIgnoreCase) ||
            !messageType.Equals(messageTemplate.MessageType, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Route parameters must match the message template body.");
        }

        var updated = await _messageTemplateDataStore.UpdateAsync(messageTemplate);
        if (updated is null)
        {
            _logger.LogWarning("MessageTemplate not found for update: Platform={Platform}, MessageType={MessageType}", platform, messageType);
            return NotFound();
        }

        _logger.LogInformation("MessageTemplate updated for Platform={Platform}, MessageType={MessageType}", platform, messageType);
        return updated;
    }
}
