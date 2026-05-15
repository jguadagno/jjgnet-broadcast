using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user Facebook publisher settings
/// </summary>
public class UserPublisherFacebookSettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPublisherFacebookSettingsDataStore> logger) : IUserPublisherFacebookSettingsDataStore
{
    /// <inheritdoc />
    public async Task<UserPublisherFacebookSettings?> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entity = await broadcastingContext.UserPublisherFacebookSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CreatedByEntraOid == ownerOid, cancellationToken);

        return entity is null ? null : mapper.Map<UserPublisherFacebookSettings>(entity);
    }

    /// <inheritdoc />
    public async Task<UserPublisherFacebookSettings?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var entity = await broadcastingContext.UserPublisherFacebookSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<UserPublisherFacebookSettings>(entity);
    }

    /// <inheritdoc />
    public async Task<UserPublisherFacebookSettings?> SaveAsync(
        UserPublisherFacebookSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);

        try
        {
            var existing = await broadcastingContext.UserPublisherFacebookSettings
                .FirstOrDefaultAsync(s => s.CreatedByEntraOid == settings.CreatedByEntraOid, cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPublisherFacebookSettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserPublisherFacebookSettings.Add(existing);
            }

            existing.IsEnabled = settings.IsEnabled;
            existing.PageId = settings.PageId;
            existing.AppId = settings.AppId;
            existing.HasPageAccessToken = settings.HasPageAccessToken;
            existing.HasAppSecret = settings.HasAppSecret;
            existing.HasClientToken = settings.HasClientToken;
            existing.HasShortLivedAccessToken = settings.HasShortLivedAccessToken;
            existing.HasLongLivedAccessToken = settings.HasLongLivedAccessToken;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserPublisherFacebookSettings>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save Facebook settings for owner {OwnerOid}",
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
            var existing = await broadcastingContext.UserPublisherFacebookSettings
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByEntraOid == ownerOid, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPublisherFacebookSettings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete Facebook settings for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}
