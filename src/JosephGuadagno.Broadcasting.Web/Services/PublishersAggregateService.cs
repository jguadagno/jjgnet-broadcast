using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the publishers aggregate endpoint on behalf of the current user.</summary>
public class PublishersAggregateService(
    IDownstreamApi apiClient) : IPublishersAggregateService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string PublishersBaseUrl = "/Publishers";

    public async Task<PublishersAggregateViewModel?> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<PublishersAggregateWebResponse>(ApiServiceName, options =>
        {
            options.RelativePath = PublishersBaseUrl;
        });

        if (response is null) return null;

        return new PublishersAggregateViewModel
        {
            Bluesky = response.Bluesky,
            Twitter = response.Twitter,
            LinkedIn = response.LinkedIn,
            Facebook = response.Facebook
        };
    }

    private sealed class PublishersAggregateWebResponse
    {
        public UserPublisherBlueskySettings? Bluesky { get; set; }
        public UserPublisherTwitterSettings? Twitter { get; set; }
        public UserPublisherLinkedInSettings? LinkedIn { get; set; }
        public UserPublisherFacebookSettings? Facebook { get; set; }
    }
}
