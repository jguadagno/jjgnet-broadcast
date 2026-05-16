using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user Bluesky publisher settings
/// </summary>
public class UserPublisherBlueskySettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPublisherBlueskySettingsDataStore> logger) : IUserPublisherBlueskySettingsDataStore
{
    /// <inheritdoc />
    public async Task<UserPublisherBlueskySettings?> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        try
        {
            var entity = await broadcastingContext.UserPublisherBlueskySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == ownerOid, cancellationToken);

            return entity is null ? null : mapper.Map<UserPublisherBlueskySettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve Bluesky settings for owner {OwnerOid}",
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserPublisherBlueskySettings?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var entity = await broadcastingContext.UserPublisherBlueskySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity is null ? null : mapper.Map<UserPublisherBlueskySettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve Bluesky settings for ID {Id}",
                id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserPublisherBlueskySettings?> SaveAsync(
        UserPublisherBlueskySettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);

        try
        {
            var existing = await broadcastingContext.UserPublisherBlueskySettings
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == settings.CreatedByEntraOid, cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPublisherBlueskySettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserPublisherBlueskySettings.Add(existing);
            }

            existing.IsEnabled = settings.IsEnabled;
            existing.UserName = settings.UserName;
            existing.HasAppPassword = settings.HasAppPassword;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserPublisherBlueskySettings>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save Bluesky settings for owner {OwnerOid}",
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
            var existing = await broadcastingContext.UserPublisherBlueskySettings
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByEntraOid == ownerOid, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPublisherBlueskySettings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete Bluesky settings for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}
