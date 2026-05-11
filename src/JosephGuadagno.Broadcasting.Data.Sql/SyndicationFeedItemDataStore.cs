using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class SyndicationFeedItemDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<SyndicationFeedItemDataStore> logger)
    : ISyndicationFeedItemDataStore
{
    private const string SourceType = "SyndicationFeed";

    public async Task<SyndicationFeedItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedItem = await broadcastingContext.SyndicationFeedItems
            .FirstOrDefaultAsync(s => s.Id == primaryKey, cancellationToken);
        
        if (dbSyndicationFeedItem is not null)
        {
            dbSyndicationFeedItem.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<SyndicationFeedItem>(dbSyndicationFeedItem);
    }

    public async Task<OperationResult<SyndicationFeedItem>> SaveAsync(SyndicationFeedItem entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceId = await broadcastingContext.ExecuteInTransactionIfSupportedAsync(async ct =>
            {
                var dbSyndicationFeedItem = mapper.Map<Models.SyndicationFeedItem>(entity);
                broadcastingContext.Entry(dbSyndicationFeedItem).State =
                    dbSyndicationFeedItem.Id == 0 ? EntityState.Added : EntityState.Modified;

                await broadcastingContext.SaveChangesAsync(ct);
                await SyncSourceTagsAsync(dbSyndicationFeedItem.Id, entity.Tags, ct);

                return dbSyndicationFeedItem.Id;
            }, cancellationToken);

            var saved = await broadcastingContext.SyndicationFeedItems
                .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

            if (saved is not null)
            {
                saved.SourceTags = await broadcastingContext.SourceTags
                    .Where(st => st.SourceId == saved.Id && st.SourceType == SourceType)
                    .ToListAsync(cancellationToken);
            }

            return saved is not null
                ? OperationResult<SyndicationFeedItem>.Success(mapper.Map<SyndicationFeedItem>(saved))
                : OperationResult<SyndicationFeedItem>.Failure("Failed to save syndication feed source");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save syndication feed source {FeedId}", entity.Id);
            return OperationResult<SyndicationFeedItem>.Failure("An error occurred while saving the syndication feed source", ex);
        }
    }

    public async Task<List<SyndicationFeedItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedItems = await broadcastingContext.SyndicationFeedItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ids = dbSyndicationFeedItems.Select(s => s.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var source in dbSyndicationFeedItems)
        {
            source.SourceTags = tagsBySourceId.TryGetValue(source.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return mapper.Map<List<SyndicationFeedItem>>(dbSyndicationFeedItems);
    }

    public async Task<List<SyndicationFeedItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedItems = await broadcastingContext.SyndicationFeedItems
            .AsNoTracking()
            .Where(s => s.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);

        var ids = dbSyndicationFeedItems.Select(s => s.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var source in dbSyndicationFeedItems)
        {
            source.SourceTags = tagsBySourceId.TryGetValue(source.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return mapper.Map<List<SyndicationFeedItem>>(dbSyndicationFeedItems);
    }

    public async Task<OperationResult<bool>> DeleteAsync(SyndicationFeedItem entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbSyndicationFeedItem = await broadcastingContext.SyndicationFeedItems
                .FirstOrDefaultAsync(s => s.Id == primaryKey, cancellationToken);
            if (dbSyndicationFeedItem == null) return OperationResult<bool>.Success(true);

            var sourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
            
            broadcastingContext.SourceTags.RemoveRange(sourceTags);
            broadcastingContext.SyndicationFeedItems.Remove(dbSyndicationFeedItem);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete syndication feed source {FeedId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the syndication feed source", ex);
        }
    }

    public async Task<SyndicationFeedItem?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedItem = await broadcastingContext.SyndicationFeedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.FeedIdentifier == feedIdentifier, cancellationToken);
        
        if (dbSyndicationFeedItem is not null)
        {
            dbSyndicationFeedItem.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbSyndicationFeedItem.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbSyndicationFeedItem is null ? null : mapper.Map<SyndicationFeedItem>(dbSyndicationFeedItem);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        var ownerOid = await broadcastingContext.SyndicationFeedItems
            .AsNoTracking()
            .Where(source => source.CreatedByEntraOid != string.Empty)
            .OrderByDescending(source => source.LastUpdatedOn)
            .Select(source => source.CreatedByEntraOid)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(ownerOid) ? null : ownerOid;
    }

    public async Task<SyndicationFeedItem?> GetRandomSyndicationDataAsync(string ownerEntraOid, DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.SyndicationFeedItems
            .AsNoTracking()
            .Where(s => s.CreatedByEntraOid == ownerEntraOid)
            .Where(s => s.PublicationDate >= cutoffDate || s.ItemLastUpdatedOn >= cutoffDate);

        if (excludedCategories.Count > 0)
        {
            var excludedSourceIds = await broadcastingContext.SourceTags
                .Where(st => st.SourceType == SourceType && excludedCategories.Contains(st.Tag))
                .Select(st => st.SourceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(s => !excludedSourceIds.Contains(s.Id));
        }

        var dbSyndicationFeedItem = await query.OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync(cancellationToken);

        if (dbSyndicationFeedItem is not null)
        {
            dbSyndicationFeedItem.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbSyndicationFeedItem.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }

        return dbSyndicationFeedItem is null ? null : mapper.Map<SyndicationFeedItem>(dbSyndicationFeedItem);
    }

    private async Task SyncSourceTagsAsync(int sourceId, IList<string> tags, CancellationToken cancellationToken)
    {
        var existing = await broadcastingContext.SourceTags
            .Where(st => st.SourceId == sourceId && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        broadcastingContext.SourceTags.RemoveRange(existing);

        if (tags.Count > 0)
        {
            var newTags = tags.Select(tag => new Models.SourceTag
            {
                SourceId = sourceId,
                SourceType = SourceType,
                Tag = tag
            });
            broadcastingContext.SourceTags.AddRange(newTags);
        }

        await broadcastingContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<SyndicationFeedItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.SyndicationFeedItem> query = broadcastingContext.SyndicationFeedItems
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(s => s.Title.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy.ToLowerInvariant();
        if (sortByLower == nameof(Models.SyndicationFeedItem.Author).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(s => s.Author) : query.OrderBy(s => s.Author);
        }
        else if (sortByLower == nameof(Models.SyndicationFeedItem.PublicationDate).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(s => s.PublicationDate) : query.OrderBy(s => s.PublicationDate);
        }
        else if (sortByLower == nameof(Models.SyndicationFeedItem.AddedOn).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(s => s.AddedOn) : query.OrderBy(s => s.AddedOn);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(s => s.Title) : query.OrderBy(s => s.Title);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var ids = dbItems.Select(s => s.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var item in dbItems)
        {
            item.SourceTags = tagsBySourceId.TryGetValue(item.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return new PagedResult<SyndicationFeedItem>
        {
            Items = mapper.Map<List<SyndicationFeedItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<SyndicationFeedItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.SyndicationFeedItem> query = broadcastingContext.SyndicationFeedItems
            .AsNoTracking()
            .Where(s => s.CreatedByEntraOid == ownerEntraOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(s => s.Title.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy.ToLowerInvariant();
        if (sortByLower == nameof(Models.SyndicationFeedItem.Author).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(s => s.Author) : query.OrderBy(s => s.Author);
        }
        else if (sortByLower == nameof(Models.SyndicationFeedItem.PublicationDate).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(s => s.PublicationDate) : query.OrderBy(s => s.PublicationDate);
        }
        else if (sortByLower == nameof(Models.SyndicationFeedItem.AddedOn).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(s => s.AddedOn) : query.OrderBy(s => s.AddedOn);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(s => s.Title) : query.OrderBy(s => s.Title);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var ids = dbItems.Select(s => s.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var item in dbItems)
        {
            item.SourceTags = tagsBySourceId.TryGetValue(item.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return new PagedResult<SyndicationFeedItem>
        {
            Items = mapper.Map<List<SyndicationFeedItem>>(dbItems),
            TotalCount = totalCount
        };
    }
}
