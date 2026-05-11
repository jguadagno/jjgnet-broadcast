using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user scheduled item publisher configurations
/// </summary>
public class UserCollectorScheduledItemDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserCollectorScheduledItemDataStore> logger) : IUserCollectorScheduledItemDataStore
{
    /// <inheritdoc />
    public async Task<List<UserCollectorScheduledItem>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entities = await broadcastingContext.UserCollectorScheduledItems
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorScheduledItem>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorScheduledItem?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var entity = await broadcastingContext.UserCollectorScheduledItems
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<UserCollectorScheduledItem>(entity);
    }

    /// <inheritdoc />
    public async Task<List<UserCollectorScheduledItem>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserCollectorScheduledItems
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CreatedByEntraOid)
            .ThenBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorScheduledItem>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorScheduledItem?> SaveAsync(
        UserCollectorScheduledItem config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);

        try
        {
            // One config per user — unique constraint is on CreatedByEntraOid alone
            var existing = await broadcastingContext.UserCollectorScheduledItems
                .FirstOrDefaultAsync(
                    c => c.CreatedByEntraOid == config.CreatedByEntraOid,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserCollectorScheduledItem
                {
                    CreatedByEntraOid = config.CreatedByEntraOid,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserCollectorScheduledItems.Add(existing);
            }

            existing.DisplayName = config.DisplayName;
            existing.IsActive = config.IsActive;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserCollectorScheduledItem>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save scheduled item config for owner {OwnerOid}",
                LogSanitizer.Sanitize(config.CreatedByEntraOid));
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
            var existing = await broadcastingContext.UserCollectorScheduledItems
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.CreatedByEntraOid == ownerOid,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserCollectorScheduledItems.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete scheduled item config for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Domain.Models.PagedResult<UserCollectorScheduledItem>> GetAllAsync(
        string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        IQueryable<Models.UserCollectorScheduledItem> query = broadcastingContext.UserCollectorScheduledItems
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(c => c.DisplayName.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.UserCollectorScheduledItem.CreatedOn).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(c => c.CreatedOn) : query.OrderBy(c => c.CreatedOn);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(c => c.DisplayName) : query.OrderBy(c => c.DisplayName);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<UserCollectorScheduledItem>
        {
            Items = mapper.Map<List<UserCollectorScheduledItem>>(entities),
            TotalCount = totalCount
        };
    }
}
