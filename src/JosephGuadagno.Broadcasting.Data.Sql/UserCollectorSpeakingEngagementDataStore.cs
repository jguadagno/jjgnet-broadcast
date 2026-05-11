using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

/// <summary>
/// SQL data store for per-user speaking engagements file collector configurations
/// </summary>
public class UserCollectorSpeakingEngagementDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserCollectorSpeakingEngagementDataStore> logger) : IUserCollectorSpeakingEngagementDataStore
{
    /// <inheritdoc />
    public async Task<List<UserCollectorSpeakingEngagement>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entities = await broadcastingContext.UserCollectorSpeakingEngagements
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorSpeakingEngagement>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorSpeakingEngagement?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        var entity = await broadcastingContext.UserCollectorSpeakingEngagements
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null ? null : mapper.Map<UserCollectorSpeakingEngagement>(entity);
    }

    /// <inheritdoc />
    public async Task<List<UserCollectorSpeakingEngagement>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await broadcastingContext.UserCollectorSpeakingEngagements
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CreatedByEntraOid)
            .ThenBy(c => c.DisplayName)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<UserCollectorSpeakingEngagement>>(entities);
    }

    /// <inheritdoc />
    public async Task<UserCollectorSpeakingEngagement?> SaveAsync(
        UserCollectorSpeakingEngagement config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.SpeakingEngagementsFile);

        try
        {
            var existing = await broadcastingContext.UserCollectorSpeakingEngagements
                .FirstOrDefaultAsync(
                    c => c.CreatedByEntraOid == config.CreatedByEntraOid && c.SpeakingEngagementsFile == config.SpeakingEngagementsFile,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserCollectorSpeakingEngagement
                {
                    CreatedByEntraOid = config.CreatedByEntraOid,
                    SpeakingEngagementsFile = config.SpeakingEngagementsFile,
                    CreatedOn = DateTimeOffset.UtcNow
                };
                broadcastingContext.UserCollectorSpeakingEngagements.Add(existing);
            }

            existing.DisplayName = config.DisplayName;
            existing.IsActive = config.IsActive;
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return mapper.Map<UserCollectorSpeakingEngagement>(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save speaking engagement config for owner {OwnerOid} and file URL {SpeakingEngagementsFile}",
                LogSanitizer.Sanitize(config.CreatedByEntraOid),
                LogSanitizer.Sanitize(config.SpeakingEngagementsFile));
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
            var existing = await broadcastingContext.UserCollectorSpeakingEngagements
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.CreatedByEntraOid == ownerOid,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserCollectorSpeakingEngagements.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete speaking engagement config for ID {Id} and owner {OwnerOid}",
                id,
                LogSanitizer.Sanitize(ownerOid));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Domain.Models.PagedResult<UserCollectorSpeakingEngagement>> GetAllAsync(
        string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        IQueryable<Models.UserCollectorSpeakingEngagement> query = broadcastingContext.UserCollectorSpeakingEngagements
            .AsNoTracking()
            .Where(c => c.CreatedByEntraOid == ownerOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(c => c.DisplayName.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.UserCollectorSpeakingEngagement.SpeakingEngagementsFile).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(c => c.SpeakingEngagementsFile) : query.OrderBy(c => c.SpeakingEngagementsFile);
        }
        else if (sortByLower == nameof(Models.UserCollectorSpeakingEngagement.CreatedOn).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(c => c.CreatedOn) : query.OrderBy(c => c.CreatedOn);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(c => c.DisplayName) : query.OrderBy(c => c.DisplayName);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<UserCollectorSpeakingEngagement>
        {
            Items = mapper.Map<List<UserCollectorSpeakingEngagement>>(entities),
            TotalCount = totalCount
        };
    }
}
