using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the Facebook publisher settings API on behalf of the current user.</summary>
public class UserPublisherFacebookSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPublisherFacebookSettingsService> logger) : IUserPublisherFacebookSettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string FacebookBaseUrl = "/Publishers/Facebook";

    public async Task<UserPublisherFacebookSettings?> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<UserPublisherFacebookSettings>(ApiServiceName, options =>
        {
            options.RelativePath = FacebookBaseUrl;
        });
        return response;
    }

    public async Task<UserPublisherFacebookSettings?> SaveCurrentUserAsync(
        UserPublisherFacebookSettings settings,
        string? pageAccessToken = null,
        string? appSecret = null,
        string? clientToken = null,
        string? shortLivedAccessToken = null,
        string? longLivedAccessToken = null)
    {
        var request = new FacebookApiRequest
        {
            IsEnabled = settings.IsEnabled,
            PageId = settings.PageId,
            AppId = settings.AppId,
            PageAccessToken = pageAccessToken,
            AppSecret = appSecret,
            ClientToken = clientToken,
            ShortLivedAccessToken = shortLivedAccessToken,
            LongLivedAccessToken = longLivedAccessToken
        };

        var response = await apiClient.PutForUserAsync<FacebookApiRequest, UserPublisherFacebookSettings>(
            ApiServiceName, request, options =>
            {
                options.RelativePath = FacebookBaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Facebook settings save returned no content for owner '{OwnerOid}'",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
        }

        return response;
    }

    private sealed class FacebookApiRequest
    {
        public bool IsEnabled { get; set; }
        public string? PageId { get; set; }
        public string? AppId { get; set; }
        public string? PageAccessToken { get; set; }
        public string? AppSecret { get; set; }
        public string? ClientToken { get; set; }
        public string? ShortLivedAccessToken { get; set; }
        public string? LongLivedAccessToken { get; set; }
    }
}
