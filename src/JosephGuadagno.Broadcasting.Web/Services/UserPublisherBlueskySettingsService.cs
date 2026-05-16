using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the Bluesky publisher settings API on behalf of the current user.</summary>
public class UserPublisherBlueskySettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPublisherBlueskySettingsService> logger) : IUserPublisherBlueskySettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BlueskyBaseUrl = "/Publishers/Bluesky";

    public async Task<UserPublisherBlueskySettings?> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<UserPublisherBlueskySettings>(ApiServiceName, options =>
        {
            options.RelativePath = BlueskyBaseUrl;
        });
        return response;
    }

    public async Task<UserPublisherBlueskySettings?> SaveCurrentUserAsync(
        UserPublisherBlueskySettings settings,
        string? appPassword = null)
    {
        var request = new BlueskyApiRequest
        {
            IsEnabled = settings.IsEnabled,
            UserName = settings.UserName,
            AppPassword = appPassword
        };

        var response = await apiClient.PutForUserAsync<BlueskyApiRequest, UserPublisherBlueskySettings>(
            ApiServiceName, request, options =>
            {
                options.RelativePath = BlueskyBaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Bluesky settings save returned no content for owner '{OwnerOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
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
