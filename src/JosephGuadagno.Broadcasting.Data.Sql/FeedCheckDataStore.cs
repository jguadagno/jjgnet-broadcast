using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class FeedCheckDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IFeedCheckDataStore
{
    public async Task<Domain.Models.FeedCheck> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.FindAsync(new object[] { primaryKey }, cancellationToken);
        return mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck);
    }

    public async Task<Domain.Models.FeedCheck> SaveAsync(Domain.Models.FeedCheck entity, CancellationToken cancellationToken = default)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.FirstOrDefaultAsync(c => c.Id == entity.Id, cancellationToken);
        if (dbFeedCheck == null)
        {
            dbFeedCheck = mapper.Map<Models.FeedCheck>(entity);
            broadcastingContext.FeedChecks.Add(dbFeedCheck);
        }
        else
        {
            dbFeedCheck.Name = entity.Name;
            dbFeedCheck.LastUpdatedOn = entity.LastUpdatedOn;
            dbFeedCheck.LastCheckedFeed = entity.LastCheckedFeed;
            dbFeedCheck.LastItemAddedOrUpdated = entity.LastItemAddedOrUpdated;
        }

        var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
        return result ? mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck) : throw new ApplicationException("Failed to save the FeedCheck");
    }

    public async Task<List<Domain.Models.FeedCheck>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbFeedChecks = await broadcastingContext.FeedChecks.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.FeedCheck>>(dbFeedChecks);
    }

    public async Task<bool> DeleteAsync(Domain.Models.FeedCheck entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.FindAsync(new object[] { primaryKey }, cancellationToken);
        if (dbFeedCheck == null)
        {
            return true;
        }

        broadcastingContext.FeedChecks.Remove(dbFeedCheck);
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
    }

    public async Task<Domain.Models.FeedCheck?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        return dbFeedCheck is null ? null : mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck);
    }
}