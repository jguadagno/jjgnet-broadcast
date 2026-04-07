using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Service for validating and looking up scheduled item sources
/// </summary>
public class ScheduledItemValidationService(
    IEngagementManager engagementManager,
    ISyndicationFeedSourceManager syndicationFeedSourceManager,
    IYouTubeSourceManager youTubeSourceManager,
    ILogger<ScheduledItemValidationService> logger) : IScheduledItemValidationService
{
    /// <summary>
    /// Validates that a source item exists for the given type and primary key
    /// </summary>
    /// <param name="itemType">The type of item (Engagements, Talks, SyndicationFeedSources, YouTubeSources)</param>
    /// <param name="itemPrimaryKey">The primary key of the item</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A lookup result with validation status and item details</returns>
    public async Task<ScheduledItemLookupResult> ValidateItemAsync(
        ScheduledItemType itemType, 
        int itemPrimaryKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return itemType switch
            {
                ScheduledItemType.Engagements => await ValidateEngagementAsync(itemPrimaryKey, cancellationToken),
                ScheduledItemType.Talks => await ValidateTalkAsync(itemPrimaryKey, cancellationToken),
                ScheduledItemType.SyndicationFeedSources => await ValidateSyndicationFeedSourceAsync(itemPrimaryKey, cancellationToken),
                ScheduledItemType.YouTubeSources => await ValidateYouTubeSourceAsync(itemPrimaryKey, cancellationToken),
                _ => new ScheduledItemLookupResult
                {
                    IsValid = false,
                    ErrorMessage = $"Unknown item type: {itemType}"
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating scheduled item type {ItemType} with key {ItemPrimaryKey}", itemType, itemPrimaryKey);
            return new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = "An error occurred while validating the item"
            };
        }
    }

    private async Task<ScheduledItemLookupResult> ValidateEngagementAsync(int primaryKey, CancellationToken cancellationToken)
    {
        var engagement = await engagementManager.GetAsync(primaryKey, cancellationToken);
        if (engagement == null)
        {
            return new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = $"Engagement with ID {primaryKey} not found"
            };
        }

        return new ScheduledItemLookupResult
        {
            IsValid = true,
            ItemTitle = engagement.Name,
            ItemDetails = $"{engagement.StartDateTime:yyyy-MM-dd} - {engagement.EndDateTime:yyyy-MM-dd}"
        };
    }

    private async Task<ScheduledItemLookupResult> ValidateTalkAsync(int primaryKey, CancellationToken cancellationToken)
    {
        try
        {
            var talk = await engagementManager.GetTalkAsync(primaryKey, cancellationToken);
            if (talk == null)
            {
                return new ScheduledItemLookupResult
                {
                    IsValid = false,
                    ErrorMessage = $"Talk with ID {primaryKey} not found"
                };
            }

            return new ScheduledItemLookupResult
            {
                IsValid = true,
                ItemTitle = talk.Name,
                ItemDetails = $"{talk.StartDateTime:yyyy-MM-dd HH:mm}"
            };
        }
        catch (ApplicationException ex)
        {
            return new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<ScheduledItemLookupResult> ValidateSyndicationFeedSourceAsync(int primaryKey, CancellationToken cancellationToken)
    {
        var feedSource = await syndicationFeedSourceManager.GetAsync(primaryKey, cancellationToken);
        if (feedSource == null)
        {
            return new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = $"Syndication feed source with ID {primaryKey} not found"
            };
        }

        return new ScheduledItemLookupResult
        {
            IsValid = true,
            ItemTitle = feedSource.Title,
            ItemDetails = $"By {feedSource.Author} - {feedSource.PublicationDate:yyyy-MM-dd}"
        };
    }

    private async Task<ScheduledItemLookupResult> ValidateYouTubeSourceAsync(int primaryKey, CancellationToken cancellationToken)
    {
        var youTubeSource = await youTubeSourceManager.GetAsync(primaryKey, cancellationToken);
        if (youTubeSource == null)
        {
            return new ScheduledItemLookupResult
            {
                IsValid = false,
                ErrorMessage = $"YouTube source with ID {primaryKey} not found"
            };
        }

        return new ScheduledItemLookupResult
        {
            IsValid = true,
            ItemTitle = youTubeSource.Title,
            ItemDetails = $"{youTubeSource.PublicationDate:yyyy-MM-dd}"
        };
    }
}
