using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for managing social media platforms in the admin UI.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
public class SocialMediaPlatformsController : Controller
{
    private readonly ISocialMediaPlatformService _platformService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor for SocialMediaPlatformsController.
    /// </summary>
    public SocialMediaPlatformsController(ISocialMediaPlatformService platformService, IMapper mapper)
    {
        _platformService = platformService;
        _mapper = mapper;
    }

    /// <summary>
    /// Lists all social media platforms (active and inactive).
    /// </summary>
    public async Task<IActionResult> Index(int page = Pagination.DefaultPage, string sortBy = "name", bool sortDescending = false, string? filter = null)
    {
        var result = await _platformService.GetAllAsync(page, Pagination.DefaultPageSize, sortBy, sortDescending, filter, includeInactive: true);
        var viewModels = _mapper.Map<List<SocialMediaPlatformViewModel>>(result.Items);

        ViewBag.Page = page;
        ViewBag.PageSize = Pagination.DefaultPageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)Pagination.DefaultPageSize);
        ViewBag.ControllerName = "SocialMediaPlatforms";
        ViewBag.ActionName = "Index";
        ViewBag.SortBy = sortBy;
        ViewBag.SortDescending = sortDescending;
        ViewBag.Filter = filter;

        return View(viewModels);
    }

    /// <summary>
    /// Shows the form to add a new social media platform.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public IActionResult Add()
    {
        return View(new SocialMediaPlatformViewModel { IsActive = true });
    }

    /// <summary>
    /// Creates a new social media platform.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Add(SocialMediaPlatformViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var platform = _mapper.Map<Domain.Models.SocialMediaPlatform>(viewModel);
        var created = await _platformService.AddAsync(platform);
        if (created is null)
        {
            TempData["ErrorMessage"] = "Failed to add the social media platform.";
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"Social media platform '{created.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the edit form for a social media platform.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id)
    {
        var platform = await _platformService.GetByIdAsync(id);
        if (platform is null)
        {
            return NotFound();
        }

        var viewModel = _mapper.Map<SocialMediaPlatformViewModel>(platform);
        return View(viewModel);
    }

    /// <summary>
    /// Saves changes to an existing social media platform.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(SocialMediaPlatformViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var platform = _mapper.Map<Domain.Models.SocialMediaPlatform>(viewModel);
        var updated = await _platformService.UpdateAsync(platform);
        if (updated is null)
        {
            TempData["ErrorMessage"] = "Failed to update the social media platform.";
            return View(viewModel);
        }

        TempData["SuccessMessage"] = $"Social media platform '{updated.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Toggles the IsActive status of a social media platform (soft delete / reactivate).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var success = await _platformService.ToggleActiveAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = "Failed to toggle the platform's active status.";
        }
        else
        {
            TempData["SuccessMessage"] = "Platform active status toggled successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the delete confirmation page for a social media platform.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireSiteAdministrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var platform = await _platformService.GetByIdAsync(id);
        if (platform is null)
        {
            return NotFound();
        }

        var viewModel = _mapper.Map<SocialMediaPlatformViewModel>(platform);
        return View(viewModel);
    }

    /// <summary>
    /// Permanently deletes a social media platform after confirmation.
    /// </summary>
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireSiteAdministrator)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _platformService.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Social media platform deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        var platform = await _platformService.GetByIdAsync(id);
        if (platform is not null)
        {
            var viewModel = _mapper.Map<SocialMediaPlatformViewModel>(platform);
            ModelState.AddModelError(string.Empty, "Failed to delete the social media platform.");
            return View(viewModel);
        }

        TempData["ErrorMessage"] = "Failed to delete the social media platform.";
        return RedirectToAction(nameof(Index));
    }
}
