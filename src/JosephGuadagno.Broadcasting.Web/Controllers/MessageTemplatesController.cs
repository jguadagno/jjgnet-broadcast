using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// The controller for managing message templates.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
public class MessageTemplatesController : Controller
{
    private readonly IMessageTemplateService _messageTemplateService;
    private readonly ISocialMediaPlatformService _socialMediaPlatformService;
    private readonly IMapper _mapper;
    private readonly ILogger<MessageTemplatesController> _logger;

    /// <summary>
    /// The constructor for the message templates controller.
    /// </summary>
    /// <param name="messageTemplateService">The message template service</param>
    /// <param name="socialMediaPlatformService">The social media platform service</param>
    /// <param name="mapper">The mapper service</param>
    /// <param name="logger">The logger</param>
    public MessageTemplatesController(IMessageTemplateService messageTemplateService,
        ISocialMediaPlatformService socialMediaPlatformService, IMapper mapper,
        ILogger<MessageTemplatesController> logger)
    {
        _messageTemplateService = messageTemplateService;
        _socialMediaPlatformService = socialMediaPlatformService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Lists all message templates grouped by platform.
    /// </summary>
    public async Task<IActionResult> Index(int page = Pagination.DefaultPage, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, string? selectedPlatform = null)
    {
        var result = await _messageTemplateService.GetAllAsync(page: 1, pageSize: 100, sortBy, sortDescending, filter);
        var allViewModels = _mapper.Map<List<MessageTemplateViewModel>>(result?.Items ?? []);

        var platformsResult = await _socialMediaPlatformService.GetAllAsync(pageSize: 100);
        var platformIcons = (platformsResult?.Items ?? [])
            .ToDictionary(p => p.Name, p => p.Icon ?? "bi-broadcast");
        ViewBag.PlatformIcons = platformIcons;

        var platforms = allViewModels.Select(t => t.Platform).Distinct().OrderBy(p => p).ToList();

        var filteredViewModels = string.IsNullOrEmpty(selectedPlatform)
            ? allViewModels
            : allViewModels.Where(t => t.Platform == selectedPlatform).ToList();

        // Compute which system defaults the user hasn't yet claimed
        var userTemplateKeys = new HashSet<(string Platform, string MessageType)>(
            allViewModels.Select(t => (t.Platform, t.MessageType)));
        var defaultTemplates = await _messageTemplateService.GetAllDefaultsAsync();
        var availableDefaults = (defaultTemplates ?? [])
            .Select(d => _mapper.Map<MessageTemplateViewModel>(d))
            .Where(d => !userTemplateKeys.Contains((d.Platform, d.MessageType)))
            .OrderBy(d => d.Platform)
            .ThenBy(d => d.MessageType)
            .ToList();

        ViewBag.Page = page;
        ViewBag.PageSize = 100;
        ViewBag.TotalCount = filteredViewModels.Count;
        ViewBag.TotalPages = 1;
        ViewBag.ControllerName = "MessageTemplates";
        ViewBag.ActionName = "Index";
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        ViewBag.Filter = filter;
        ViewBag.Platforms = platforms;
        ViewBag.SelectedPlatform = selectedPlatform;
        ViewBag.AvailableDefaults = availableDefaults;

        return View(filteredViewModels);
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

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || template.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this message template.";
                return RedirectToAction("Index");
            }
        }

        return View(_mapper.Map<MessageTemplateViewModel>(template));
    }

    /// <summary>
    /// Saves changes to a message template.
    /// </summary>
    /// <param name="model">The updated message template view model</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MessageTemplateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var template = _mapper.Map<Domain.Models.MessageTemplate>(model);
        var saved = await _messageTemplateService.UpdateAsync(model.Platform, template);
        if (saved is null)
        {
            _logger.LogWarning("Failed to save MessageTemplate for Platform={Platform}, MessageType={MessageType}",
                LogSanitizer.Sanitize(model.Platform), LogSanitizer.Sanitize(model.MessageType));
            ModelState.AddModelError(string.Empty, "Failed to save the message template.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Message template saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the create form for a message template, pre-populated from the system default.
    /// </summary>
    /// <param name="platform">The platform name</param>
    /// <param name="messageType">The message type</param>
    [HttpGet]
    public async Task<IActionResult> Create(string platform, string messageType)
    {
        var defaultTemplate = await _messageTemplateService.GetDefaultAsync(platform, messageType);
        MessageTemplateViewModel viewModel;
        if (defaultTemplate is not null)
        {
            viewModel = _mapper.Map<MessageTemplateViewModel>(defaultTemplate);
        }
        else
        {
            viewModel = new MessageTemplateViewModel
            {
                Platform = platform,
                MessageType = messageType,
                Template = string.Empty,
                Description = string.Empty
            };
        }
        return View(viewModel);
    }

    /// <summary>
    /// Creates a new user-owned message template.
    /// </summary>
    /// <param name="model">The message template view model</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MessageTemplateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var template = _mapper.Map<Domain.Models.MessageTemplate>(model);
        var saved = await _messageTemplateService.CreateAsync(model.Platform, template);
        if (saved is null)
        {
            _logger.LogWarning("Failed to create MessageTemplate for Platform={Platform}, MessageType={MessageType}",
                LogSanitizer.Sanitize(model.Platform), LogSanitizer.Sanitize(model.MessageType));
            ModelState.AddModelError(string.Empty, "Failed to create the message template.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Message template created successfully.";
        return RedirectToAction(nameof(Index));
    }
}
