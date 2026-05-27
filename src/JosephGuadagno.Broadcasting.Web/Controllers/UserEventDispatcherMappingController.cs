using System.Globalization;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Constants;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Manages per-user event dispatcher mappings in the Web UI.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
[Route("Dispatchers/EventDispatcherMappings")]
public class UserEventDispatcherMappingController(
    IUserEventDispatcherMappingService mappingService,
    ISocialMediaPlatformService socialMediaPlatformService,
    ISetupService setupService) : Controller
{
    /// <summary>
    /// Lists all event dispatcher mappings for the authenticated user.
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var mappings = await mappingService.GetAllAsync();
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        var platformLookup = platforms.Items.ToDictionary(platform => platform.Id);

        var viewModels = mappings
            .Select(mapping => MapToViewModel(mapping, platformLookup))
            .ToList();

        return View(viewModels);
    }

    /// <summary>
    /// Shows the create form.
    /// </summary>
    [HttpGet("Create")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Create()
    {
        var viewModel = new UserEventDispatcherMappingViewModel
        {
            IsActive = true
        };

        await PopulateOptionsAsync(viewModel);
        return View(viewModel);
    }

    /// <summary>
    /// Creates a new event dispatcher mapping.
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Create(UserEventDispatcherMappingViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        var created = await mappingService.AddAsync(MapToDomainModel(viewModel));
        if (created is null)
        {
            TempData["ErrorMessage"] = "Unable to save the event dispatcher mapping.";
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Event dispatcher mapping saved successfully.";
        await setupService.InvalidateAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the edit form.
    /// </summary>
    [HttpGet("Edit/{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id)
    {
        var mapping = await mappingService.GetAsync(id);
        if (mapping is null)
        {
            return NotFound();
        }

        var viewModel = await BuildViewModelAsync(mapping);
        return View(viewModel);
    }

    /// <summary>
    /// Updates an existing event dispatcher mapping.
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id, UserEventDispatcherMappingViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        var existing = await mappingService.GetAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = MapToDomainModel(viewModel);
        updated.CreatedByEntraOid = existing.CreatedByEntraOid;
        updated.CreatedOn = existing.CreatedOn;
        updated.LastUpdatedOn = existing.LastUpdatedOn;

        var saved = await mappingService.UpdateAsync(updated);
        if (saved is null)
        {
            TempData["ErrorMessage"] = "Unable to update the event dispatcher mapping.";
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Event dispatcher mapping updated successfully.";
        await setupService.InvalidateAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the delete confirmation page.
    /// </summary>
    [HttpGet("Delete/{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Delete(int id)
    {
        var mapping = await mappingService.GetAsync(id);
        if (mapping is null)
        {
            return NotFound();
        }

        var viewModel = await BuildViewModelAsync(mapping);
        return View(viewModel);
    }

    /// <summary>
    /// Deletes the selected event dispatcher mapping.
    /// </summary>
    [HttpPost("Delete/{id:int}")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var mapping = await mappingService.GetAsync(id);
        if (mapping is null)
        {
            return NotFound();
        }

        var deleted = await mappingService.DeleteAsync(id);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Unable to delete the event dispatcher mapping.";
            var viewModel = await BuildViewModelAsync(mapping);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Event dispatcher mapping deleted successfully.";
        await setupService.InvalidateAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserEventDispatcherMappingViewModel> BuildViewModelAsync(UserEventDispatcherMapping mapping)
    {
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        var platformLookup = platforms.Items.ToDictionary(platform => platform.Id);
        var viewModel = MapToViewModel(mapping, platformLookup);
        viewModel.SocialMediaPlatforms = BuildPlatformOptions(platforms.Items, viewModel.SocialMediaPlatformId);
        viewModel.EventTypes = BuildEventTypeOptions(viewModel.EventType);
        return viewModel;
    }

    private async Task PopulateOptionsAsync(UserEventDispatcherMappingViewModel viewModel)
    {
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        viewModel.SocialMediaPlatforms = BuildPlatformOptions(platforms.Items, viewModel.SocialMediaPlatformId);
        viewModel.EventTypes = BuildEventTypeOptions(viewModel.EventType);
    }

    private static List<SelectListItem> BuildPlatformOptions(IEnumerable<SocialMediaPlatform> platforms, int selectedPlatformId)
    {
        return
        [
            new SelectListItem("Select a social media platform", string.Empty),
            .. platforms
                .Where(platform => platform.IsActive)
                .OrderBy(platform => platform.Name)
                .Select(platform => new SelectListItem(platform.Name, platform.Id.ToString(CultureInfo.InvariantCulture))
                {
                    Selected = platform.Id == selectedPlatformId
                })
        ];
    }

    private static List<SelectListItem> BuildEventTypeOptions(string? selectedEventType)
    {
        return
        [
            new SelectListItem("Select an event type", string.Empty),
            .. DispatcherEventTypes.All.Select(option => new SelectListItem(option.Label, option.Value)
            {
                Selected = string.Equals(option.Value, selectedEventType, StringComparison.OrdinalIgnoreCase)
            })
        ];
    }

    private static UserEventDispatcherMappingViewModel MapToViewModel(
        UserEventDispatcherMapping mapping,
        IReadOnlyDictionary<int, SocialMediaPlatform> platformLookup)
    {
        platformLookup.TryGetValue(mapping.SocialMediaPlatformId, out var platform);
        var eventType = DispatcherEventTypes.Get(mapping.EventType);

        return new UserEventDispatcherMappingViewModel
        {
            Id = mapping.Id,
            EventType = mapping.EventType,
            EventTypeDisplayName = eventType.Label,
            EventTypeIcon = eventType.Icon,
            SocialMediaPlatformId = mapping.SocialMediaPlatformId,
            SocialMediaPlatformName = platform?.Name ?? $"Platform {mapping.SocialMediaPlatformId}",
            SocialMediaPlatformIcon = platform?.Icon ?? "bi-broadcast",
            IsActive = mapping.IsActive,
            CreatedOn = mapping.CreatedOn,
            LastUpdatedOn = mapping.LastUpdatedOn
        };
    }

    private static UserEventDispatcherMapping MapToDomainModel(UserEventDispatcherMappingViewModel viewModel)
    {
        return new UserEventDispatcherMapping
        {
            Id = viewModel.Id,
            EventType = viewModel.EventType,
            SocialMediaPlatformId = viewModel.SocialMediaPlatformId,
            IsActive = viewModel.IsActive
        };
    }
}
