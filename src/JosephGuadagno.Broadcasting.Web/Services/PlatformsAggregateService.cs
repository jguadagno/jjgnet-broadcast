using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the platforms aggregate endpoint on behalf of the current user.</summary>
public class PlatformsAggregateService(
    IDownstreamApi apiClient,
    ILogger<PlatformsAggregateService> logger) : IPlatformsAggregateService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string PlatformsBaseUrl = "/Platforms";

    public async Task<PlatformsAggregateViewModel?> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<PlatformsAggregateWebResponse>(ApiServiceName, options =>
        {
            options.RelativePath = PlatformsBaseUrl;
        });

        if (response is null)
        {
            logger.LogWarning("GetCurrentUserAsync downstream returned null for platforms aggregate");
            return null;
        }

        return new PlatformsAggregateViewModel
        {
            Bluesky = response.Bluesky,
            Twitter = response.Twitter,
            LinkedIn = response.LinkedIn,
            Facebook = response.Facebook
        };
    }

    private sealed class PlatformsAggregateWebResponse
    {
        public UserPlatformBlueskySettings? Bluesky { get; set; }

        public UserPlatformTwitterSettings? Twitter { get; set; }

        public UserPlatformLinkedInSettings? LinkedIn { get; set; }

        public UserPlatformFacebookSettings? Facebook { get; set; }
    }
}

