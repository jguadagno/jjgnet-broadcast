using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Stub implementation of <see cref="IScheduledItemValidationService"/>.
/// Source-item validation requires a dedicated API endpoint that does not yet exist.
/// See: https://github.com/jguadagno/jjgnet-broadcast/issues (ScheduledItemValidationService redesign)
/// </summary>
public class ScheduledItemValidationService : IScheduledItemValidationService
{
    /// <inheritdoc />
    public Task<ScheduledItemLookupResult> ValidateItemAsync(
        ScheduledItemType itemType,
        int itemPrimaryKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ScheduledItemLookupResult
        {
            IsValid = false,
            ErrorMessage = "Source item validation is not yet available. Please verify the item ID manually."
        });
    }
}
