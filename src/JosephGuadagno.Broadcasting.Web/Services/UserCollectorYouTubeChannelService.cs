using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
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
    private const string YouTubeChannelBaseUrl = "/Collectors/YouTube/Settings";

    public async Task<List<UserCollectorYouTubeChannel>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserCollectorYouTubeChannel>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{YouTubeChannelBaseUrl}?pageSize={Pagination.MaxPageSize}";
        });

        if (response is null)
        {
            logger.LogWarning("GetCurrentUserAsync downstream returned null for YouTube channels");
        }

        return response?.Items.ToList() ?? [];
    }

    public async Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserCollectorYouTubeChannel>>(ApiServiceName, options =>
        {
            options.RelativePath = BuildRelativePath(ownerOid) + $"&pageSize={Pagination.MaxPageSize}";
        });

        if (response is null)
        {
            logger.LogWarning("GetByUserAsync downstream returned null for YouTube channels (ownerOid='{OwnerOid}')", LogSanitizer.Sanitize(ownerOid));
        }

        return response?.Items.ToList() ?? [];
    }

    public async Task<UserCollectorYouTubeChannel?> GetByIdAsync(int id)
    {
        var response = await apiClient.GetOptionalForUserAsync<UserCollectorYouTubeChannel>(ApiServiceName, options =>
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
        return await DeleteAsync($"{YouTubeChannelBaseUrl}/{id}", id);
    }

    public async Task<bool> DeleteByUserAsync(string ownerOid, int id)
    {
        return await DeleteAsync($"{BuildRelativePath(ownerOid)}/{id}", id, ownerOid);
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
                "API returned null for {Operation} with channelId {ChannelId} and ownerOid '{OwnerOid}'",
                nameof(AddAsync),
                channel.Id,
                LogSanitizer.Sanitize(channel.CreatedByEntraOid));
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
                "API returned null for {Operation} with channelId {ChannelId}",
                nameof(UpdateAsync),
                channel.Id);
        }

        return response;
    }

    private async Task<bool> DeleteAsync(string relativePath, int id, string? ownerOid = null)
    {
        var response = await apiClient.CallApiForUserAsync<HttpResponseMessage>(ApiServiceName, options =>
        {
            options.HttpMethod = HttpMethod.Delete.Method;
            options.RelativePath = relativePath;
        });

        if (response is { StatusCode: HttpStatusCode.NoContent })
        {
            return true;
        }

        logger.LogWarning(
            "API returned unexpected status for {Operation} with id {Id} and ownerOid '{OwnerOid}': {StatusCode}",
            nameof(DeleteAsync),
            id,
            LogSanitizer.Sanitize(ownerOid),
            response?.StatusCode);
        return false;
    }

    private static string BuildRelativePath(string? ownerOid = null)
    {
        return string.IsNullOrWhiteSpace(ownerOid)
            ? YouTubeChannelBaseUrl
            : $"{YouTubeChannelBaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }
}
