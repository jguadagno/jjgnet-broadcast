using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the user collector speaking engagement API.
/// </summary>
public class UserCollectorSpeakingEngagementService(
    IDownstreamApi apiClient,
    ILogger<UserCollectorSpeakingEngagementService> logger) : IUserCollectorSpeakingEngagementService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/UserCollectorSpeakingEngagements";

    public async Task<List<UserCollectorSpeakingEngagement>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserCollectorSpeakingEngagement>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}?pageSize={Pagination.MaxPageSize}";
        });

        return response?.Items.ToList() ?? [];
    }

    public async Task<List<UserCollectorSpeakingEngagement>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserCollectorSpeakingEngagement>>(ApiServiceName, options =>
        {
            options.RelativePath = BuildRelativePath(ownerOid) + $"&pageSize={Pagination.MaxPageSize}";
        });

        return response?.Items.ToList() ?? [];
    }

    public async Task<UserCollectorSpeakingEngagement?> GetByIdAsync(int id)
    {
        var response = await apiClient.GetForUserAsync<UserCollectorSpeakingEngagement>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}/{id}";
        });

        return response;
    }

    public async Task<UserCollectorSpeakingEngagement?> AddCurrentUserAsync(UserCollectorSpeakingEngagement engagement)
    {
        return await AddAsync(BaseUrl, engagement);
    }

    public async Task<UserCollectorSpeakingEngagement?> AddByUserAsync(string ownerOid, UserCollectorSpeakingEngagement engagement)
    {
        return await AddAsync(BuildRelativePath(ownerOid), engagement);
    }

    public async Task<UserCollectorSpeakingEngagement?> UpdateCurrentUserAsync(UserCollectorSpeakingEngagement engagement)
    {
        return await UpdateAsync($"{BaseUrl}/{engagement.Id}", engagement);
    }

    public async Task<UserCollectorSpeakingEngagement?> UpdateByUserAsync(string ownerOid, UserCollectorSpeakingEngagement engagement)
    {
        return await UpdateAsync($"{BuildRelativePath(ownerOid)}/{engagement.Id}", engagement);
    }

    public async Task<bool> DeleteCurrentUserAsync(int id)
    {
        return await DeleteAsync($"{BaseUrl}/{id}");
    }

    public async Task<bool> DeleteByUserAsync(string ownerOid, int id)
    {
        return await DeleteAsync($"{BuildRelativePath(ownerOid)}/{id}");
    }

    private async Task<UserCollectorSpeakingEngagement?> AddAsync(string relativePath, UserCollectorSpeakingEngagement engagement)
    {
        var response = await apiClient.PostForUserAsync<UserCollectorSpeakingEngagement, UserCollectorSpeakingEngagement>(
            ApiServiceName,
            engagement,
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Speaking engagement add returned no content for owner {OwnerOid}",
                engagement.CreatedByEntraOid);
        }

        return response;
    }

    private async Task<UserCollectorSpeakingEngagement?> UpdateAsync(string relativePath, UserCollectorSpeakingEngagement engagement)
    {
        var response = await apiClient.PutForUserAsync<UserCollectorSpeakingEngagement, UserCollectorSpeakingEngagement>(
            ApiServiceName,
            engagement,
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (response is null)
        {
            logger.LogWarning(
                "Speaking engagement update returned no content for id {Id}",
                engagement.Id);
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
            logger.LogError(ex, "Failed to delete speaking engagement at {RelativePath}", relativePath);
            return false;
        }
    }

    private static string BuildRelativePath(string? ownerOid = null)
    {
        return string.IsNullOrWhiteSpace(ownerOid)
            ? BaseUrl
            : $"{BaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }
}
