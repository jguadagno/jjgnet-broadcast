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
public class UserPublisherLinkedInSettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPublisherLinkedInSettingsDataStore> logger) : IUserPublisherLinkedInSettingsDataStore
{
    /// <inheritdoc />
    public async Task<UserPublisherLinkedInSettings?> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entity = await broadcastingContext.UserPublisherLinkedInSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CreatedByEntraOid == ownerOid, cancellationToken);

        return entity is null ? null : mapper.Map<UserPublisherLinkedInSettings>(entity);
    }

    /// <inheritdoc />
    public async Task<UserPublisherLinkedInSettings?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var entity = await broadcastingContext.UserPublisherLinkedInSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<UserPublisherLinkedInSettings>(entity);
    }

    /// <inheritdoc />
    public async Task<UserPublisherLinkedInSettings?> SaveAsync(
        UserPublisherLinkedInSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);

        try
        {
            var existing = await broadcastingContext.UserPublisherLinkedInSettings
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == settings.CreatedByEntraOid, cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPublisherLinkedInSettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserPublisherLinkedInSettings.Add(existing);
            }

            existing.IsEnabled = settings.IsEnabled;
            existing.AuthorId = settings.AuthorId;
            existing.ClientId = settings.ClientId;
            existing.HasClientSecret = settings.HasClientSecret;
            existing.HasAccessToken = settings.HasAccessToken;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserPublisherLinkedInSettings>(existing);
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
            var existing = await broadcastingContext.UserPublisherLinkedInSettings
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByEntraOid == ownerOid, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPublisherLinkedInSettings.Remove(existing);
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
