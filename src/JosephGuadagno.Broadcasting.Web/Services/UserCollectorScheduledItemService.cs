using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls the per-user scheduled item collector configuration API endpoints.
/// </summary>
public class UserCollectorScheduledItemService(
    IDownstreamApi apiClient,
    ILogger<UserCollectorScheduledItemService> logger) : IUserCollectorScheduledItemService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string BaseUrl = "/Collectors/ScheduledItem/Settings";

    public async Task<UserCollectorScheduledItem?> GetAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<UserCollectorScheduledItem>(ApiServiceName, options =>
        {
            options.RelativePath = $"{BaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
        });

        return response;
    }

    public async Task<UserCollectorScheduledItem?> SaveAsync(UserCollectorScheduledItem item)
    {
        var response = await apiClient.PutForUserAsync<UserCollectorScheduledItem, UserCollectorScheduledItem>(
            ApiServiceName,
            item,
            options =>
            {
                options.RelativePath = $"{BaseUrl}?ownerOid={Uri.EscapeDataString(item.CreatedByEntraOid)}";
            });

        if (response is null)
        {
            logger.LogWarning(
                "Scheduled item save returned no content for owner {OwnerOid}",
                item.CreatedByEntraOid);
        }

        return response;
    }

    public async Task<bool> DeleteAsync(string ownerOid)
    {
        try
        {
            await apiClient.CallApiForUserAsync(
                ApiServiceName,
                options =>
                {
                    options.HttpMethod = HttpMethod.Delete.Method;
                    options.RelativePath = $"{BaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
                });

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete scheduled item config for owner {OwnerOid}", ownerOid);
            return false;
        }
    }
}
