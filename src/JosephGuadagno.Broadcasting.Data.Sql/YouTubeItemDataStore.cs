using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class YouTubeItemDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<YouTubeItemDataStore> logger) : IYouTubeItemDataStore
{
    private const string SourceType = "YouTube";

    public async Task<Domain.Models.YouTubeItem> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbYouTubeItem = await broadcastingContext.YouTubeItems
            .FirstOrDefaultAsync(y => y.Id == primaryKey, cancellationToken);
        
        if (dbYouTubeItem is not null)
        {
            dbYouTubeItem.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<Domain.Models.YouTubeItem>(dbYouTubeItem);
    }

    public async Task<OperationResult<Domain.Models.YouTubeItem>> SaveAsync(Domain.Models.YouTubeItem entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceId = await broadcastingContext.ExecuteInTransactionIfSupportedAsync(async ct =>
            {
                var dbYouTubeItem = mapper.Map<Models.YouTubeItem>(entity);
                broadcastingContext.Entry(dbYouTubeItem).State =
                    dbYouTubeItem.Id == 0 ? EntityState.Added : EntityState.Modified;

                await broadcastingContext.SaveChangesAsync(ct);
                await SyncSourceTagsAsync(dbYouTubeItem.Id, entity.Tags, ct);

                return dbYouTubeItem.Id;
            }, cancellationToken);

            var saved = await broadcastingContext.YouTubeItems
                .FirstOrDefaultAsync(y => y.Id == sourceId, cancellationToken);

            if (saved is not null)
            {
                saved.SourceTags = await broadcastingContext.SourceTags
                    .Where(st => st.SourceId == saved.Id && st.SourceType == SourceType)
                    .ToListAsync(cancellationToken);
            }

            return saved is not null
                ? OperationResult<Domain.Models.YouTubeItem>.Success(mapper.Map<Domain.Models.YouTubeItem>(saved))
                : OperationResult<Domain.Models.YouTubeItem>.Failure("Failed to save YouTube source");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save YouTube source {SourceId}", entity.Id);
            return OperationResult<Domain.Models.YouTubeItem>.Failure("An error occurred while saving the YouTube source", ex);
        }
    }

    public async Task<List<Domain.Models.YouTubeItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbYouTubeItems = await broadcastingContext.YouTubeItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ids = dbYouTubeItems.Select(y => y.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var source in dbYouTubeItems)
        {
            source.SourceTags = tagsBySourceId.TryGetValue(source.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return mapper.Map<List<Domain.Models.YouTubeItem>>(dbYouTubeItems);
    }

    public async Task<List<Domain.Models.YouTubeItem>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbYouTubeItems = await broadcastingContext.YouTubeItems
            .AsNoTracking()
            .Where(y => y.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);

        var ids = dbYouTubeItems.Select(y => y.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var source in dbYouTubeItems)
        {
            source.SourceTags = tagsBySourceId.TryGetValue(source.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return mapper.Map<List<Domain.Models.YouTubeItem>>(dbYouTubeItems);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.YouTubeItem entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbYouTubeItem = await broadcastingContext.YouTubeItems
                .FirstOrDefaultAsync(y => y.Id == primaryKey, cancellationToken);
            if (dbYouTubeItem == null) return OperationResult<bool>.Success(true);

            var sourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
            
            broadcastingContext.SourceTags.RemoveRange(sourceTags);
            broadcastingContext.YouTubeItems.Remove(dbYouTubeItem);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete YouTube source {SourceId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the YouTube source", ex);
        }
    }

    public async Task<Domain.Models.YouTubeItem?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var dbYouTubeItem = await broadcastingContext.YouTubeItems
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Url == url, cancellationToken);
        
        if (dbYouTubeItem is not null)
        {
            dbYouTubeItem.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbYouTubeItem.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbYouTubeItem is null ? null : mapper.Map<Domain.Models.YouTubeItem>(dbYouTubeItem);
    }

    public async Task<Domain.Models.YouTubeItem?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var dbYouTubeItem = await broadcastingContext.YouTubeItems
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.VideoId == videoId, cancellationToken);
        
        if (dbYouTubeItem is not null)
        {
            dbYouTubeItem.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbYouTubeItem.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbYouTubeItem is null ? null : mapper.Map<Domain.Models.YouTubeItem>(dbYouTubeItem);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        var ownerOid = await broadcastingContext.YouTubeItems
            .AsNoTracking()
            .Where(source => source.CreatedByEntraOid != string.Empty)
            .OrderByDescending(source => source.LastUpdatedOn)
            .Select(source => source.CreatedByEntraOid)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(ownerOid) ? null : ownerOid;
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

    public async Task<Domain.Models.PagedResult<Domain.Models.YouTubeItem>> GetAllAsync(int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.YouTubeItem> query = broadcastingContext.YouTubeItems
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(y => y.Title.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.YouTubeItem.Author).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(y => y.Author) : query.OrderBy(y => y.Author);
        }
        else if (sortByLower == nameof(Models.YouTubeItem.PublicationDate).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(y => y.PublicationDate) : query.OrderBy(y => y.PublicationDate);
        }
        else if (sortByLower == nameof(Models.YouTubeItem.AddedOn).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(y => y.AddedOn) : query.OrderBy(y => y.AddedOn);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(y => y.Title) : query.OrderBy(y => y.Title);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var ids = dbItems.Select(y => y.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var item in dbItems)
        {
            item.SourceTags = tagsBySourceId.TryGetValue(item.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return new Domain.Models.PagedResult<Domain.Models.YouTubeItem>
        {
            Items = mapper.Map<List<Domain.Models.YouTubeItem>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.YouTubeItem>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "title", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.YouTubeItem> query = broadcastingContext.YouTubeItems
            .AsNoTracking()
            .Where(y => y.CreatedByEntraOid == ownerEntraOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(y => y.Title.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.YouTubeItem.Author).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(y => y.Author) : query.OrderBy(y => y.Author);
        }
        else if (sortByLower == nameof(Models.YouTubeItem.PublicationDate).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(y => y.PublicationDate) : query.OrderBy(y => y.PublicationDate);
        }
        else if (sortByLower == nameof(Models.YouTubeItem.AddedOn).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(y => y.AddedOn) : query.OrderBy(y => y.AddedOn);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(y => y.Title) : query.OrderBy(y => y.Title);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var ids = dbItems.Select(y => y.Id).ToList();
        var allTags = await broadcastingContext.SourceTags
            .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var item in dbItems)
        {
            item.SourceTags = tagsBySourceId.TryGetValue(item.Id, out var tags) ? tags : new List<Models.SourceTag>();
        }

        return new Domain.Models.PagedResult<Domain.Models.YouTubeItem>
        {
            Items = mapper.Map<List<Domain.Models.YouTubeItem>>(dbItems),
            TotalCount = totalCount
        };
    }
}
