using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class SyndicationFeedSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper)
    : ISyndicationFeedSourceDataStore
{
    public async Task<Domain.Models.SyndicationFeedSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.FindAsync(new object[] { primaryKey }, cancellationToken);
        return mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<OperationResult<Domain.Models.SyndicationFeedSource>> SaveAsync(Domain.Models.SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbSyndicationFeedSource = mapper.Map<Models.SyndicationFeedSource>(entity);
            broadcastingContext.Entry(dbSyndicationFeedSource).State =
                dbSyndicationFeedSource.Id == 0 ? EntityState.Added : EntityState.Modified;

            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return OperationResult<Domain.Models.SyndicationFeedSource>.Success(mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource));
            }
            return OperationResult<Domain.Models.SyndicationFeedSource>.Failure("Failed to save syndication feed source");
        }
        catch (Exception ex)
        {
            return OperationResult<Domain.Models.SyndicationFeedSource>.Failure("An error occurred while saving the syndication feed source", ex);
        }
    }

    public async Task<List<Domain.Models.SyndicationFeedSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSources = await broadcastingContext.SyndicationFeedSources.ToListAsync(cancellationToken);
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
            var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.FindAsync(new object[] { primaryKey }, cancellationToken);
            if (dbSyndicationFeedSource == null) return OperationResult<bool>.Success(true);

            broadcastingContext.SyndicationFeedSources.Remove(dbSyndicationFeedSource);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure("An error occurred while deleting the syndication feed source", ex);
        }
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.AsNoTracking()
            .FirstOrDefaultAsync(s => s.FeedIdentifier == feedIdentifier, cancellationToken);
        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Url == url, cancellationToken);
        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.SyndicationFeedSources.AsNoTracking()
            .Where(s => s.PublicationDate >= cutoffDate || s.ItemLastUpdatedOn >= cutoffDate);
        foreach (var excludedCategory in excludedCategories)
        {
            query = query.Where(s => s.Tags == null || !s.Tags.Contains(excludedCategory));
        }

        var dbSyndicationFeedSource = await query.OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync(cancellationToken);

        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }
}