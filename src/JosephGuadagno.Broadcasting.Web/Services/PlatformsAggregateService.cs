using AutoMapper;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>Calls the platforms aggregate endpoint on behalf of the current user.</summary>
public class PlatformsAggregateService(
    IDownstreamApi apiClient,
    ILogger<PlatformsAggregateService> logger,
    IMapper mapper) : IPlatformsAggregateService
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

        return mapper.Map<PlatformsAggregateViewModel>(response);
    }
}

