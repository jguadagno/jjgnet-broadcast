using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user RSS/Atom/JSON feed collector configurations
/// </summary>
public class UserCollectorFeedSourceDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserCollectorFeedSourceDataStore> logger) : IUserCollectorFeedSourceDataStore
{
    /// <inheritdoc />
    public async Task<List<UserCollectorFeedSource>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entities = await broadcastingContext.UserCollectorFeedSources
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorFeedSource>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorFeedSource?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var entity = await broadcastingContext.UserCollectorFeedSources
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<UserCollectorFeedSource>(entity);
    }

    /// <inheritdoc />
    public async Task<List<UserCollectorFeedSource>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserCollectorFeedSources
            .AsNoTracking()
            .Where(c => c.IsActive == true)
            .OrderBy(c => c.CreatedByEntraOid)
            .ThenBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorFeedSource>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorFeedSource?> SaveAsync(
        UserCollectorFeedSource config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FeedUrl);

        try
        {
            var existing = await broadcastingContext.UserCollectorFeedSources
                .FirstOrDefaultAsync(
                    c => c.CreatedByEntraOid == config.CreatedByEntraOid && c.FeedUrl == config.FeedUrl,
                    cancellationToken);

            if (existing is null)
            {
                existing = mapper.Map<Models.UserCollectorFeedSource>(config);
                existing.CreatedOn = DateTimeOffset.UtcNow;
                broadcastingContext.UserCollectorFeedSources.Add(existing);
            }
            else
            {
                mapper.Map(config, existing);
            }

            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserCollectorFeedSource>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save feed source config for owner {OwnerOid} and feed URL {FeedUrl}",
                LogSanitizer.Sanitize(config.CreatedByEntraOid),
                LogSanitizer.Sanitize(config.FeedUrl));
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
            var existing = await broadcastingContext.UserCollectorFeedSources
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.CreatedByEntraOid == ownerOid,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserCollectorFeedSources.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete feed source config for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }
}
