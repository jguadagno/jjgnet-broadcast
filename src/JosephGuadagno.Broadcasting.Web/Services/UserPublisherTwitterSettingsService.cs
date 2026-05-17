using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the Twitter publisher settings API on behalf of the current user.</summary>
public class UserPublisherTwitterSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPublisherTwitterSettingsService> logger) : IUserPublisherTwitterSettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string TwitterBaseUrl = "/Publishers/Twitter";

    public async Task<UserPublisherTwitterSettings?> GetCurrentUserAsync()
    {
        return await apiClient.GetOptionalForUserAsync<UserPublisherTwitterSettings>(ApiServiceName, options =>
        {
            options.RelativePath = TwitterBaseUrl;
        });
    }

    public async Task<UserPublisherTwitterSettings?> SaveCurrentUserAsync(
        UserPublisherTwitterSettings settings,
        string? consumerKey = null,
        string? consumerSecret = null,
        string? accessToken = null,
        string? accessTokenSecret = null)
    {
        var request = new TwitterApiRequest
        {
            IsEnabled = settings.IsEnabled,
            ConsumerKey = consumerKey,
            ConsumerSecret = consumerSecret,
            AccessToken = accessToken,
            AccessTokenSecret = accessTokenSecret
        };

        var response = await apiClient.PutForUserAsync<TwitterApiRequest, UserPublisherTwitterSettings>(
            ApiServiceName, request, options =>
            {
                options.RelativePath = TwitterBaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Twitter settings save returned no content for owner '{OwnerOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
        }

        return response;
    }

    private sealed class TwitterApiRequest
    {
        public bool IsEnabled { get; set; }
        public string? ConsumerKey { get; set; }
        public string? ConsumerSecret { get; set; }
        public string? AccessToken { get; set; }
        public string? AccessTokenSecret { get; set; }
    }
}
