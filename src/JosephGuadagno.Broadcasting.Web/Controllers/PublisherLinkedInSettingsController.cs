using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>Manages LinkedIn publisher settings for the current user.</summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("Publishers/LinkedIn/Settings")]
public class PublisherLinkedInSettingsController(
    IUserPublisherLinkedInSettingsService service,
    ISetupService setupService,
    ILogger<PublisherLinkedInSettingsController> logger) : Controller
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
    public async Task<IActionResult> Save(LinkedInPublisherSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var settings = new UserPublisherLinkedInSettings
        {
            Id = model.Id,
            IsEnabled = model.IsEnabled,
            AuthorId = model.AuthorId,
            ClientId = model.ClientId,
            HasClientSecret = model.HasClientSecret,
            HasAccessToken = model.HasAccessToken,
            CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty
        };

        var saved = await service.SaveCurrentUserAsync(settings, model.ClientSecret, model.AccessToken);
        if (saved is null)
        {
            logger.LogWarning("LinkedIn settings save failed for user '{UserOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            TempData["ErrorMessage"] = "Unable to save LinkedIn publisher settings right now.";
            return View("Edit", model);
        }

        TempData["SuccessMessage"] = "LinkedIn settings saved.";
        await setupService.InvalidateAsync();
        return RedirectToAction(nameof(Index));
    }

    private static LinkedInPublisherSettingsViewModel MapToViewModel(UserPublisherLinkedInSettings? settings) =>
        new()
        {
            Id = settings?.Id ?? 0,
            IsEnabled = settings?.IsEnabled ?? false,
            AuthorId = settings?.AuthorId,
            ClientId = settings?.ClientId,
            HasClientSecret = settings?.HasClientSecret ?? false,
            HasAccessToken = settings?.HasAccessToken ?? false,
            PlatformName = "LinkedIn",
            PlatformIcon = "bi-linkedin"
        };
}
