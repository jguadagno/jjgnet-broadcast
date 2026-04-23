using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;

using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls the API to validate that a scheduled item's source record exists.
/// </summary>
public class ScheduledItemValidationService(
    IDownstreamApi apiClient,
    ILogger<ScheduledItemValidationService> logger) : IScheduledItemValidationService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string ValidateSourceItemUrl = "/Schedules/validate-source-item";

    /// <inheritdoc />
    public async Task<ScheduledItemLookupResult> ValidateItemAsync(
        ScheduledItemType itemType,
        int itemPrimaryKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.GetForUserAsync<ScheduledItemLookupResult>(ApiServiceName, options =>
            {
                options.RelativePath = $"{ValidateSourceItemUrl}?itemType={itemType}&itemPrimaryKey={itemPrimaryKey}";
            });

            return response ?? new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = "Validation service returned no response"
            };
        }
        catch (Exception ex)
        {
            var sanitized = ex.Message.Replace("\r", string.Empty).Replace("\n", string.Empty);
            logger.LogError(ex, "Error validating source item type={ItemType} key={ItemPrimaryKey}: {Message}",
                itemType, itemPrimaryKey, sanitized);
            return new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = "An error occurred while validating the item"
            };
        }
    }
}
