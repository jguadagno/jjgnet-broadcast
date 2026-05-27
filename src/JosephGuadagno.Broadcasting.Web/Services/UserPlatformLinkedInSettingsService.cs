using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the LinkedIn publisher settings API on behalf of the current user.</summary>
public class UserPlatformLinkedInSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPlatformLinkedInSettingsService> logger) : IUserPlatformLinkedInSettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string LinkedInBaseUrl = "/Publishers/LinkedIn";

    public async Task<UserPlatformLinkedInSettings?> GetCurrentUserAsync()
    {
        return await apiClient.GetOptionalForUserAsync<UserPlatformLinkedInSettings>(ApiServiceName, options =>
        {
            options.RelativePath = LinkedInBaseUrl;
        });
    }

    public async Task<UserPlatformLinkedInSettings?> SaveCurrentUserAsync(
        UserPlatformLinkedInSettings settings,
        string? clientSecret = null,
        string? accessToken = null)
    {
        var request = new LinkedInApiRequest
        {
            IsEnabled = settings.IsEnabled,
            AuthorId = settings.AuthorId,
            ClientId = settings.ClientId,
            ClientSecret = clientSecret,
            AccessToken = accessToken
        };

        var response = await apiClient.PutForUserAsync<LinkedInApiRequest, UserPlatformLinkedInSettings>(
            ApiServiceName, request, options =>
            {
                options.RelativePath = LinkedInBaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "LinkedIn settings save returned no content for owner '{OwnerOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
        }

        return response;
    }

    private sealed class LinkedInApiRequest
    {
        public bool IsEnabled { get; set; }
        public string? AuthorId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? AccessToken { get; set; }
    }
}

