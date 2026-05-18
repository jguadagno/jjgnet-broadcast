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
/// Controller for managing per-user feed source collector configurations.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
[Route("Collectors/FeedSource/Settings")]
public class CollectorFeedSourcesController(
	IUserCollectorFeedSourceService service,
	IMapper mapper,
	ILogger<CollectorFeedSourcesController> logger)
	: Controller
{
	/// <summary>
    /// Lists the current user's feed sources.
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        int page = Pagination.DefaultPage,
        string sortBy = "displayName",
        bool sortDescending = false,
        string? filter = null)
    {
        var items = await service.GetCurrentUserAsync();
        var viewModels = mapper.Map<List<UserCollectorFeedSourceViewModel>>(items);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = items.Count;
        ViewBag.TotalPages = (int)Math.Ceiling(items.Count / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "CollectorFeedSources";
        ViewBag.ActionName = "Index";
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        ViewBag.Filter = filter;

        return View(viewModels);
    }

    /// <summary>
    /// Shows details for a feed source.
    /// </summary>
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var source = await service.GetByIdAsync(id);
        if (source == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this feed source.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = mapper.Map<UserCollectorFeedSourceViewModel>(source);
        return View(viewModel);
    }

    /// <summary>
    /// Displays the add feed source form.
    /// </summary>
    [HttpGet("Add")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public IActionResult Add()
    {
        return View(new UserCollectorFeedSourceViewModel());
    }

    /// <summary>
    /// Creates a new feed source for the current user.
    /// </summary>
    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Add(UserCollectorFeedSourceViewModel viewModel)
    {
        if (!ModelState.IsValid) return View(viewModel);

        var model = mapper.Map<UserCollectorFeedSource>(viewModel);
        model.CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty;

        var result = await service.AddCurrentUserAsync(model);
        if (result == null)
        {
            logger.LogWarning("Failed to add feed source for user, URL: {FeedUrl}",
                LogSanitizer.Sanitize(viewModel.FeedUrl));
            TempData["ErrorMessage"] = "Failed to add the feed source.";
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"Feed source '{LogSanitizer.Sanitize(viewModel.DisplayName)}' added successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    /// <summary>
    /// Displays the edit feed source form.
    /// </summary>
    [HttpGet("Edit/{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id)
    {
        var source = await service.GetByIdAsync(id);
        if (source == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this feed source.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = mapper.Map<UserCollectorFeedSourceViewModel>(source);
        return View(viewModel);
    }

    /// <summary>
    /// Updates an existing feed source.
    /// </summary>
    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(UserCollectorFeedSourceViewModel viewModel)
    {
        if (!ModelState.IsValid) return View(viewModel);

        var existing = await service.GetByIdAsync(viewModel.Id);
        if (existing == null) return NotFound();

        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        bool isSiteAdmin = User.IsInRole(RoleNames.SiteAdministrator);

        if (!isSiteAdmin && (currentUserOid == null || existing.CreatedByEntraOid != currentUserOid))
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this feed source.";
            return RedirectToAction(nameof(Index));
        }

        var model = mapper.Map<UserCollectorFeedSource>(viewModel);
        model.CreatedByEntraOid = existing.CreatedByEntraOid;

        UserCollectorFeedSource? result;
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
            logger.LogWarning("Failed to update feed source {Id}", viewModel.Id);
            ModelState.AddModelError(string.Empty, "Failed to update the feed source.");
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"Feed source '{LogSanitizer.Sanitize(viewModel.DisplayName)}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    /// <summary>
    /// Shows the delete confirmation page.
    /// </summary>
    [HttpGet("Delete/{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Delete(int id)
    {
        var source = await service.GetByIdAsync(id);
        if (source == null) return NotFound();

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
            if (currentUserOid == null || source.CreatedByEntraOid != currentUserOid)
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this feed source.";
                return RedirectToAction(nameof(Index));
            }
        }

        var viewModel = mapper.Map<UserCollectorFeedSourceViewModel>(source);
        return View(viewModel);
    }

    /// <summary>
    /// Deletes a feed source after confirmation.
    /// </summary>
    [HttpPost("Delete")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var source = await service.GetByIdAsync(id);
        if (source == null) return NotFound();

        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        bool isSiteAdmin = User.IsInRole(RoleNames.SiteAdministrator);

        if (!isSiteAdmin && (currentUserOid == null || source.CreatedByEntraOid != currentUserOid))
        {
            TempData["ErrorMessage"] = "You do not have permission to delete this feed source.";
            return RedirectToAction(nameof(Index));
        }

        bool result;
        if (isSiteAdmin && source.CreatedByEntraOid != currentUserOid)
        {
            result = await service.DeleteByUserAsync(source.CreatedByEntraOid, id);
        }
        else
        {
            result = await service.DeleteCurrentUserAsync(id);
        }

        if (result)
        {
            TempData["SuccessMessage"] = "Feed source deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = mapper.Map<UserCollectorFeedSourceViewModel>(source);
        ModelState.AddModelError(string.Empty, "Failed to delete the feed source.");
        return View(viewModel);
    }
}
