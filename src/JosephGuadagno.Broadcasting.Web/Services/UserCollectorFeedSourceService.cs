using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the user collector feed source API.
/// </summary>
public class UserCollectorFeedSourceService(
    IDownstreamApi apiClient,
    ILogger<UserCollectorFeedSourceService> logger) : IUserCollectorFeedSourceService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string FeedSourceBaseUrl = "/UserCollectorFeedSources";

    public async Task<List<UserCollectorFeedSource>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserCollectorFeedSource>>(ApiServiceName, options =>
        {
            options.RelativePath = FeedSourceBaseUrl;
        });

        return response ?? [];
    }

    public async Task<List<UserCollectorFeedSource>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<List<UserCollectorFeedSource>>(ApiServiceName, options =>
        {
            options.RelativePath = BuildRelativePath(ownerOid);
        });

        return response ?? [];
    }

    public async Task<UserCollectorFeedSource?> GetByIdAsync(int id)
    {
        var response = await apiClient.GetForUserAsync<UserCollectorFeedSource>(ApiServiceName, options =>
        {
            options.RelativePath = $"{FeedSourceBaseUrl}/{id}";
        });

        return response;
    }

    public async Task<UserCollectorFeedSource?> AddCurrentUserAsync(UserCollectorFeedSource feedSource)
    {
        return await AddAsync(FeedSourceBaseUrl, feedSource);
    }

    public async Task<UserCollectorFeedSource?> AddByUserAsync(string ownerOid, UserCollectorFeedSource feedSource)
    {
        return await AddAsync(BuildRelativePath(ownerOid), feedSource);
    }

    public async Task<UserCollectorFeedSource?> UpdateCurrentUserAsync(UserCollectorFeedSource feedSource)
    {
        return await UpdateAsync($"{FeedSourceBaseUrl}/{feedSource.Id}", feedSource);
    }

    public async Task<UserCollectorFeedSource?> UpdateByUserAsync(string ownerOid, UserCollectorFeedSource feedSource)
    {
        return await UpdateAsync($"{BuildRelativePath(ownerOid)}/{feedSource.Id}", feedSource);
    }

    public async Task<bool> DeleteCurrentUserAsync(int id)
    {
        return await DeleteAsync($"{FeedSourceBaseUrl}/{id}");
    }

    public async Task<bool> DeleteByUserAsync(string ownerOid, int id)
    {
        return await DeleteAsync($"{BuildRelativePath(ownerOid)}/{id}");
    }

    private async Task<UserCollectorFeedSource?> AddAsync(string relativePath, UserCollectorFeedSource feedSource)
    {
        var response = await apiClient.PostForUserAsync<UserCollectorFeedSource, UserCollectorFeedSource>(
            ApiServiceName,
            feedSource,
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Feed source add returned no content for owner {OwnerOid}",
                feedSource.CreatedByEntraOid);
        }

        return response;
    }

    private async Task<UserCollectorFeedSource?> UpdateAsync(string relativePath, UserCollectorFeedSource feedSource)
    {
        var response = await apiClient.PutForUserAsync<UserCollectorFeedSource, UserCollectorFeedSource>(
            ApiServiceName,
            feedSource,
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Feed source update returned no content for id {Id}",
                feedSource.Id);
        }

        return response;
    }

    private async Task<bool> DeleteAsync(string relativePath)
    {
        try
        {
            await apiClient.CallApiForUserAsync(
                ApiServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Delete.Method;
                    options.RelativePath = relativePath;
                });

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete feed source at {RelativePath}", relativePath);
            return false;
        }
    }

    private static string BuildRelativePath(string? ownerOid = null)
    {
        return string.IsNullOrWhiteSpace(ownerOid)
            ? FeedSourceBaseUrl
            : $"{FeedSourceBaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }
}
