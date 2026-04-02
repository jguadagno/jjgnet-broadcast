using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for managing message templates.
/// </summary>
[Authorize(Policy = "RequireContributor")]
public class MessageTemplatesController : Controller
{
    private readonly IMessageTemplateService _messageTemplateService;
    private readonly IMapper _mapper;
    private readonly ILogger<MessageTemplatesController> _logger;

    /// <summary>
    /// The constructor for the message templates controller.
    /// </summary>
    /// <param name="messageTemplateService">The message template service</param>
    /// <param name="mapper">The mapper service</param>
    /// <param name="logger">The logger</param>
    public MessageTemplatesController(IMessageTemplateService messageTemplateService, IMapper mapper,
        ILogger<MessageTemplatesController> logger)
    {
        _messageTemplateService = messageTemplateService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Lists all message templates grouped by platform.
    /// </summary>
    public async Task<IActionResult> Index(int page = Pagination.DefaultPage)
    {
        var result = await _messageTemplateService.GetAllAsync(page, Pagination.DefaultPageSize);
        var viewModels = _mapper.Map<List<MessageTemplateViewModel>>(result?.Items ?? []);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = result?.TotalCount ?? 0;
        ViewBag.TotalPages = (int)Math.Ceiling((result?.TotalCount ?? 0) / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "MessageTemplates";
        ViewBag.ActionName = "Index";

        return View(viewModels);
    }

    /// <summary>
    /// Shows the edit form for a message template.
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    [HttpGet]
    public async Task<IActionResult> Edit(string platform, string messageType)
    {
        var template = await _messageTemplateService.GetAsync(platform, messageType);
        if (template is null)
        {
            return NotFound();
        }
        return View(_mapper.Map<MessageTemplateViewModel>(template));
    }

    /// <summary>
    /// Saves changes to a message template.
    /// </summary>
    /// <param name="model">The updated message template view model</param>
    [HttpPost]
    public async Task<IActionResult> Edit(MessageTemplateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var template = _mapper.Map<Domain.Models.MessageTemplate>(model);
        var saved = await _messageTemplateService.UpdateAsync(template);
        if (saved is null)
        {
            _logger.LogWarning("Failed to save MessageTemplate for Platform={Platform}, MessageType={MessageType}",
                model.Platform, model.MessageType);
            ModelState.AddModelError(string.Empty, "Failed to save the message template.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Message template saved successfully.";
        return RedirectToAction(nameof(Index));
    }
}