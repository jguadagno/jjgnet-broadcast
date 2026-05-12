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
/// Controller for managing per-user YouTube channel collector configurations.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
public class CollectorYouTubeChannelsController(
    IUserCollectorYouTubeChannelService service,
    IMapper mapper,
    ILogger<CollectorYouTubeChannelsController> logger)
    : Controller
{
    /// <summary>
    /// Lists the current user's YouTube channel configurations.
    /// </summary>
    public async Task<IActionResult> Index(
        int page = Pagination.DefaultPage,
        string sortBy = "displayName",
        bool sortDescending = false,
        string? filter = null)
    {
        var items = await service.GetCurrentUserAsync();
        var viewModels = mapper.Map<List<UserCollectorYouTubeChannelViewModel>>(items);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = items.Count;
        ViewBag.TotalPages = (int)Math.Ceiling(items.Count / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "CollectorYouTubeChannels";
        ViewBag.ActionName = "Index";
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        ViewBag.Filter = filter;

        return View(viewModels);
    }

    /// <summary>
    /// Shows details for a YouTube channel configuration.
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var channel = await service.GetByIdAsync(id);
        if (channel == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || channel.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this YouTube channel.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = mapper.Map<UserCollectorYouTubeChannelViewModel>(channel);
        return View(viewModel);
    }

    /// <summary>
    /// Displays the add YouTube channel form.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public IActionResult Add()
    {
        return View(new UserCollectorYouTubeChannelViewModel());
    }

    /// <summary>
    /// Creates a new YouTube channel configuration for the current user.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Add(UserCollectorYouTubeChannelViewModel viewModel)
    {
        if (string.IsNullOrWhiteSpace(viewModel.ApiKey))
        {
            ModelState.AddModelError(nameof(viewModel.ApiKey), "API Key is required.");
        }
        if (!ModelState.IsValid) return View(viewModel);

        var model = mapper.Map<UserCollectorYouTubeChannel>(viewModel);
        model.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty;

        var result = await service.AddCurrentUserAsync(model);
        if (result == null)
        {
            logger.LogWarning("Failed to add YouTube channel for user, ChannelId: {ChannelId}",
                LogSanitizer.Sanitize(viewModel.ChannelId));
            TempData["ErrorMessage"] = "Failed to add the YouTube channel.";
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"YouTube channel '{LogSanitizer.Sanitize(viewModel.DisplayName)}' added successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    /// <summary>
    /// Displays the edit YouTube channel form.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id)
    {
        var channel = await service.GetByIdAsync(id);
        if (channel == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || channel.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this YouTube channel.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = mapper.Map<UserCollectorYouTubeChannelViewModel>(channel);
        return View(viewModel);
    }

    /// <summary>
    /// Updates an existing YouTube channel configuration.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(UserCollectorYouTubeChannelViewModel viewModel)
    {
        if (!viewModel.HasApiKey && string.IsNullOrWhiteSpace(viewModel.ApiKey))
        {
            ModelState.AddModelError(nameof(viewModel.ApiKey), "API Key is required when no key has been previously stored.");
        }
        if (!ModelState.IsValid) return View(viewModel);

        var existing = await service.GetByIdAsync(viewModel.Id);
        if (existing == null) return NotFound();

        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        bool isSiteAdmin = User.IsInRole(RoleNames.SiteAdministrator);

        if (!isSiteAdmin && (currentUserOid == null || existing.CreatedByEntraOid != currentUserOid))
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this YouTube channel.";
            return RedirectToAction(nameof(Index));
        }

        var model = mapper.Map<UserCollectorYouTubeChannel>(viewModel);
        model.CreatedByEntraOid = existing.CreatedByEntraOid;

        UserCollectorYouTubeChannel? result;
        if (isSiteAdmin && existing.CreatedByEntraOid != currentUserOid)
        {
            result = await service.UpdateByUserAsync(existing.CreatedByEntraOid, model);
        }
        else
        {
            result = await service.UpdateCurrentUserAsync(model);
        }

        if (result == null)
        {
            logger.LogWarning("Failed to update YouTube channel {Id}", viewModel.Id);
            ModelState.AddModelError(string.Empty, "Failed to update the YouTube channel.");
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"YouTube channel '{LogSanitizer.Sanitize(viewModel.DisplayName)}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Delete(int id)
    {
        var channel = await service.GetByIdAsync(id);
        if (channel == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || channel.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this YouTube channel.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = mapper.Map<UserCollectorYouTubeChannelViewModel>(channel);
        return View(viewModel);
    }

    /// <summary>
    /// Deletes a YouTube channel configuration after confirmation.
    /// </summary>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var channel = await service.GetByIdAsync(id);
        if (channel == null) return NotFound();

        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        bool isSiteAdmin = User.IsInRole(RoleNames.SiteAdministrator);

        if (!isSiteAdmin && (currentUserOid == null || channel.CreatedByEntraOid != currentUserOid))
        {
            TempData["ErrorMessage"] = "You do not have permission to delete this YouTube channel.";
            return RedirectToAction(nameof(Index));
        }

        bool result;
        if (isSiteAdmin && channel.CreatedByEntraOid != currentUserOid)
        {
            result = await service.DeleteByUserAsync(channel.CreatedByEntraOid, id);
        }
        else
        {
            result = await service.DeleteCurrentUserAsync(id);
        }

        if (result)
        {
            TempData["SuccessMessage"] = "YouTube channel deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = mapper.Map<UserCollectorYouTubeChannelViewModel>(channel);
        ModelState.AddModelError(string.Empty, "Failed to delete the YouTube channel.");
        return View(viewModel);
    }
}
