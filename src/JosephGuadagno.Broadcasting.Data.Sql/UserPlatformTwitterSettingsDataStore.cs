using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user Twitter/X publisher settings
/// </summary>
public class UserPlatformTwitterSettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPlatformTwitterSettingsDataStore> logger) : IUserPlatformTwitterSettingsDataStore
{
    /// <inheritdoc />
    public async Task<UserPlatformTwitterSettings?> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        try
        {
            var entity = await broadcastingContext.UserPlatformTwitterSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == ownerOid, cancellationToken);

            return entity is null ? null : mapper.Map<UserPlatformTwitterSettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve Twitter settings for owner {OwnerOid}",
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserPlatformTwitterSettings?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var entity = await broadcastingContext.UserPlatformTwitterSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity is null ? null : mapper.Map<UserPlatformTwitterSettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve Twitter settings for ID {Id}",
                id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserPlatformTwitterSettings?> SaveAsync(
        UserPlatformTwitterSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);

        try
        {
            var existing = await broadcastingContext.UserPlatformTwitterSettings
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == settings.CreatedByEntraOid, cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPlatformTwitterSettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserPlatformTwitterSettings.Add(existing);
            }

            existing.IsEnabled = settings.IsEnabled;
            existing.HasConsumerKey = settings.HasConsumerKey;
            existing.HasConsumerSecret = settings.HasConsumerSecret;
            existing.HasAccessToken = settings.HasAccessToken;
            existing.HasAccessTokenSecret = settings.HasAccessTokenSecret;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserPlatformTwitterSettings>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save Twitter settings for owner {OwnerOid}",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        int id, string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        try
        {
            var existing = await broadcastingContext.UserPlatformTwitterSettings
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByEntraOid == ownerOid, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPlatformTwitterSettings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete Twitter settings for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}

