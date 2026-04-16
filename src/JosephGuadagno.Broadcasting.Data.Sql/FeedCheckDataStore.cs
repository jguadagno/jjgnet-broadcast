using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class FeedCheckDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<FeedCheckDataStore> logger) : IFeedCheckDataStore
{
    public async Task<Domain.Models.FeedCheck> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.FindAsync(new object[] { primaryKey }, cancellationToken);
        return mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck);
    }

    public async Task<OperationResult<Domain.Models.FeedCheck>> SaveAsync(Domain.Models.FeedCheck entity, CancellationToken cancellationToken = default)
    {
        try
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
            if (result) return OperationResult<Domain.Models.FeedCheck>.Success(mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck));
            return OperationResult<Domain.Models.FeedCheck>.Failure("Failed to save the FeedCheck");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save feed check {FeedCheckId}", entity.Id);
            return OperationResult<Domain.Models.FeedCheck>.Failure("An error occurred while saving the FeedCheck", ex);
        }
    }

    public async Task<List<Domain.Models.FeedCheck>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbFeedChecks = await broadcastingContext.FeedChecks.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.FeedCheck>>(dbFeedChecks);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.FeedCheck entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbFeedCheck = await broadcastingContext.FeedChecks.FindAsync(new object[] { primaryKey }, cancellationToken);
            if (dbFeedCheck == null) return OperationResult<bool>.Success(true);

            broadcastingContext.FeedChecks.Remove(dbFeedCheck);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete feed check {FeedCheckId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the FeedCheck", ex);
        }
    }

    public async Task<Domain.Models.FeedCheck?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var dbFeedCheck = await broadcastingContext.FeedChecks.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        return dbFeedCheck is null ? null : mapper.Map<Domain.Models.FeedCheck>(dbFeedCheck);
    }
}