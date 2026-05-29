using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for per-user event-to-dispatcher mappings.
/// </summary>
public class UserEventDistributorMappingManager(IUserEventDistributorMappingDataStore dataStore) : IUserEventDistributorMappingManager
{
    /// <inheritdoc />
    public Task<List<UserEventDistributorMapping>> GetByUserAsync(
        string ownerOid,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetByUserAsync(ownerOid, activeOnly, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<UserEventDistributorMapping>> GetByUserAndEventTypeAsync(
        string ownerOid,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ValidateEventType(eventType);
        return dataStore.GetByUserAndEventTypeAsync(ownerOid, eventType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserEventDistributorMapping?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return dataStore.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserEventDistributorMapping?> SaveAsync(
        UserEventDistributorMapping mapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping.CreatedByEntraOid);
        ValidateEventType(mapping.EventType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mapping.SocialMediaPlatformId);
        return dataStore.SaveAsync(mapping, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.DeleteAsync(id, ownerOid, cancellationToken);
    }

    private static void ValidateEventType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        var isSupported = eventType is MessageTemplates.MessageTypes.NewSyndicationFeedItem
            or MessageTemplates.MessageTypes.NewYouTubeItem
            or MessageTemplates.MessageTypes.NewSpeakingEngagement
            or MessageTemplates.MessageTypes.RandomPost
            or MessageTemplates.MessageTypes.ScheduledItem;

        if (!isSupported)
        {
            throw new ArgumentException("EventType must be a supported message type", nameof(eventType));
        }
    }
}
