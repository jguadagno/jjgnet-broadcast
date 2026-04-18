using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the user publisher settings API.
/// </summary>
public class UserPublisherSettingService(
    IDownstreamApi apiClient,
    ILogger<UserPublisherSettingService> logger) : IUserPublisherSettingService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string CurrentUserBaseUrl = "/users/me/publishers";

    public async Task<List<UserPublisherSetting>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserPublisherSetting>>(ApiServiceName, options =>
        {
            options.RelativePath = CurrentUserBaseUrl;
        });

        return response ?? [];
    }

    public async Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<List<UserPublisherSetting>>(ApiServiceName, options =>
        {
            options.RelativePath = $"/users/{Uri.EscapeDataString(ownerOid)}/publishers";
        });

        return response ?? [];
    }

    public Task<UserPublisherSetting?> SaveCurrentUserAsync(UserPublisherSetting setting)
    {
        return SaveAsync($"{CurrentUserBaseUrl}/{setting.SocialMediaPlatformId}", setting);
    }

    public Task<UserPublisherSetting?> SaveByUserAsync(string ownerOid, UserPublisherSetting setting)
    {
        return SaveAsync($"/users/{Uri.EscapeDataString(ownerOid)}/publishers/{setting.SocialMediaPlatformId}", setting);
    }

    private async Task<UserPublisherSetting?> SaveAsync(string relativePath, UserPublisherSetting setting)
    {
        var savedSetting = await apiClient.PutForUserAsync<UserPublisherSettingRequest, UserPublisherSetting>(
            ApiServiceName,
            new UserPublisherSettingRequest
            {
                IsEnabled = setting.IsEnabled,
                Settings = setting.Settings
            },
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (savedSetting is null)
        {
            logger.LogWarning(
                "Publisher settings save returned no content for owner {OwnerOid} and platform {PlatformId}",
                setting.CreatedByEntraOid,
                setting.SocialMediaPlatformId);
        }

        return savedSetting;
    }

    private sealed class UserPublisherSettingRequest
    {
        public bool IsEnabled { get; set; }
        public Dictionary<string, string?> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
