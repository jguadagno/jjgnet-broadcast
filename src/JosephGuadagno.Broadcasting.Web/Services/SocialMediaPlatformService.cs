using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;

using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the Social Media Platforms API
/// </summary>
public class SocialMediaPlatformService(IDownstreamApi apiClient, ILogger<SocialMediaPlatformService> logger) : ISocialMediaPlatformService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string PlatformBaseUrl = "/socialmediaplatforms";

    /// <summary>
    /// Gets a paged list of social media platforms
    /// </summary>
    public async Task<PagedResult<SocialMediaPlatform>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null, bool includeInactive = false)
    {
        var pagedResponse = await apiClient.GetForUserAsync<PagedResponse<SocialMediaPlatform>>(ApiServiceName, options =>
        {
            var queryParams = $"includeInactive={includeInactive}&page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDescending={sortDescending}";
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams += $"&filter={Uri.EscapeDataString(filter)}";
            }
            options.RelativePath = $"{PlatformBaseUrl}?{queryParams}";
        });

        if (pagedResponse is null) return new PagedResult<SocialMediaPlatform>();
        return new PagedResult<SocialMediaPlatform> { Items = pagedResponse.Items.ToList(), TotalCount = pagedResponse.TotalCount };
    }

    /// <summary>
    /// Gets a social media platform by its ID
    /// </summary>
    public async Task<SocialMediaPlatform?> GetByIdAsync(int id)
    {
        return await apiClient.GetForUserAsync<SocialMediaPlatform>(ApiServiceName, options =>
        {
            options.RelativePath = $"{PlatformBaseUrl}/{id}";
        });
    }

    /// <summary>
    /// Adds a new social media platform
    /// </summary>
    public async Task<SocialMediaPlatform?> AddAsync(SocialMediaPlatform platform)
    {
        return await apiClient.PostForUserAsync<SocialMediaPlatform, SocialMediaPlatform>(ApiServiceName, platform, options =>
        {
            options.RelativePath = PlatformBaseUrl;
        });
    }

    /// <summary>
    /// Updates an existing social media platform
    /// </summary>
    public async Task<SocialMediaPlatform?> UpdateAsync(SocialMediaPlatform platform)
    {
        return await apiClient.PutForUserAsync<SocialMediaPlatform, SocialMediaPlatform>(ApiServiceName, platform, options =>
        {
            options.RelativePath = $"{PlatformBaseUrl}/{platform.Id}";
        });
    }

    /// <summary>
    /// Toggles the IsActive status of a social media platform (soft delete / reactivate)
    /// </summary>
    public async Task<bool> ToggleActiveAsync(int id)
    {
        var platform = await GetByIdAsync(id);
        if (platform is null) return false;

        platform.IsActive = !platform.IsActive;
        var updated = await UpdateAsync(platform);
        return updated is not null;
    }

    /// <summary>
    /// Deletes a social media platform via the API
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var response = await apiClient.CallApiForUserAsync<HttpResponseMessage>(ApiServiceName, options =>
        {
            options.RelativePath = $"{PlatformBaseUrl}/{id}";
            options.HttpMethod = HttpMethod.Delete.Method;
        });

        if (response is { StatusCode: HttpStatusCode.NoContent or HttpStatusCode.NotFound })
            return true;

        logger.LogWarning("Unexpected status {StatusCode} deleting platform {PlatformId}",
            response?.StatusCode, id);
        return false;
    }
}
