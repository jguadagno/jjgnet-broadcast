using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for per-user collector settings.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
public class CollectorSettingsController : Controller
{
    private readonly IUserCollectorFeedSourceService _feedSourceService;
    private readonly IUserCollectorYouTubeChannelService _youTubeChannelService;
    private readonly IUserApprovalManager _userApprovalManager;
    private readonly ILogger<CollectorSettingsController> _logger;

    public CollectorSettingsController(
        IUserCollectorFeedSourceService feedSourceService,
        IUserCollectorYouTubeChannelService youTubeChannelService,
        IUserApprovalManager userApprovalManager,
        ILogger<CollectorSettingsController> logger)
    {
        _feedSourceService = feedSourceService;
        _youTubeChannelService = youTubeChannelService;
        _userApprovalManager = userApprovalManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        var viewModel = await BuildPageViewModelAsync(resolution.Context!);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFeedSource(UserCollectorFeedSourceViewModel model, string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageViewModelAsync(resolution.Context!);
            return View("Index", invalidPage);
        }

        var feedSource = new UserCollectorFeedSource
        {
            CreatedByEntraOid = resolution.Context!.TargetUserOid,
            FeedUrl = model.FeedUrl,
            DisplayName = model.DisplayName,
            IsActive = true
        };

        var result = resolution.Context.IsManagedBySiteAdmin
            ? await _feedSourceService.AddByUserAsync(resolution.Context.TargetUserOid, feedSource)
            : await _feedSourceService.AddCurrentUserAsync(feedSource);

        if (result is null)
        {
            _logger.LogWarning(
                "Failed to add feed source for owner {OwnerOid}, URL: {FeedUrl}",
                resolution.Context.TargetUserOid,
                LogSanitizer.Sanitize(model.FeedUrl));
            TempData["ErrorMessage"] = "Unable to add feed source. Please try again.";
        }
        else
        {
            _logger.LogInformation(
                "Feed source added for owner {OwnerOid}, ID: {Id}",
                resolution.Context.TargetUserOid,
                result.Id);
            TempData["SuccessMessage"] = $"Feed source '{LogSanitizer.Sanitize(model.DisplayName)}' added successfully.";
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFeedSource(UserCollectorFeedSourceViewModel model, string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageViewModelAsync(resolution.Context!);
            return View("Index", invalidPage);
        }

        var feedSource = new UserCollectorFeedSource
        {
            Id = model.Id,
            CreatedByEntraOid = resolution.Context!.TargetUserOid,
            FeedUrl = model.FeedUrl,
            DisplayName = model.DisplayName,
            IsActive = model.IsActive
        };

        var result = resolution.Context.IsManagedBySiteAdmin
            ? await _feedSourceService.UpdateByUserAsync(resolution.Context.TargetUserOid, feedSource)
            : await _feedSourceService.UpdateCurrentUserAsync(feedSource);

        if (result is null)
        {
            _logger.LogWarning(
                "Failed to update feed source {Id} for owner {OwnerOid}",
                model.Id,
                resolution.Context.TargetUserOid);
            TempData["ErrorMessage"] = "Unable to update feed source. Please try again.";
        }
        else
        {
            _logger.LogInformation(
                "Feed source {Id} updated for owner {OwnerOid}",
                model.Id,
                resolution.Context.TargetUserOid);
            TempData["SuccessMessage"] = $"Feed source '{LogSanitizer.Sanitize(model.DisplayName)}' updated successfully.";
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeedSource(int id, string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        var success = resolution.Context!.IsManagedBySiteAdmin
            ? await _feedSourceService.DeleteByUserAsync(resolution.Context.TargetUserOid, id)
            : await _feedSourceService.DeleteCurrentUserAsync(id);

        if (success)
        {
            _logger.LogInformation(
                "Feed source {Id} deleted for owner {OwnerOid}",
                id,
                resolution.Context.TargetUserOid);
            TempData["SuccessMessage"] = "Feed source removed successfully.";
        }
        else
        {
            _logger.LogWarning(
                "Failed to delete feed source {Id} for owner {OwnerOid}",
                id,
                resolution.Context.TargetUserOid);
            TempData["ErrorMessage"] = "Unable to remove feed source. Please try again.";
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddYouTubeChannel(UserCollectorYouTubeChannelViewModel model, string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageViewModelAsync(resolution.Context!);
            return View("Index", invalidPage);
        }

        var channel = new UserCollectorYouTubeChannel
        {
            CreatedByEntraOid = resolution.Context!.TargetUserOid,
            ChannelId = model.ChannelId,
            DisplayName = model.DisplayName,
            IsActive = true
        };

        var result = resolution.Context.IsManagedBySiteAdmin
            ? await _youTubeChannelService.AddByUserAsync(resolution.Context.TargetUserOid, channel)
            : await _youTubeChannelService.AddCurrentUserAsync(channel);

        if (result is null)
        {
            _logger.LogWarning(
                "Failed to add YouTube channel for owner {OwnerOid}, ChannelId: {ChannelId}",
                resolution.Context.TargetUserOid,
                LogSanitizer.Sanitize(model.ChannelId));
            TempData["ErrorMessage"] = "Unable to add YouTube channel. Please try again.";
        }
        else
        {
            _logger.LogInformation(
                "YouTube channel added for owner {OwnerOid}, ID: {Id}",
                resolution.Context.TargetUserOid,
                result.Id);
            TempData["SuccessMessage"] = $"YouTube channel '{LogSanitizer.Sanitize(model.DisplayName)}' added successfully.";
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditYouTubeChannel(UserCollectorYouTubeChannelViewModel model, string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageViewModelAsync(resolution.Context!);
            return View("Index", invalidPage);
        }

        var channel = new UserCollectorYouTubeChannel
        {
            Id = model.Id,
            CreatedByEntraOid = resolution.Context!.TargetUserOid,
            ChannelId = model.ChannelId,
            DisplayName = model.DisplayName,
            IsActive = model.IsActive
        };

        var result = resolution.Context.IsManagedBySiteAdmin
            ? await _youTubeChannelService.UpdateByUserAsync(resolution.Context.TargetUserOid, channel)
            : await _youTubeChannelService.UpdateCurrentUserAsync(channel);

        if (result is null)
        {
            _logger.LogWarning(
                "Failed to update YouTube channel {Id} for owner {OwnerOid}",
                model.Id,
                resolution.Context.TargetUserOid);
            TempData["ErrorMessage"] = "Unable to update YouTube channel. Please try again.";
        }
        else
        {
            _logger.LogInformation(
                "YouTube channel {Id} updated for owner {OwnerOid}",
                model.Id,
                resolution.Context.TargetUserOid);
            TempData["SuccessMessage"] = $"YouTube channel '{LogSanitizer.Sanitize(model.DisplayName)}' updated successfully.";
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteYouTubeChannel(int id, string? userOid = null)
    {
        var resolution = await ResolveTargetUserAsync(userOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        var success = resolution.Context!.IsManagedBySiteAdmin
            ? await _youTubeChannelService.DeleteByUserAsync(resolution.Context.TargetUserOid, id)
            : await _youTubeChannelService.DeleteCurrentUserAsync(id);

        if (success)
        {
            _logger.LogInformation(
                "YouTube channel {Id} deleted for owner {OwnerOid}",
                id,
                resolution.Context.TargetUserOid);
            TempData["SuccessMessage"] = "YouTube channel removed successfully.";
        }
        else
        {
            _logger.LogWarning(
                "Failed to delete YouTube channel {Id} for owner {OwnerOid}",
                id,
                resolution.Context.TargetUserOid);
            TempData["ErrorMessage"] = "Unable to remove YouTube channel. Please try again.";
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    private async Task<CollectorSettingsPageViewModel> BuildPageViewModelAsync(TargetCollectorSettingsContext context)
    {
        var feedSources = context.IsManagedBySiteAdmin
            ? await _feedSourceService.GetByUserAsync(context.TargetUserOid)
            : await _feedSourceService.GetCurrentUserAsync();

        var youTubeChannels = context.IsManagedBySiteAdmin
            ? await _youTubeChannelService.GetByUserAsync(context.TargetUserOid)
            : await _youTubeChannelService.GetCurrentUserAsync();

        return new CollectorSettingsPageViewModel
        {
            TargetUserEntraOid = context.TargetUserOid,
            TargetUserDisplayName = context.TargetUserDisplayName,
            IsManagedBySiteAdmin = context.IsManagedBySiteAdmin,
            FeedSources = feedSources.Select(fs => new UserCollectorFeedSourceViewModel
            {
                Id = fs.Id,
                FeedUrl = fs.FeedUrl,
                DisplayName = fs.DisplayName,
                IsActive = fs.IsActive,
                CreatedOn = fs.CreatedOn,
                LastUpdatedOn = fs.LastUpdatedOn,
                IsManagedBySiteAdmin = context.IsManagedBySiteAdmin
            }).ToList(),
            YouTubeChannels = youTubeChannels.Select(ch => new UserCollectorYouTubeChannelViewModel
            {
                Id = ch.Id,
                ChannelId = ch.ChannelId,
                DisplayName = ch.DisplayName,
                IsActive = ch.IsActive,
                CreatedOn = ch.CreatedOn,
                LastUpdatedOn = ch.LastUpdatedOn,
                IsManagedBySiteAdmin = context.IsManagedBySiteAdmin
            }).ToList()
        };
    }

    private async Task<(TargetCollectorSettingsContext? Context, IActionResult? FailureResult)> ResolveTargetUserAsync(string? requestedUserOid)
    {
        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(currentUserOid))
        {
            _logger.LogWarning("Unable to resolve collector settings because the current user's Entra object id claim is missing.");
            TempData["ErrorMessage"] = "We couldn't determine which account to load collector settings for.";
            return (null, RedirectToAction("Index", "Home"));
        }

        if (string.IsNullOrWhiteSpace(requestedUserOid) || string.Equals(requestedUserOid, currentUserOid, StringComparison.OrdinalIgnoreCase))
        {
            var currentUser = await _userApprovalManager.GetUserAsync(currentUserOid);
            return (
                new TargetCollectorSettingsContext(
                    currentUserOid,
                    currentUser?.DisplayName ?? User.Identity?.Name,
                    false),
                null);
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            TempData["ErrorMessage"] = "Only Site Administrators can manage another user's collector settings.";
            return (null, RedirectToAction(nameof(Index)));
        }

        var targetUser = await _userApprovalManager.GetUserAsync(requestedUserOid);
        if (targetUser is null)
        {
            return (null, NotFound());
        }

        return (
            new TargetCollectorSettingsContext(
                targetUser.EntraObjectId,
                targetUser.DisplayName ?? targetUser.Email,
                true),
            null);
    }

    private static object? BuildRouteValues(TargetCollectorSettingsContext context)
    {
        return context.IsManagedBySiteAdmin ? new { userOid = context.TargetUserOid } : null;
    }

    private sealed record TargetCollectorSettingsContext(
        string TargetUserOid,
        string? TargetUserDisplayName,
        bool IsManagedBySiteAdmin);
}
