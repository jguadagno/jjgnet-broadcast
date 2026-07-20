using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user random post schedules and filtering settings.
/// </summary>
public class UserRandomPostSettingsDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserRandomPostSettingsDataStore> logger) : IUserRandomPostSettingsDataStore
{
    /// <inheritdoc />
    public async Task<List<UserRandomPostSettings>> GetByUserAsync(
        string ownerOid,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var query = broadcastingContext.UserRandomPostSettings
            .AsNoTracking()
            .Where(s => s.CreatedByEntraOid == ownerOid);

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var entities = await query
            .OrderBy(s => s.SocialMediaPlatformId)
            .ThenBy(s => s.CronExpression)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserRandomPostSettings>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserRandomPostSettings?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var entity = await broadcastingContext.UserRandomPostSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity is null ? null : mapper.Map<UserRandomPostSettings>(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve random post settings for ID {Id}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<UserRandomPostSettings>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserRandomPostSettings
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.CreatedByEntraOid)
            .ThenBy(s => s.SocialMediaPlatformId)
            .ThenBy(s => s.CronExpression)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserRandomPostSettings>>(entities);
    }

    /// <inheritdoc />
    public async Task<List<UserRandomPostSettings>> GetAllDueAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserRandomPostSettings
            .AsNoTracking()
            .Where(s => s.IsActive && (s.NextRunDateUtc == null || s.NextRunDateUtc <= utcNow))
            .OrderBy(s => s.CreatedByEntraOid)
            .ThenBy(s => s.SocialMediaPlatformId)
            .ThenBy(s => s.CronExpression)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserRandomPostSettings>>(entities);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateNextRunAsync(
        int id,
        DateTimeOffset? nextRunUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var existing = await broadcastingContext.UserRandomPostSettings
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            existing.NextRunDateUtc = nextRunUtc;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;
            existing.CronParseFailureCount = 0;

            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update next run date for random post settings ID {Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IncrementCronFailureAsync(
        int id,
        int failureThreshold = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var existing = await broadcastingContext.UserRandomPostSettings
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            existing.CronParseFailureCount++;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            var deactivated = false;
            if (existing.CronParseFailureCount >= failureThreshold)
            {
                existing.IsActive = false;
                deactivated = true;
            }

            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return deactivated;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to increment cron failure count for random post settings ID {Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<UserRandomPostSettings?> SaveAsync(
        UserRandomPostSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CronExpression);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.SocialMediaPlatformId);

        try
        {
            Models.UserRandomPostSettings? existing = null;
            if (settings.Id > 0)
            {
                existing = await broadcastingContext.UserRandomPostSettings
                    .FirstOrDefaultAsync(
                        s => s.Id == settings.Id && s.CreatedByEntraOid == settings.CreatedByEntraOid,
                        cancellationToken);
            }

            existing ??= await broadcastingContext.UserRandomPostSettings
                .FirstOrDefaultAsync(
                    s => s.CreatedByEntraOid == settings.CreatedByEntraOid
                        && s.SocialMediaPlatformId == settings.SocialMediaPlatformId
                        && s.CronExpression == settings.CronExpression,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserRandomPostSettings
                {
                    CreatedByEntraOid = settings.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserRandomPostSettings.Add(existing);
            }

            existing.SocialMediaPlatformId = settings.SocialMediaPlatformId;
            existing.CronExpression = settings.CronExpression;
            existing.CutoffDate = settings.CutoffDate;
            existing.ExcludedCategories = string.Join(",", settings.ExcludedCategories
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Select(category => category.Trim()));
            existing.IsActive = settings.IsActive;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return mapper.Map<UserRandomPostSettings>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save random post settings for owner {OwnerOid}, platform {PlatformId}, cron {CronExpression}",
                LogSanitizer.Sanitize(settings.CreatedByEntraOid),
                settings.SocialMediaPlatformId,
                LogSanitizer.Sanitize(settings.CronExpression));
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
            var existing = await broadcastingContext.UserRandomPostSettings
                .FirstOrDefaultAsync(
                    s => s.Id == id && s.CreatedByEntraOid == ownerOid,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserRandomPostSettings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete random post settings for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}
