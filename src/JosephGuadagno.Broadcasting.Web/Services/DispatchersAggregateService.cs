using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the dispatchers aggregate endpoint on behalf of the current user.</summary>
public class DispatchersAggregateService(
    IDownstreamApi apiClient) : IDispatchersAggregateService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string DispatchersBaseUrl = "/Dispatchers";

    public async Task<DispatchersAggregateViewModel?> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<DispatchersAggregateWebResponse>(ApiServiceName, options =>
        {
            options.RelativePath = DispatchersBaseUrl;
        });

        if (response is null)
        {
            return null;
        }

        return new DispatchersAggregateViewModel
        {
            Bluesky = response.Bluesky,
            Twitter = response.Twitter,
            LinkedIn = response.LinkedIn,
            Facebook = response.Facebook
        };
    }

    private sealed class DispatchersAggregateWebResponse
    {
        public UserPlatformBlueskySettings? Bluesky { get; set; }

        public UserPlatformTwitterSettings? Twitter { get; set; }

        public UserPlatformLinkedInSettings? LinkedIn { get; set; }

        public UserPlatformFacebookSettings? Facebook { get; set; }
    }
}

