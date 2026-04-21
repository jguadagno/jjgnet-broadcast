using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class SyndicationFeedSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<SyndicationFeedSourceDataStore> logger)
    : ISyndicationFeedSourceDataStore
{
    private const string SourceType = "SyndicationFeed";

    public async Task<Domain.Models.SyndicationFeedSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources
            .FirstOrDefaultAsync(s => s.Id == primaryKey, cancellationToken);
        
        if (dbSyndicationFeedSource is not null)
        {
            dbSyndicationFeedSource.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<OperationResult<Domain.Models.SyndicationFeedSource>> SaveAsync(Domain.Models.SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbSyndicationFeedSource = mapper.Map<Models.SyndicationFeedSource>(entity);
            broadcastingContext.Entry(dbSyndicationFeedSource).State =
                dbSyndicationFeedSource.Id == 0 ? EntityState.Added : EntityState.Modified;

            await broadcastingContext.ExecuteInTransactionIfSupportedAsync(async () =>
            {
                await broadcastingContext.SaveChangesAsync(cancellationToken);
                await SyncSourceTagsAsync(dbSyndicationFeedSource.Id, entity.Tags, cancellationToken);
            }, cancellationToken);

            var saved = await broadcastingContext.SyndicationFeedSources
                .FirstOrDefaultAsync(s => s.Id == dbSyndicationFeedSource.Id, cancellationToken);

            if (saved is not null)
            {
                saved.SourceTags = await broadcastingContext.SourceTags
                    .Where(st => st.SourceId == saved.Id && st.SourceType == SourceType)
                    .ToListAsync(cancellationToken);
            }

            return saved is not null
                ? OperationResult<Domain.Models.SyndicationFeedSource>.Success(mapper.Map<Domain.Models.SyndicationFeedSource>(saved))
                : OperationResult<Domain.Models.SyndicationFeedSource>.Failure("Failed to save syndication feed source");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save syndication feed source {FeedId}", entity.Id);
            return OperationResult<Domain.Models.SyndicationFeedSource>.Failure("An error occurred while saving the syndication feed source", ex);
        }
    }

    public async Task<List<Domain.Models.SyndicationFeedSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSources = await broadcastingContext.SyndicationFeedSources
            .ToListAsync(cancellationToken);
        
        foreach (var source in dbSyndicationFeedSources)
        {
            source.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == source.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<List<Domain.Models.SyndicationFeedSource>>(dbSyndicationFeedSources);
    }

    public async Task<List<Domain.Models.SyndicationFeedSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSources = await broadcastingContext.SyndicationFeedSources
            .Where(s => s.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);
        
        foreach (var source in dbSyndicationFeedSources)
        {
            source.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == source.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<List<Domain.Models.SyndicationFeedSource>>(dbSyndicationFeedSources);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources
                .FirstOrDefaultAsync(s => s.Id == primaryKey, cancellationToken);
            if (dbSyndicationFeedSource == null) return OperationResult<bool>.Success(true);

            var sourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
            
            broadcastingContext.SourceTags.RemoveRange(sourceTags);
            broadcastingContext.SyndicationFeedSources.Remove(dbSyndicationFeedSource);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete syndication feed source {FeedId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the syndication feed source", ex);
        }
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.FeedIdentifier == feedIdentifier, cancellationToken);
        
        if (dbSyndicationFeedSource is not null)
        {
            dbSyndicationFeedSource.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbSyndicationFeedSource.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Url == url, cancellationToken);
        
        if (dbSyndicationFeedSource is not null)
        {
            dbSyndicationFeedSource.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbSyndicationFeedSource.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        var ownerOid = await broadcastingContext.SyndicationFeedSources
            .AsNoTracking()
            .Where(source => source.CreatedByEntraOid != string.Empty)
            .OrderByDescending(source => source.LastUpdatedOn)
            .Select(source => source.CreatedByEntraOid)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(ownerOid) ? null : ownerOid;
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.SyndicationFeedSources
            .AsNoTracking()
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

        var dbSyndicationFeedSource = await query.OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync(cancellationToken);

        if (dbSyndicationFeedSource is not null)
        {
            dbSyndicationFeedSource.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbSyndicationFeedSource.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }

        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetRandomSyndicationDataAsync(string ownerEntraOid, DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.SyndicationFeedSources
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

        var dbSyndicationFeedSource = await query.OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync(cancellationToken);

        if (dbSyndicationFeedSource is not null)
        {
            dbSyndicationFeedSource.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbSyndicationFeedSource.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }

        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
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
}
