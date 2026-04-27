using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Controller for per-user publisher settings.
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
public class PublisherSettingsController : Controller
{
    private readonly IUserPublisherSettingService _userPublisherSettingService;
    private readonly ISocialMediaPlatformService _socialMediaPlatformService;
    private readonly IUserApprovalManager _userApprovalManager;
    private readonly ILogger<PublisherSettingsController> _logger;

    public PublisherSettingsController(
        IUserPublisherSettingService userPublisherSettingService,
        ISocialMediaPlatformService socialMediaPlatformService,
        IUserApprovalManager userApprovalManager,
        ILogger<PublisherSettingsController> logger)
    {
        _userPublisherSettingService = userPublisherSettingService;
        _socialMediaPlatformService = socialMediaPlatformService;
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
    public Task<IActionResult> SaveBluesky(BlueskyPublisherSettingsViewModel model)
    {
        return SavePlatformAsync(model, vm => new UserPublisherSetting
        {
            CreatedByEntraOid = vm.CreatedByEntraOid,
            SocialMediaPlatformId = vm.SocialMediaPlatformId,
            SocialMediaPlatformName = vm.PlatformName,
            IsEnabled = vm.IsEnabled,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(BlueskyPublisherSettings.BlueskyUserName)] = vm.UserName,
                [nameof(BlueskyPublisherSettings.BlueskyPassword)] = vm.AppPassword
            }
        }, "Bluesky settings saved.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveTwitter(TwitterPublisherSettingsViewModel model)
    {
        return SavePlatformAsync(model, vm => new UserPublisherSetting
        {
            CreatedByEntraOid = vm.CreatedByEntraOid,
            SocialMediaPlatformId = vm.SocialMediaPlatformId,
            SocialMediaPlatformName = vm.PlatformName,
            IsEnabled = vm.IsEnabled,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(TwitterPublisherSettings.ConsumerKey)] = vm.ConsumerKey,
                [nameof(TwitterPublisherSettings.ConsumerSecret)] = vm.ConsumerSecret,
                [nameof(TwitterPublisherSettings.OAuthToken)] = vm.AccessToken,
                [nameof(TwitterPublisherSettings.OAuthTokenSecret)] = vm.AccessTokenSecret
            }
        }, "Twitter/X settings saved.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveFacebook(FacebookPublisherSettingsViewModel model)
    {
        return SavePlatformAsync(model, vm => new UserPublisherSetting
        {
            CreatedByEntraOid = vm.CreatedByEntraOid,
            SocialMediaPlatformId = vm.SocialMediaPlatformId,
            SocialMediaPlatformName = vm.PlatformName,
            IsEnabled = vm.IsEnabled,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(FacebookPublisherSettings.PageId)] = vm.PageId,
                [nameof(FacebookPublisherSettings.AppId)] = vm.AppId,
                [nameof(FacebookPublisherSettings.PageAccessToken)] = vm.PageAccessToken,
                [nameof(FacebookPublisherSettings.AppSecret)] = vm.AppSecret,
                [nameof(FacebookPublisherSettings.ClientToken)] = vm.ClientToken,
                [nameof(FacebookPublisherSettings.ShortLivedAccessToken)] = vm.ShortLivedAccessToken,
                [nameof(FacebookPublisherSettings.LongLivedAccessToken)] = vm.LongLivedAccessToken,
                [nameof(FacebookPublisherSettings.GraphApiVersion)] = vm.GraphApiVersion,
                [nameof(FacebookPublisherSettings.GraphApiRootUrl)] = vm.GraphApiRootUrl
            }
        }, "Facebook settings saved.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveLinkedIn(LinkedInPublisherSettingsViewModel model)
    {
        return SavePlatformAsync(model, vm => new UserPublisherSetting
        {
            CreatedByEntraOid = vm.CreatedByEntraOid,
            SocialMediaPlatformId = vm.SocialMediaPlatformId,
            SocialMediaPlatformName = vm.PlatformName,
            IsEnabled = vm.IsEnabled,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(LinkedInPublisherSettings.AuthorId)] = vm.AuthorId,
                [nameof(LinkedInPublisherSettings.ClientId)] = vm.ClientId,
                [nameof(LinkedInPublisherSettings.ClientSecret)] = vm.ClientSecret,
                [nameof(LinkedInPublisherSettings.AccessToken)] = vm.AccessToken,
                [nameof(LinkedInPublisherSettings.AccessTokenUrl)] = vm.AccessTokenUrl
            }
        }, "LinkedIn settings saved.");
    }

    private async Task<IActionResult> SavePlatformAsync<TViewModel>(
        TViewModel model,
        Func<TViewModel, UserPublisherSetting> mapRequest,
        string successMessage)
        where TViewModel : PublisherPlatformSettingsViewModel
    {
        var resolution = await ResolveTargetUserAsync(model.CreatedByEntraOid);
        if (resolution.FailureResult is not null)
        {
            return resolution.FailureResult;
        }

        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageViewModelAsync(resolution.Context!, model);
            return View("Index", invalidPage);
        }

        var request = mapRequest(model);
        var savedSetting = resolution.Context!.IsManagedBySiteAdmin
            ? await _userPublisherSettingService.SaveByUserAsync(resolution.Context.TargetUserOid, request)
            : await _userPublisherSettingService.SaveCurrentUserAsync(request);

        if (savedSetting is null)
        {
            TempData["ErrorMessage"] = $"Unable to save {model.PlatformName} publisher settings right now.";
            return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
        }

        TempData["SuccessMessage"] = successMessage;
        return RedirectToAction(nameof(Index), BuildRouteValues(resolution.Context));
    }

    private async Task<UserPublisherSettingsPageViewModel> BuildPageViewModelAsync(
        TargetPublisherSettingsContext context,
        PublisherPlatformSettingsViewModel? invalidModel = null)
    {
        var availablePlatforms = await _socialMediaPlatformService.GetAllAsync(pageSize: Pagination.MaxPageSize, includeInactive: true);
        var currentSettings = context.IsManagedBySiteAdmin
            ? await _userPublisherSettingService.GetByUserAsync(context.TargetUserOid)
            : await _userPublisherSettingService.GetCurrentUserAsync();
        var settingsByPlatformId = currentSettings.ToDictionary(setting => setting.SocialMediaPlatformId);

        var platforms = availablePlatforms.Items
            .Where(platform => platform.IsActive || settingsByPlatformId.ContainsKey(platform.Id))
            .OrderBy(platform => platform.Name)
            .Select(platform => CreateViewModel(platform, context.TargetUserOid, settingsByPlatformId.GetValueOrDefault(platform.Id), context.IsManagedBySiteAdmin))
            .ToList();

        if (invalidModel is not null)
        {
            invalidModel.IsManagedBySiteAdmin = context.IsManagedBySiteAdmin;
            var existingIndex = platforms.FindIndex(platform => platform.SocialMediaPlatformId == invalidModel.SocialMediaPlatformId);
            if (existingIndex >= 0)
            {
                platforms[existingIndex] = invalidModel;
            }
        }

        return new UserPublisherSettingsPageViewModel
        {
            TargetUserEntraOid = context.TargetUserOid,
            TargetUserDisplayName = context.TargetUserDisplayName,
            IsManagedBySiteAdmin = context.IsManagedBySiteAdmin,
            Platforms = platforms
        };
    }

    private static PublisherPlatformSettingsViewModel CreateViewModel(
        SocialMediaPlatform platform,
        string targetUserOid,
        UserPublisherSetting? setting,
        bool isManagedBySiteAdmin)
    {
        var normalizedName = NormalizePlatformName(platform.Name);

        return normalizedName switch
        {
            "bluesky" => new BlueskyPublisherSettingsViewModel
            {
                Id = setting?.Id ?? 0,
                CreatedByEntraOid = targetUserOid,
                SocialMediaPlatformId = platform.Id,
                PlatformName = platform.Name,
                PlatformIcon = platform.Icon,
                IsEnabled = setting?.IsEnabled ?? false,
                IsManagedBySiteAdmin = isManagedBySiteAdmin,
                CredentialSetupDocumentationUrl = platform.CredentialSetupDocumentationUrl,
                UserName = GetSettingValue(setting, nameof(BlueskyPublisherSettings.BlueskyUserName)),
                HasAppPassword = HasStoredValue(setting, nameof(BlueskyPublisherSettings.BlueskyPassword))
            },
            "twitter" => new TwitterPublisherSettingsViewModel
            {
                Id = setting?.Id ?? 0,
                CreatedByEntraOid = targetUserOid,
                SocialMediaPlatformId = platform.Id,
                PlatformName = platform.Name,
                PlatformIcon = platform.Icon,
                IsEnabled = setting?.IsEnabled ?? false,
                IsManagedBySiteAdmin = isManagedBySiteAdmin,
                CredentialSetupDocumentationUrl = platform.CredentialSetupDocumentationUrl,
                HasConsumerKey = HasStoredValue(setting, nameof(TwitterPublisherSettings.ConsumerKey)),
                HasConsumerSecret = HasStoredValue(setting, nameof(TwitterPublisherSettings.ConsumerSecret)),
                HasAccessToken = HasStoredValue(setting, nameof(TwitterPublisherSettings.OAuthToken)),
                HasAccessTokenSecret = HasStoredValue(setting, nameof(TwitterPublisherSettings.OAuthTokenSecret))
            },
            "facebook" => new FacebookPublisherSettingsViewModel
            {
                Id = setting?.Id ?? 0,
                CreatedByEntraOid = targetUserOid,
                SocialMediaPlatformId = platform.Id,
                PlatformName = platform.Name,
                PlatformIcon = platform.Icon,
                IsEnabled = setting?.IsEnabled ?? false,
                IsManagedBySiteAdmin = isManagedBySiteAdmin,
                CredentialSetupDocumentationUrl = platform.CredentialSetupDocumentationUrl,
                PageId = GetSettingValue(setting, nameof(FacebookPublisherSettings.PageId)),
                AppId = GetSettingValue(setting, nameof(FacebookPublisherSettings.AppId)),
                GraphApiVersion = GetSettingValue(setting, nameof(FacebookPublisherSettings.GraphApiVersion)),
                GraphApiRootUrl = GetSettingValue(setting, nameof(FacebookPublisherSettings.GraphApiRootUrl)),
                HasPageAccessToken = HasStoredValue(setting, nameof(FacebookPublisherSettings.PageAccessToken)),
                HasAppSecret = HasStoredValue(setting, nameof(FacebookPublisherSettings.AppSecret)),
                HasClientToken = HasStoredValue(setting, nameof(FacebookPublisherSettings.ClientToken)),
                HasShortLivedAccessToken = HasStoredValue(setting, nameof(FacebookPublisherSettings.ShortLivedAccessToken)),
                HasLongLivedAccessToken = HasStoredValue(setting, nameof(FacebookPublisherSettings.LongLivedAccessToken))
            },
            "linkedin" => new LinkedInPublisherSettingsViewModel
            {
                Id = setting?.Id ?? 0,
                CreatedByEntraOid = targetUserOid,
                SocialMediaPlatformId = platform.Id,
                PlatformName = platform.Name,
                PlatformIcon = platform.Icon,
                IsEnabled = setting?.IsEnabled ?? false,
                IsManagedBySiteAdmin = isManagedBySiteAdmin,
                CredentialSetupDocumentationUrl = platform.CredentialSetupDocumentationUrl,
                AuthorId = GetSettingValue(setting, nameof(LinkedInPublisherSettings.AuthorId)),
                ClientId = GetSettingValue(setting, nameof(LinkedInPublisherSettings.ClientId)),
                AccessTokenUrl = GetSettingValue(setting, nameof(LinkedInPublisherSettings.AccessTokenUrl)),
                HasClientSecret = HasStoredValue(setting, nameof(LinkedInPublisherSettings.ClientSecret)),
                HasAccessToken = HasStoredValue(setting, nameof(LinkedInPublisherSettings.AccessToken))
            },
            _ => new UnsupportedPublisherSettingsViewModel
            {
                Id = setting?.Id ?? 0,
                CreatedByEntraOid = targetUserOid,
                SocialMediaPlatformId = platform.Id,
                PlatformName = platform.Name,
                PlatformIcon = platform.Icon,
                IsEnabled = setting?.IsEnabled ?? false,
                IsManagedBySiteAdmin = isManagedBySiteAdmin,
                CredentialSetupDocumentationUrl = platform.CredentialSetupDocumentationUrl
            }
        };
    }

    private async Task<(TargetPublisherSettingsContext? Context, IActionResult? FailureResult)> ResolveTargetUserAsync(string? requestedUserOid)
    {
        var currentUserOid = User.FindFirstValue(ApplicationClaimTypes.EntraObjectId);
        if (string.IsNullOrWhiteSpace(currentUserOid))
        {
            _logger.LogWarning("Unable to resolve publisher settings because the current user's Entra object id claim is missing.");
            TempData["ErrorMessage"] = "We couldn't determine which account to load publisher settings for.";
            return (null, RedirectToAction("Index", "Home"));
        }

        if (string.IsNullOrWhiteSpace(requestedUserOid) || string.Equals(requestedUserOid, currentUserOid, StringComparison.OrdinalIgnoreCase))
        {
            var currentUser = await _userApprovalManager.GetUserAsync(currentUserOid);
            return (
                new TargetPublisherSettingsContext(
                    currentUserOid,
                    currentUser?.DisplayName ?? User.Identity?.Name,
                    false),
                null);
        }

        if (!User.IsInRole(RoleNames.SiteAdministrator))
        {
            TempData["ErrorMessage"] = "Only Site Administrators can manage another user's publisher settings.";
            return (null, RedirectToAction(nameof(Index)));
        }

        var targetUser = await _userApprovalManager.GetUserAsync(requestedUserOid);
        if (targetUser is null)
        {
            return (null, NotFound());
        }

        return (
            new TargetPublisherSettingsContext(
                targetUser.EntraObjectId,
                targetUser.DisplayName ?? targetUser.Email,
                true),
            null);
    }

    private static object? BuildRouteValues(TargetPublisherSettingsContext context)
    {
        return context.IsManagedBySiteAdmin ? new { userOid = context.TargetUserOid } : null;
    }

    private static string? GetSettingValue(UserPublisherSetting? setting, string key)
    {
        if (setting?.Settings is null)
        {
            return null;
        }

        return setting.Settings.TryGetValue(key, out var value)
            ? value
            : setting.Settings.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static bool HasStoredValue(UserPublisherSetting? setting, string key)
    {
        if (setting is null)
        {
            return false;
        }

        var value = GetSettingValue(setting, key);
        return !string.IsNullOrWhiteSpace(value)
               && setting.WriteOnlyFields.Any(writeOnlyField => writeOnlyField.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePlatformName(string platformName)
    {
        return platformName
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant() switch
        {
            var value when value.StartsWith("twitter", StringComparison.Ordinal) => "twitter",
            var value => value
        };
    }

    private sealed record TargetPublisherSettingsContext(
        string TargetUserOid,
        string? TargetUserDisplayName,
        bool IsManagedBySiteAdmin);
}
