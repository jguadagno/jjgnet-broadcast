using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the Bluesky publisher settings API on behalf of the current user.</summary>
public class UserPlatformBlueskySettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPlatformBlueskySettingsService> logger) : IUserPlatformBlueskySettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BlueskyBaseUrl = "/Platforms/Bluesky";

    public async Task<UserPlatformBlueskySettings?> GetCurrentUserAsync()
    {
        return await apiClient.GetOptionalForUserAsync<UserPlatformBlueskySettings>(ApiServiceName, options =>
        {
            options.RelativePath = BlueskyBaseUrl;
        });
    }

    public async Task<UserPlatformBlueskySettings?> SaveCurrentUserAsync(
        UserPlatformBlueskySettings settings,
        string? appPassword = null)
    {
        var request = new BlueskyApiRequest
        {
            IsEnabled = settings.IsEnabled,
            UserName = settings.UserName,
            AppPassword = appPassword
        };

        var response = await apiClient.PutForUserAsync<BlueskyApiRequest, UserPlatformBlueskySettings>(
            ApiServiceName, request, options =>
            {
                options.RelativePath = BlueskyBaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "API returned null for {Operation} with ownerOid '{OwnerOid}' and userName '{UserName}'",
                nameof(SaveCurrentUserAsync),
                LogSanitizer.Sanitize(settings.CreatedByEntraOid),
                LogSanitizer.Sanitize(settings.UserName));
        }

        return response;
    }

    private sealed class BlueskyApiRequest
    {
        public bool IsEnabled { get; set; }
        public string? UserName { get; set; }
        public string? AppPassword { get; set; }
    }
}

