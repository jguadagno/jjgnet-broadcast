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
/// Manages per-user event publisher mappings in the Web UI.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
[Route("Publishers/EventPublisherMappings")]
public class UserEventPublisherMappingController(
    IUserEventPublisherMappingService mappingService,
    ISocialMediaPlatformService socialMediaPlatformService,
    ISetupService setupService) : Controller
{
    /// <summary>
    /// Lists all event publisher mappings for the authenticated user.
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
        var viewModel = new UserEventPublisherMappingViewModel
        {
            IsActive = true
        };

        await PopulateOptionsAsync(viewModel);
        return View(viewModel);
    }

    /// <summary>
    /// Creates a new event publisher mapping.
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Create(UserEventPublisherMappingViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        var created = await mappingService.AddAsync(MapToDomainModel(viewModel));
        if (created is null)
        {
            TempData["ErrorMessage"] = "Unable to save the event publisher mapping.";
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Event publisher mapping saved successfully.";
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
    /// Updates an existing event publisher mapping.
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id, UserEventPublisherMappingViewModel viewModel)
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
            TempData["ErrorMessage"] = "Unable to update the event publisher mapping.";
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Event publisher mapping updated successfully.";
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
    /// Deletes the selected event publisher mapping.
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
            TempData["ErrorMessage"] = "Unable to delete the event publisher mapping.";
            var viewModel = await BuildViewModelAsync(mapping);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Event publisher mapping deleted successfully.";
        await setupService.InvalidateAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserEventPublisherMappingViewModel> BuildViewModelAsync(UserEventPublisherMapping mapping)
    {
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        var platformLookup = platforms.Items.ToDictionary(platform => platform.Id);
        var viewModel = MapToViewModel(mapping, platformLookup);
        viewModel.SocialMediaPlatforms = BuildPlatformOptions(platforms.Items, viewModel.SocialMediaPlatformId);
        viewModel.EventTypes = BuildEventTypeOptions(viewModel.EventType);
        return viewModel;
    }

    private async Task PopulateOptionsAsync(UserEventPublisherMappingViewModel viewModel)
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
            .. PublisherEventTypes.All.Select(option => new SelectListItem(option.Label, option.Value)
            {
                Selected = string.Equals(option.Value, selectedEventType, StringComparison.OrdinalIgnoreCase)
            })
        ];
    }

    private static UserEventPublisherMappingViewModel MapToViewModel(
        UserEventPublisherMapping mapping,
        IReadOnlyDictionary<int, SocialMediaPlatform> platformLookup)
    {
        platformLookup.TryGetValue(mapping.SocialMediaPlatformId, out var platform);
        var eventType = PublisherEventTypes.Get(mapping.EventType);

        return new UserEventPublisherMappingViewModel
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

    private static UserEventPublisherMapping MapToDomainModel(UserEventPublisherMappingViewModel viewModel)
    {
        return new UserEventPublisherMapping
        {
            Id = viewModel.Id,
            EventType = viewModel.EventType,
            SocialMediaPlatformId = viewModel.SocialMediaPlatformId,
            IsActive = viewModel.IsActive
        };
    }
}
