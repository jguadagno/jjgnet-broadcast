using System.Net;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Extensions;
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
    private const string BaseUrl = "/Collectors/SpeakingEngagement/Settings";

    public async Task<List<UserCollectorSpeakingEngagement>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserCollectorSpeakingEngagement>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}?pageSize={Pagination.MaxPageSize}";
        });

        if (response is null)
        {
            logger.LogWarning("GetCurrentUserAsync downstream returned null for speaking engagements");
        }

        return response?.Items.ToList() ?? [];
    }

    public async Task<List<UserCollectorSpeakingEngagement>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserCollectorSpeakingEngagement>>(ApiServiceName, options =>
        {
            options.RelativePath = BuildRelativePath(ownerOid) + $"&pageSize={Pagination.MaxPageSize}";
        });

        if (response is null)
        {
            logger.LogWarning("GetByUserAsync downstream returned null for speaking engagements (ownerOid='{OwnerOid}')", LogSanitizer.Sanitize(ownerOid));
        }

        return response?.Items.ToList() ?? [];
    }

    public async Task<UserCollectorSpeakingEngagement?> GetByIdAsync(int id)
    {
        var response = await apiClient.GetOptionalForUserAsync<UserCollectorSpeakingEngagement>(ApiServiceName, options =>
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
        return await DeleteAsync($"{BaseUrl}/{id}", id);
    }

    public async Task<bool> DeleteByUserAsync(string ownerOid, int id)
    {
        return await DeleteAsync($"{BuildRelativePath(ownerOid)}/{id}", id, ownerOid);
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
                "API returned null for {Operation} with engagementId {EngagementId} and ownerOid '{OwnerOid}'",
                nameof(AddAsync),
                engagement.Id,
                LogSanitizer.Sanitize(engagement.CreatedByEntraOid));
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
                "API returned null for {Operation} with engagementId {EngagementId}",
                nameof(UpdateAsync),
                engagement.Id);
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
            ? BaseUrl
            : $"{BaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }
}
