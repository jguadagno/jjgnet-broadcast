using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user LinkedIn publisher settings
/// </summary>
public class UserPlatformLinkedInSettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPlatformLinkedInSettingsDataStore> logger) : IUserPlatformLinkedInSettingsDataStore
{
    /// <inheritdoc />
    public async Task<UserPlatformLinkedInSettings?> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        try
        {
            var entity = await broadcastingContext.UserPlatformLinkedInSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == ownerOid, cancellationToken);

            return entity is null ? null : mapper.Map<UserPlatformLinkedInSettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve LinkedIn settings for owner {OwnerOid}",
                LogSanitizer.Sanitize(ownerOid));
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserPlatformLinkedInSettings?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var entity = await broadcastingContext.UserPlatformLinkedInSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity is null ? null : mapper.Map<UserPlatformLinkedInSettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to retrieve LinkedIn settings for ID {Id}",
                id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserPlatformLinkedInSettings?> SaveAsync(
        UserPlatformLinkedInSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);

        try
        {
            var existing = await broadcastingContext.UserPlatformLinkedInSettings
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == settings.CreatedByEntraOid, cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPlatformLinkedInSettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserPlatformLinkedInSettings.Add(existing);
            }

            existing.IsEnabled = settings.IsEnabled;
            existing.AuthorId = settings.AuthorId;
            existing.ClientId = settings.ClientId;
            existing.HasClientSecret = settings.HasClientSecret;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserPlatformLinkedInSettings>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save LinkedIn settings for owner {OwnerOid}",
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
            var existing = await broadcastingContext.UserPlatformLinkedInSettings
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByEntraOid == ownerOid, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPlatformLinkedInSettings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete LinkedIn settings for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}

