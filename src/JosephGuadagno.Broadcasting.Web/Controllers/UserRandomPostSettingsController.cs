using System.Globalization;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Manages per-user random post settings in the Web UI.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireViewer)]
[Route("Publishers/RandomPostSettings")]
public class UserRandomPostSettingsController(
    IUserRandomPostSettingsService settingsService,
    ISocialMediaPlatformService socialMediaPlatformService,
    ISetupService setupService) : Controller
{
    /// <summary>
    /// Lists all random post settings for the authenticated user.
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var settings = await settingsService.GetAllAsync();
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        var platformLookup = platforms.Items.ToDictionary(platform => platform.Id);

        var viewModels = settings
            .Select(setting => MapToViewModel(setting, platformLookup))
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
        var viewModel = new UserRandomPostSettingsViewModel
        {
            IsActive = true
        };

        await PopulatePlatformOptionsAsync(viewModel);
        return View(viewModel);
    }

    /// <summary>
    /// Creates a new random post settings record.
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Create(UserRandomPostSettingsViewModel viewModel)
    {
        if (!TryParseUtcCutoffDate(viewModel.CutoffDateUtc, out var cutoffDate))
        {
            ModelState.AddModelError(nameof(UserRandomPostSettingsViewModel.CutoffDateLocal), "Enter a valid cutoff date.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePlatformOptionsAsync(viewModel);
            return View(viewModel);
        }

        var created = await settingsService.AddAsync(MapToDomainModel(viewModel, cutoffDate));
        if (created is null)
        {
            TempData["ErrorMessage"] = "Unable to save the random post settings.";
            await PopulatePlatformOptionsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Random post settings saved successfully.";
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
        var settings = await settingsService.GetAsync(id);
        if (settings is null)
        {
            return NotFound();
        }

        var viewModel = await BuildViewModelAsync(settings);
        return View(viewModel);
    }

    /// <summary>
    /// Updates an existing random post settings record.
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> Edit(int id, UserRandomPostSettingsViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!TryParseUtcCutoffDate(viewModel.CutoffDateUtc, out var cutoffDate))
        {
            ModelState.AddModelError(nameof(UserRandomPostSettingsViewModel.CutoffDateLocal), "Enter a valid cutoff date.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePlatformOptionsAsync(viewModel);
            return View(viewModel);
        }

        var existing = await settingsService.GetAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = MapToDomainModel(viewModel, cutoffDate);
        updated.CreatedByEntraOid = existing.CreatedByEntraOid;
        updated.CreatedOn = existing.CreatedOn;
        updated.LastUpdatedOn = existing.LastUpdatedOn;

        var saved = await settingsService.UpdateAsync(updated);
        if (saved is null)
        {
            TempData["ErrorMessage"] = "Unable to update the random post settings.";
            await PopulatePlatformOptionsAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Random post settings updated successfully.";
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
        var settings = await settingsService.GetAsync(id);
        if (settings is null)
        {
            return NotFound();
        }

        var viewModel = await BuildViewModelAsync(settings);
        return View(viewModel);
    }

    /// <summary>
    /// Deletes the selected random post settings record.
    /// </summary>
    [HttpPost("Delete/{id:int}")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var settings = await settingsService.GetAsync(id);
        if (settings is null)
        {
            return NotFound();
        }

        var deleted = await settingsService.DeleteAsync(id);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Unable to delete the random post settings.";
            var viewModel = await BuildViewModelAsync(settings);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Random post settings deleted successfully.";
        await setupService.InvalidateAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserRandomPostSettingsViewModel> BuildViewModelAsync(UserRandomPostSettings settings)
    {
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        var platformLookup = platforms.Items.ToDictionary(platform => platform.Id);
        var viewModel = MapToViewModel(settings, platformLookup);
        viewModel.SocialMediaPlatforms = BuildPlatformOptions(platforms.Items, viewModel.SocialMediaPlatformId);
        return viewModel;
    }

    private async Task PopulatePlatformOptionsAsync(UserRandomPostSettingsViewModel viewModel)
    {
        var platforms = await socialMediaPlatformService.GetAllAsync(pageSize: 100);
        viewModel.SocialMediaPlatforms = BuildPlatformOptions(platforms.Items, viewModel.SocialMediaPlatformId);
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

    private static UserRandomPostSettingsViewModel MapToViewModel(
        UserRandomPostSettings settings,
        IReadOnlyDictionary<int, SocialMediaPlatform> platformLookup)
    {
        platformLookup.TryGetValue(settings.SocialMediaPlatformId, out var platform);

        return new UserRandomPostSettingsViewModel
        {
            Id = settings.Id,
            SocialMediaPlatformId = settings.SocialMediaPlatformId,
            SocialMediaPlatformName = platform?.Name ?? $"Platform {settings.SocialMediaPlatformId}",
            SocialMediaPlatformIcon = platform?.Icon ?? "bi-broadcast",
            CronExpression = settings.CronExpression,
            CutoffDateUtc = settings.CutoffDate?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            ExcludedCategoriesText = string.Join(", ", settings.ExcludedCategories),
            IsActive = settings.IsActive,
            CreatedOn = settings.CreatedOn,
            LastUpdatedOn = settings.LastUpdatedOn
        };
    }

    private static UserRandomPostSettings MapToDomainModel(UserRandomPostSettingsViewModel viewModel, DateTimeOffset? cutoffDate)
    {
        return new UserRandomPostSettings
        {
            Id = viewModel.Id,
            SocialMediaPlatformId = viewModel.SocialMediaPlatformId,
            CronExpression = viewModel.CronExpression.Trim(),
            CutoffDate = cutoffDate,
            ExcludedCategories = viewModel.ExcludedCategoriesText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            IsActive = viewModel.IsActive
        };
    }

    private static bool TryParseUtcCutoffDate(string? cutoffDateUtc, out DateTimeOffset? cutoffDate)
    {
        cutoffDate = null;

        if (string.IsNullOrWhiteSpace(cutoffDateUtc))
        {
            return true;
        }

        if (!DateTimeOffset.TryParse(
                cutoffDateUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return false;
        }

        cutoffDate = parsed;
        return true;
    }
}
