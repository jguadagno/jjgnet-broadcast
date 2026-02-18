using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class SyndicationFeedSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper)
    : ISyndicationFeedSourceDataStore
{
    public async Task<Domain.Models.SyndicationFeedSource> GetAsync(int primaryKey)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.FindAsync(primaryKey);
        return mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<Domain.Models.SyndicationFeedSource> SaveAsync(Domain.Models.SyndicationFeedSource entity)
    {
        var dbSyndicationFeedSource = mapper.Map<Models.SyndicationFeedSource>(entity);
        broadcastingContext.Entry(dbSyndicationFeedSource).State =
            dbSyndicationFeedSource.Id == 0 ? EntityState.Added : EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
        }

        throw new ApplicationException("Failed to save syndication feed source");
    }

    public async Task<List<Domain.Models.SyndicationFeedSource>> GetAllAsync()
    {
        var dbSyndicationFeedSources = await broadcastingContext.SyndicationFeedSources.ToListAsync();
        return mapper.Map<List<Domain.Models.SyndicationFeedSource>>(dbSyndicationFeedSources);
    }

    public async Task<bool> DeleteAsync(Domain.Models.SyndicationFeedSource entity)
    {
        return await DeleteAsync(entity.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.FindAsync(primaryKey);
        if (dbSyndicationFeedSource == null)
        {
            return true;
        }

        broadcastingContext.SyndicationFeedSources.Remove(dbSyndicationFeedSource);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetByUrlAsync(string url)
    {
        var dbSyndicationFeedSource = await broadcastingContext.SyndicationFeedSources.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Url == url);
        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }

    public async Task<Domain.Models.SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories)
    {
        var query = broadcastingContext.SyndicationFeedSources.AsNoTracking()
            .Where(s => s.PublicationDate >= cutoffDate || s.ItemLastUpdatedOn >= cutoffDate);
        foreach (var excludedCategory in excludedCategories)
        {
            query = query.Where(s => s.Tags == null || !s.Tags.Contains(excludedCategory));
        }

        var dbSyndicationFeedSource = await query.OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync();

        return dbSyndicationFeedSource is null ? null : mapper.Map<Domain.Models.SyndicationFeedSource>(dbSyndicationFeedSource);
    }
}