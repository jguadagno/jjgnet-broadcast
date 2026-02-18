using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class FeedCheckDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IFeedCheckDataStore
{
    public async Task<Domain.Models.FeedCheck> GetAsync(int primaryKey)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.FindAsync(primaryKey);
        return mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck);
    }

    public async Task<Domain.Models.FeedCheck> SaveAsync(Domain.Models.FeedCheck entity)
    {
        var dbFeedCheck = broadcastingContext.FeedChecks.FirstOrDefault(c => c.Id == entity.Id);
        if (dbFeedCheck == null)
        {
            dbFeedCheck = mapper.Map<Models.FeedCheck>(entity);
        }
        else
        {
            dbFeedCheck.Name = entity.Name;
            dbFeedCheck.LastUpdatedOn = entity.LastUpdatedOn;
            dbFeedCheck.LastCheckedFeed = entity.LastCheckedFeed;
            dbFeedCheck.LastItemAddedOrUpdated = entity.LastItemAddedOrUpdated;
        }

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        return result ? mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck) : throw new ApplicationException("Failed to save the FeedCheck");
    }

    public async Task<List<Domain.Models.FeedCheck>> GetAllAsync()
    {
        var dbFeedChecks = await broadcastingContext.FeedChecks.ToListAsync();
        return mapper.Map<List<Domain.Models.FeedCheck>>(dbFeedChecks);
    }

    public async Task<bool> DeleteAsync(Domain.Models.FeedCheck entity)
    {
        return await DeleteAsync(entity.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.FindAsync(primaryKey);
        if (dbFeedCheck == null)
        {
            return true;
        }

        broadcastingContext.FeedChecks.Remove(dbFeedCheck);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Domain.Models.FeedCheck?> GetByNameAsync(string name)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name);
        return dbFeedCheck is null ? null : mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck);
    }
}