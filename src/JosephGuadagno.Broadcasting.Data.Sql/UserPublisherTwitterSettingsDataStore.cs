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
public class UserPublisherTwitterSettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPublisherTwitterSettingsDataStore> logger) : IUserPublisherTwitterSettingsDataStore
{
    /// <inheritdoc />
    public async Task<UserPublisherTwitterSettings?> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        try
        {
            var entity = await broadcastingContext.UserPublisherTwitterSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == ownerOid, cancellationToken);

            return entity is null ? null : mapper.Map<UserPublisherTwitterSettings>(entity);
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
    public async Task<UserPublisherTwitterSettings?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var entity = await broadcastingContext.UserPublisherTwitterSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity is null ? null : mapper.Map<UserPublisherTwitterSettings>(entity);
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
    public async Task<UserPublisherTwitterSettings?> SaveAsync(
        UserPublisherTwitterSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);

        try
        {
            var existing = await broadcastingContext.UserPublisherTwitterSettings
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == settings.CreatedByEntraOid, cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPublisherTwitterSettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserPublisherTwitterSettings.Add(existing);
            }

            existing.IsEnabled = settings.IsEnabled;
            existing.HasConsumerKey = settings.HasConsumerKey;
            existing.HasConsumerSecret = settings.HasConsumerSecret;
            existing.HasAccessToken = settings.HasAccessToken;
            existing.HasAccessTokenSecret = settings.HasAccessTokenSecret;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserPublisherTwitterSettings>(existing);
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
            var existing = await broadcastingContext.UserPublisherTwitterSettings
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByEntraOid == ownerOid, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPublisherTwitterSettings.Remove(existing);
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
