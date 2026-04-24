using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the user collector YouTube channel API.
/// </summary>
public class UserCollectorYouTubeChannelService(
    IDownstreamApi apiClient,
    ILogger<UserCollectorYouTubeChannelService> logger) : IUserCollectorYouTubeChannelService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string YouTubeChannelBaseUrl = "/UserCollectorYouTubeChannels";

    public async Task<List<UserCollectorYouTubeChannel>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<List<UserCollectorYouTubeChannel>>(ApiServiceName, options =>
        {
            options.RelativePath = YouTubeChannelBaseUrl;
        });

        return response ?? [];
    }

    public async Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<List<UserCollectorYouTubeChannel>>(ApiServiceName, options =>
        {
            options.RelativePath = BuildRelativePath(ownerOid);
        });

        return response ?? [];
    }

    public async Task<UserCollectorYouTubeChannel?> GetByIdAsync(int id)
    {
        var response = await apiClient.GetForUserAsync<UserCollectorYouTubeChannel>(ApiServiceName, options =>
        {
            options.RelativePath = $"{YouTubeChannelBaseUrl}/{id}";
        });

        return response;
    }

    public async Task<UserCollectorYouTubeChannel?> AddCurrentUserAsync(UserCollectorYouTubeChannel channel)
    {
        return await AddAsync(YouTubeChannelBaseUrl, channel);
    }

    public async Task<UserCollectorYouTubeChannel?> AddByUserAsync(string ownerOid, UserCollectorYouTubeChannel channel)
    {
        return await AddAsync(BuildRelativePath(ownerOid), channel);
    }

    public async Task<UserCollectorYouTubeChannel?> UpdateCurrentUserAsync(UserCollectorYouTubeChannel channel)
    {
        return await UpdateAsync($"{YouTubeChannelBaseUrl}/{channel.Id}", channel);
    }

    public async Task<UserCollectorYouTubeChannel?> UpdateByUserAsync(string ownerOid, UserCollectorYouTubeChannel channel)
    {
        return await UpdateAsync($"{BuildRelativePath(ownerOid)}/{channel.Id}", channel);
    }

    public async Task<bool> DeleteCurrentUserAsync(int id)
    {
        return await DeleteAsync($"{YouTubeChannelBaseUrl}/{id}");
    }

    public async Task<bool> DeleteByUserAsync(string ownerOid, int id)
    {
        return await DeleteAsync($"{BuildRelativePath(ownerOid)}/{id}");
    }

    private async Task<UserCollectorYouTubeChannel?> AddAsync(string relativePath, UserCollectorYouTubeChannel channel)
    {
        var response = await apiClient.PostForUserAsync<UserCollectorYouTubeChannel, UserCollectorYouTubeChannel>(
            ApiServiceName,
            channel,
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (response is null)
        {
            logger.LogWarning(
                "YouTube channel add returned no content for owner {OwnerOid}",
                channel.CreatedByEntraOid);
        }

        return response;
    }

    private async Task<UserCollectorYouTubeChannel?> UpdateAsync(string relativePath, UserCollectorYouTubeChannel channel)
    {
        var response = await apiClient.PutForUserAsync<UserCollectorYouTubeChannel, UserCollectorYouTubeChannel>(
            ApiServiceName,
            channel,
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (response is null)
        {
            logger.LogWarning(
                "YouTube channel update returned no content for id {Id}",
                channel.Id);
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
            logger.LogError(ex, "Failed to delete YouTube channel at {RelativePath}", relativePath);
            return false;
        }
    }

    private static string BuildRelativePath(string? ownerOid = null)
    {
        return string.IsNullOrWhiteSpace(ownerOid)
            ? YouTubeChannelBaseUrl
            : $"{YouTubeChannelBaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }
}
