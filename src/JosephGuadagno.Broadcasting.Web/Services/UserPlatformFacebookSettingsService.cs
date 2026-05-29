using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the Facebook dispatcher settings API on behalf of the current user.</summary>
public class UserPlatformFacebookSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPlatformFacebookSettingsService> logger) : IUserPlatformFacebookSettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string FacebookBaseUrl = "/Dispatchers/Facebook";

    public async Task<UserPlatformFacebookSettings?> GetCurrentUserAsync()
    {
        return await apiClient.GetOptionalForUserAsync<UserPlatformFacebookSettings>(ApiServiceName, options =>
        {
            options.RelativePath = FacebookBaseUrl;
        });
    }

    public async Task<UserPlatformFacebookSettings?> SaveCurrentUserAsync(
        UserPlatformFacebookSettings settings,
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

        var response = await apiClient.PutForUserAsync<FacebookApiRequest, UserPlatformFacebookSettings>(
            ApiServiceName, request, options =>
            {
                options.RelativePath = FacebookBaseUrl;
            });

        if (response is null)
        {
            logger.LogWarning(
                "API returned null for {Operation} with ownerOid '{OwnerOid}' and pageId '{PageId}'",
                nameof(SaveCurrentUserAsync),
                LogSanitizer.Sanitize(settings.CreatedByEntraOid),
                LogSanitizer.Sanitize(settings.PageId));
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

