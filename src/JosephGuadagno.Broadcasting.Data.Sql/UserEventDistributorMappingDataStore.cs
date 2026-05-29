using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user event-to-dispatcher mappings.
/// </summary>
public class UserEventDistributorMappingDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserEventDistributorMappingDataStore> logger) : IUserEventDistributorMappingDataStore
{
    /// <inheritdoc />
    public async Task<List<UserEventDistributorMapping>> GetByUserAsync(
        string ownerOid,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var query = broadcastingContext.UserEventDistributorMappings
            .AsNoTracking()
            .Where(m => m.CreatedByEntraOid == ownerOid);

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        var entities = await query
            .OrderBy(m => m.EventType)
            .ThenBy(m => m.SocialMediaPlatformId)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserEventDistributorMapping>>(entities);
    }

    /// <inheritdoc />
    public async Task<List<UserEventDistributorMapping>> GetByUserAndEventTypeAsync(
        string ownerOid,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        try
        {
            var entities = await broadcastingContext.UserEventDistributorMappings
                .AsNoTracking()
                .Where(m => m.CreatedByEntraOid == ownerOid && m.EventType == eventType && m.IsActive)
                .OrderBy(m => m.SocialMediaPlatformId)
                .ToListAsync(cancellationToken);

            return mapper.Map<List<UserEventDistributorMapping>>(entities);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve event dispatcher mappings for owner {OwnerOid} and event type {EventType}",
                LogSanitizer.Sanitize(ownerOid),
                LogSanitizer.Sanitize(eventType));
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<UserEventDistributorMapping?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var entity = await broadcastingContext.UserEventDistributorMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            return entity is null ? null : mapper.Map<UserEventDistributorMapping>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve event dispatcher mapping for ID {Id}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserEventDistributorMapping?> SaveAsync(
        UserEventDistributorMapping mapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping.EventType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mapping.SocialMediaPlatformId);

        try
        {
            Models.UserEventDistributorMapping? existing = null;
            if (mapping.Id > 0)
            {
                existing = await broadcastingContext.UserEventDistributorMappings
                    .FirstOrDefaultAsync(
                        m => m.Id == mapping.Id && m.CreatedByEntraOid == mapping.CreatedByEntraOid,
                        cancellationToken);
            }

            existing ??= await broadcastingContext.UserEventDistributorMappings
                .FirstOrDefaultAsync(
                    m => m.CreatedByEntraOid == mapping.CreatedByEntraOid
                        && m.EventType == mapping.EventType
                        && m.SocialMediaPlatformId == mapping.SocialMediaPlatformId,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserEventDistributorMapping
                {
                    CreatedByEntraOid = mapping.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserEventDistributorMappings.Add(existing);
            }

            existing.EventType = mapping.EventType;
            existing.SocialMediaPlatformId = mapping.SocialMediaPlatformId;
            existing.IsActive = mapping.IsActive;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return mapper.Map<UserEventDistributorMapping>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save event dispatcher mapping for owner {OwnerOid}, event type {EventType}, platform {PlatformId}",
                LogSanitizer.Sanitize(mapping.CreatedByEntraOid),
                LogSanitizer.Sanitize(mapping.EventType),
                mapping.SocialMediaPlatformId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        int id,
        string ownerOid,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        try
        {
            var existing = await broadcastingContext.UserEventDistributorMappings
                .FirstOrDefaultAsync(
                    m => m.Id == id && m.CreatedByEntraOid == ownerOid,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserEventDistributorMappings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete event dispatcher mapping for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}
