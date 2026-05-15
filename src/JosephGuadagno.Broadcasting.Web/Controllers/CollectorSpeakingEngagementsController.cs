using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for managing per-user speaking engagement collector configurations.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
public class CollectorSpeakingEngagementsController : Controller
{
    private readonly IUserCollectorSpeakingEngagementService _service;
    private readonly IMapper _mapper;
    private readonly ILogger<CollectorSpeakingEngagementsController> _logger;

    public CollectorSpeakingEngagementsController(
        IUserCollectorSpeakingEngagementService service,
        IMapper mapper,
        ILogger<CollectorSpeakingEngagementsController> logger)
    {
        _service = service;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Lists the current user's speaking engagement configurations.
    /// </summary>
    public async Task<IActionResult> Index(
        int page = Pagination.DefaultPage,
        string sortBy = "displayName",
        bool sortDescending = false,
        string? filter = null)
    {
        var items = await _service.GetCurrentUserAsync();
        var viewModels = _mapper.Map<List<UserCollectorSpeakingEngagementViewModel>>(items);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = items.Count;
        ViewBag.TotalPages = (int)Math.Ceiling(items.Count / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "CollectorSpeakingEngagements";
        ViewBag.ActionName = "Index";
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        ViewBag.Filter = filter;

        return View(viewModels);
    }

    /// <summary>
    /// Shows details for a speaking engagement configuration.
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var engagement = await _service.GetByIdAsync(id);
        if (engagement == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || engagement.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this speaking engagement.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = _mapper.Map<UserCollectorSpeakingEngagementViewModel>(engagement);
        return View(viewModel);
    }

    /// <summary>
    /// Displays the add speaking engagement form.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public IActionResult Add()
    {
        return View(new UserCollectorSpeakingEngagementViewModel());
    }

    /// <summary>
    /// Creates a new speaking engagement configuration for the current user.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Add(UserCollectorSpeakingEngagementViewModel viewModel)
    {
        if (!ModelState.IsValid) return View(viewModel);

        var model = _mapper.Map<UserCollectorSpeakingEngagement>(viewModel);
        model.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty;

        var result = await _service.AddCurrentUserAsync(model);
        if (result == null)
        {
            _logger.LogWarning("Failed to add speaking engagement for user, URL: {FileUrl}",
                LogSanitizer.Sanitize(viewModel.SpeakingEngagementsFile));
            TempData["ErrorMessage"] = "Failed to add the speaking engagement.";
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"Speaking engagement '{LogSanitizer.Sanitize(viewModel.DisplayName)}' added successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    /// <summary>
    /// Displays the edit speaking engagement form.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id)
    {
        var engagement = await _service.GetByIdAsync(id);
        if (engagement == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || engagement.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this speaking engagement.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = _mapper.Map<UserCollectorSpeakingEngagementViewModel>(engagement);
        return View(viewModel);
    }

    /// <summary>
    /// Updates an existing speaking engagement configuration.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(UserCollectorSpeakingEngagementViewModel viewModel)
    {
        if (!ModelState.IsValid) return View(viewModel);

        var existing = await _service.GetByIdAsync(viewModel.Id);
        if (existing == null) return NotFound();

        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        bool isSiteAdmin = User.IsInRole(RoleNames.SiteAdministrator);

        if (!isSiteAdmin && (currentUserOid == null || existing.CreatedByEntraOid != currentUserOid))
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this speaking engagement.";
            return RedirectToAction(nameof(Index));
        }

        var model = _mapper.Map<UserCollectorSpeakingEngagement>(viewModel);
        model.CreatedByEntraOid = existing.CreatedByEntraOid;

        UserCollectorSpeakingEngagement? result;
        if (isSiteAdmin && existing.CreatedByEntraOid != currentUserOid)
        {
            result = await _service.UpdateByUserAsync(existing.CreatedByEntraOid, model);
        }
        else
        {
            result = await _service.UpdateCurrentUserAsync(model);
        }

        if (result == null)
        {
            _logger.LogWarning("Failed to update speaking engagement {Id}", viewModel.Id);
            ModelState.AddModelError(string.Empty, "Failed to update the speaking engagement.");
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"Speaking engagement '{LogSanitizer.Sanitize(viewModel.DisplayName)}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Delete(int id)
    {
        var engagement = await _service.GetByIdAsync(id);
        if (engagement == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || engagement.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this speaking engagement.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = _mapper.Map<UserCollectorSpeakingEngagementViewModel>(engagement);
        return View(viewModel);
    }

    /// <summary>
    /// Deletes a speaking engagement configuration after confirmation.
    /// </summary>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var engagement = await _service.GetByIdAsync(id);
        if (engagement == null) return NotFound();

        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        bool isSiteAdmin = User.IsInRole(RoleNames.SiteAdministrator);

        if (!isSiteAdmin && (currentUserOid == null || engagement.CreatedByEntraOid != currentUserOid))
        {
            TempData["ErrorMessage"] = "You do not have permission to delete this speaking engagement.";
            return RedirectToAction(nameof(Index));
        }

        bool result;
        if (isSiteAdmin && engagement.CreatedByEntraOid != currentUserOid)
        {
            result = await _service.DeleteByUserAsync(engagement.CreatedByEntraOid, id);
        }
        else
        {
            result = await _service.DeleteCurrentUserAsync(id);
        }

        if (result)
        {
            TempData["SuccessMessage"] = "Speaking engagement deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = _mapper.Map<UserCollectorSpeakingEngagementViewModel>(engagement);
        ModelState.AddModelError(string.Empty, "Failed to delete the speaking engagement.");
        return View(viewModel);
    }
}
