using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
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
    private readonly IUserCollectorSpeakingEngagementService _speakingEngagementService;
    private readonly IUserApprovalManager _userApprovalManager;
    private readonly ILogger<CollectorSettingsController> _logger;

    public CollectorSettingsController(
        IUserCollectorFeedSourceService feedSourceService,
        IUserCollectorYouTubeChannelService youTubeChannelService,
        IUserCollectorSpeakingEngagementService speakingEngagementService,
        IUserApprovalManager userApprovalManager,
        ILogger<CollectorSettingsController> logger)
    {
        _feedSourceService = feedSourceService;
        _youTubeChannelService = youTubeChannelService;
        _speakingEngagementService = speakingEngagementService;
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

    private async Task<CollectorSettingsPageViewModel> BuildPageViewModelAsync(TargetCollectorSettingsContext context)
    {
        var feedSources = context.IsManagedBySiteAdmin
            ? await _feedSourceService.GetByUserAsync(context.TargetUserOid)
            : await _feedSourceService.GetCurrentUserAsync();

        var youTubeChannels = context.IsManagedBySiteAdmin
            ? await _youTubeChannelService.GetByUserAsync(context.TargetUserOid)
            : await _youTubeChannelService.GetCurrentUserAsync();

        var speakingEngagements = context.IsManagedBySiteAdmin
            ? await _speakingEngagementService.GetByUserAsync(context.TargetUserOid)
            : await _speakingEngagementService.GetCurrentUserAsync();

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
            }).ToList(),
            SpeakingEngagements = speakingEngagements.Select(se => new UserCollectorSpeakingEngagementViewModel
            {
                Id = se.Id,
                SpeakingEngagementsFile = se.SpeakingEngagementsFile,
                DisplayName = se.DisplayName,
                IsActive = se.IsActive,
                CreatedOn = se.CreatedOn,
                LastUpdatedOn = se.LastUpdatedOn,
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
