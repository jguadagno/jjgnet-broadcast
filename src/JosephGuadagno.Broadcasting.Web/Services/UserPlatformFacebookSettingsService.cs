using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the Facebook platform settings API on behalf of the current user.</summary>
public class UserPlatformFacebookSettingsService(
    IDownstreamApi apiClient,
    ILogger<UserPlatformFacebookSettingsService> logger) : IUserPlatformFacebookSettingsService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string FacebookBaseUrl = "/Platforms/Facebook";

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

}

