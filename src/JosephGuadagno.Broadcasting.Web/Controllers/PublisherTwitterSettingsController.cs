using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>Manages Twitter/X publisher settings for the current user.</summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
[Route("Publishers/Twitter/Settings")]
public class PublisherTwitterSettingsController(
    IUserPublisherTwitterSettingsService service,
    ILogger<PublisherTwitterSettingsController> logger) : Controller
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
    public async Task<IActionResult> Save(TwitterPublisherSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var settings = new UserPublisherTwitterSettings
        {
            Id = model.Id,
            IsEnabled = model.IsEnabled,
            HasConsumerKey = model.HasConsumerKey,
            HasConsumerSecret = model.HasConsumerSecret,
            HasAccessToken = model.HasAccessToken,
            HasAccessTokenSecret = model.HasAccessTokenSecret,
            CreatedByEntraOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId) ?? string.Empty
        };

        var saved = await service.SaveCurrentUserAsync(
            settings,
            model.ConsumerKey,
            model.ConsumerSecret,
            model.AccessToken,
            model.AccessTokenSecret);

        if (saved is null)
        {
            logger.LogWarning("Twitter settings save failed for user '{UserOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            TempData["ErrorMessage"] = "Unable to save Twitter/X publisher settings right now.";
            return View("Edit", model);
        }

        TempData["SuccessMessage"] = "Twitter/X settings saved.";
        return RedirectToAction(nameof(Index));
    }

    private static TwitterPublisherSettingsViewModel MapToViewModel(UserPublisherTwitterSettings? settings) =>
        new()
        {
            Id = settings?.Id ?? 0,
            IsEnabled = settings?.IsEnabled ?? false,
            HasConsumerKey = settings?.HasConsumerKey ?? false,
            HasConsumerSecret = settings?.HasConsumerSecret ?? false,
            HasAccessToken = settings?.HasAccessToken ?? false,
            HasAccessTokenSecret = settings?.HasAccessTokenSecret ?? false,
            PlatformName = "Twitter/X",
            PlatformIcon = "bi-twitter-x"
        };
}
