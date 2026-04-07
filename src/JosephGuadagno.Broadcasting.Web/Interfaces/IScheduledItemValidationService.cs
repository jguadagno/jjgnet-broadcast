using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service for validating and looking up scheduled item sources
/// </summary>
public interface IScheduledItemValidationService
{
    /// <summary>
    /// Validates that a source item exists for the given type and primary key
    /// </summary>
    /// <param name="itemType">The type of item (Engagements, Talks, SyndicationFeedSources, YouTubeSources)</param>
    /// <param name="itemPrimaryKey">The primary key of the item</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A lookup result with validation status and item details</returns>
    Task<ScheduledItemLookupResult> ValidateItemAsync(
        ScheduledItemType itemType, 
        int itemPrimaryKey, 
        CancellationToken cancellationToken = default);
}
