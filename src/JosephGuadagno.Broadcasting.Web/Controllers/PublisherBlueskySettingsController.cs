using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>Manages Bluesky publisher settings for the current user.</summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("Publishers/Bluesky/Settings")]
public class PublisherBlueskySettingsController(
    IUserPublisherBlueskySettingsService service,
    ILogger<PublisherBlueskySettingsController> logger) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var settings = await service.GetCurrentUserAsync();
        var viewModel = MapToViewModel(settings);
        return View(viewModel);
    }

    [HttpGet("Edit")]
    public async Task<IActionResult> Edit()
    {
        var settings = await service.GetCurrentUserAsync();
        var viewModel = MapToViewModel(settings);
        return View(viewModel);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BlueskyPublisherSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var settings = new UserPublisherBlueskySettings
        {
            Id = model.Id,
            IsEnabled = model.IsEnabled,
            UserName = model.UserName,
            HasAppPassword = model.HasAppPassword,
            CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty
        };

        var saved = await service.SaveCurrentUserAsync(settings, model.AppPassword);
        if (saved is null)
        {
            logger.LogWarning("Bluesky settings save failed for user '{UserOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            TempData["ErrorMessage"] = "Unable to save Bluesky publisher settings right now.";
            return View("Edit", model);
        }

        TempData["SuccessMessage"] = "Bluesky settings saved.";
        return RedirectToAction(nameof(Index));
    }

    private static BlueskyPublisherSettingsViewModel MapToViewModel(UserPublisherBlueskySettings? settings) =>
        new()
        {
            Id = settings?.Id ?? 0,
            IsEnabled = settings?.IsEnabled ?? false,
            UserName = settings?.UserName,
            HasAppPassword = settings?.HasAppPassword ?? false,
            PlatformName = "Bluesky",
            PlatformIcon = "bi-cloud"
        };
}
