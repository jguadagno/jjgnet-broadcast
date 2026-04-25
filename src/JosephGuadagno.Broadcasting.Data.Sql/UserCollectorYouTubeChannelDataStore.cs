using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user YouTube channel collector configurations
/// </summary>
public class UserCollectorYouTubeChannelDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserCollectorYouTubeChannelDataStore> logger) : IUserCollectorYouTubeChannelDataStore
{
    /// <inheritdoc />
    public async Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entities = await broadcastingContext.UserCollectorYouTubeChannels
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorYouTubeChannel>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorYouTubeChannel?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var entity = await broadcastingContext.UserCollectorYouTubeChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<UserCollectorYouTubeChannel>(entity);
    }

    /// <inheritdoc />
    public async Task<List<UserCollectorYouTubeChannel>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserCollectorYouTubeChannels
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CreatedByEntraOid)
            .ThenBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorYouTubeChannel>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorYouTubeChannel?> SaveAsync(
        UserCollectorYouTubeChannel config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.ChannelId);

        try
        {
            var existing = await broadcastingContext.UserCollectorYouTubeChannels
                .FirstOrDefaultAsync(
                    c => c.CreatedByEntraOid == config.CreatedByEntraOid && c.ChannelId == config.ChannelId,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserCollectorYouTubeChannel
                {
                    CreatedByEntraOid = config.CreatedByEntraOid,
                    ChannelId = config.ChannelId,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserCollectorYouTubeChannels.Add(existing);
            }

            existing.DisplayName = config.DisplayName;
            existing.IsActive = config.IsActive;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserCollectorYouTubeChannel>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save YouTube channel config for owner {OwnerOid} and channel ID {ChannelId}",
                LogSanitizer.Sanitize(config.CreatedByEntraOid),
                LogSanitizer.Sanitize(config.ChannelId));
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
            var existing = await broadcastingContext.UserCollectorYouTubeChannels
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.CreatedByEntraOid == ownerOid,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserCollectorYouTubeChannels.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete YouTube channel config for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Domain.Models.PagedResult<UserCollectorYouTubeChannel>> GetAllAsync(
        string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        IQueryable<Models.UserCollectorYouTubeChannel> query = broadcastingContext.UserCollectorYouTubeChannels
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(c => c.DisplayName.ToLower().Contains(lowerFilter));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "channelid" => sortDescending ? query.OrderByDescending(c => c.ChannelId) : query.OrderBy(c => c.ChannelId),
            "createdon" => sortDescending ? query.OrderByDescending(c => c.CreatedOn) : query.OrderBy(c => c.CreatedOn),
            _ => sortDescending ? query.OrderByDescending(c => c.DisplayName) : query.OrderBy(c => c.DisplayName),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<UserCollectorYouTubeChannel>
        {
            Items = mapper.Map<List<UserCollectorYouTubeChannel>>(entities),
            TotalCount = totalCount
        };
    }
}
