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
/// Controller for managing the per-user scheduled item collector configuration.
/// Each user has at most one scheduled item configuration.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("Collectors/ScheduledItems")]
public class CollectorScheduledItemController(
    IUserCollectorScheduledItemService service,
    ILogger<CollectorScheduledItemController> logger)
    : Controller
{
    /// <summary>
    /// Shows the current user's scheduled item configuration.
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var ownerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            TempData["ErrorMessage"] = "Unable to resolve your account identifier.";
            return RedirectToAction("Index", "Home");
        }

        var item = await service.GetAsync(ownerOid);
        if (item is null)
        {
            return View((UserCollectorScheduledItemViewModel?)null);
        }

        var viewModel = new UserCollectorScheduledItemViewModel
        {
            DisplayName = item.DisplayName,
            IsActive = item.IsActive,
            CreatedOn = item.CreatedOn,
            LastUpdatedOn = item.LastUpdatedOn
        };
        return View(viewModel);
    }

    /// <summary>
    /// Displays the edit/create form for the scheduled item configuration.
    /// </summary>
    [HttpGet("Edit")]
    public async Task<IActionResult> Edit()
    {
        var ownerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            TempData["ErrorMessage"] = "Unable to resolve your account identifier.";
            return RedirectToAction("Index", "Home");
        }

        var item = await service.GetAsync(ownerOid);
        var viewModel = item is not null
            ? new UserCollectorScheduledItemViewModel
            {
                DisplayName = item.DisplayName,
                IsActive = item.IsActive,
                CreatedOn = item.CreatedOn,
                LastUpdatedOn = item.LastUpdatedOn
            }
            : new UserCollectorScheduledItemViewModel();

        return View(viewModel);
    }

    /// <summary>
    /// Creates or updates the scheduled item configuration for the current user.
    /// </summary>
    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserCollectorScheduledItemViewModel viewModel)
    {
        if (!ModelState.IsValid) return View(viewModel);

        var ownerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            TempData["ErrorMessage"] = "Unable to resolve your account identifier.";
            return RedirectToAction("Index", "Home");
        }

        var model = new UserCollectorScheduledItem
        {
            CreatedByEntraOid = ownerOid,
            DisplayName = viewModel.DisplayName,
            IsActive = viewModel.IsActive
        };

        var result = await service.SaveAsync(model);
        if (result is null)
        {
            logger.LogWarning("Failed to save scheduled item config for user {OwnerOid}",
                LogSanitizer.Sanitize(ownerOid));
            ModelState.AddModelError(string.Empty, "Failed to save the scheduled item configuration.");
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Scheduled item configuration saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the delete confirmation page.
    /// </summary>
    [HttpGet("Delete")]
    public async Task<IActionResult> Delete()
    {
        var ownerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            TempData["ErrorMessage"] = "Unable to resolve your account identifier.";
            return RedirectToAction("Index", "Home");
        }

        var item = await service.GetAsync(ownerOid);
        if (item is null) return NotFound();

        var viewModel = new UserCollectorScheduledItemViewModel
        {
            DisplayName = item.DisplayName,
            IsActive = item.IsActive,
            CreatedOn = item.CreatedOn,
            LastUpdatedOn = item.LastUpdatedOn
        };
        return View(viewModel);
    }

    /// <summary>
    /// Deletes the scheduled item configuration after confirmation.
    /// </summary>
    [HttpPost("Delete")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed()
    {
        var ownerOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            TempData["ErrorMessage"] = "Unable to resolve your account identifier.";
            return RedirectToAction("Index", "Home");
        }

        var result = await service.DeleteAsync(ownerOid);
        if (result)
        {
            TempData["SuccessMessage"] = "Scheduled item configuration deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        logger.LogWarning("Failed to delete scheduled item config for user {OwnerOid}",
            LogSanitizer.Sanitize(ownerOid));
        TempData["ErrorMessage"] = "Failed to delete the scheduled item configuration.";
        return RedirectToAction(nameof(Index));
    }
}
