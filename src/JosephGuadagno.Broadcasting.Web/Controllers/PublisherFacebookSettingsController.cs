using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>Manages Facebook publisher settings for the current user.</summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("Publishers/Facebook/Settings")]
public class PublisherFacebookSettingsController(
    IUserPublisherFacebookSettingsService service,
    ILogger<PublisherFacebookSettingsController> logger) : Controller
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
    public async Task<IActionResult> Save(FacebookPublisherSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var settings = new UserPublisherFacebookSettings
        {
            Id = model.Id,
            IsEnabled = model.IsEnabled,
            PageId = model.PageId,
            AppId = model.AppId,
            HasPageAccessToken = model.HasPageAccessToken,
            HasAppSecret = model.HasAppSecret,
            HasClientToken = model.HasClientToken,
            HasShortLivedAccessToken = model.HasShortLivedAccessToken,
            HasLongLivedAccessToken = model.HasLongLivedAccessToken,
            CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty
        };

        var saved = await service.SaveCurrentUserAsync(
            settings,
            model.PageAccessToken,
            model.AppSecret,
            model.ClientToken,
            model.ShortLivedAccessToken,
            model.LongLivedAccessToken);

        if (saved is null)
        {
            logger.LogWarning("Facebook settings save failed for user '{UserOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            TempData["ErrorMessage"] = "Unable to save Facebook publisher settings right now.";
            return View("Edit", model);
        }

        TempData["SuccessMessage"] = "Facebook settings saved.";
        return RedirectToAction(nameof(Index));
    }

    private static FacebookPublisherSettingsViewModel MapToViewModel(UserPublisherFacebookSettings? settings) =>
        new()
        {
            Id = settings?.Id ?? 0,
            IsEnabled = settings?.IsEnabled ?? false,
            PageId = settings?.PageId,
            AppId = settings?.AppId,
            HasPageAccessToken = settings?.HasPageAccessToken ?? false,
            HasAppSecret = settings?.HasAppSecret ?? false,
            HasClientToken = settings?.HasClientToken ?? false,
            HasShortLivedAccessToken = settings?.HasShortLivedAccessToken ?? false,
            HasLongLivedAccessToken = settings?.HasLongLivedAccessToken ?? false,
            PlatformName = "Facebook",
            PlatformIcon = "bi-facebook"
        };
}
