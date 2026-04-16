using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EngagementDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<EngagementDataStore> logger) : IEngagementDataStore
{
    public async Task<Domain.Models.Engagement> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbEngagement =
            await broadcastingContext.Engagements.Include(e => e.Talks)
                .FirstOrDefaultAsync(e => e.Id == primaryKey, cancellationToken);
        return mapper.Map<Domain.Models.Engagement>(dbEngagement);
    }

    public async Task<OperationResult<Domain.Models.Engagement>> SaveAsync(Domain.Models.Engagement engagement, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbEngagement = mapper.Map<Models.Engagement>(engagement);
            broadcastingContext.Entry(dbEngagement).State =
                dbEngagement.Id == 0 ? EntityState.Added : EntityState.Modified;

            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return OperationResult<Domain.Models.Engagement>.Success(mapper.Map<Domain.Models.Engagement>(dbEngagement));
            }
            return OperationResult<Domain.Models.Engagement>.Failure("Failed to save engagement");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save engagement {EngagementId}", engagement.Id);
            return OperationResult<Domain.Models.Engagement>.Failure("An error occurred while saving the engagement", ex);
        }
    }

    public async Task<List<Domain.Models.Engagement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbEngagements = await broadcastingContext.Engagements.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.Engagement>>(dbEngagements);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.Engagement engagement, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(engagement.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbEngagement = await broadcastingContext.Engagements
                .Include(e => e.Talks)
                .FirstOrDefaultAsync(e => e.Id == primaryKey, cancellationToken);

            if (dbEngagement == null) return OperationResult<bool>.Success(true);

            foreach (var talk in dbEngagement.Talks)
            {
                broadcastingContext.Talks.Remove(talk);
            }
            broadcastingContext.Engagements.Remove(dbEngagement);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete engagement {EngagementId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the engagement", ex);
        }
    }

    public async Task<List<Domain.Models.Talk>> GetTalksForEngagementAsync(int engagementId, CancellationToken cancellationToken = default)
    {
        var talks = await broadcastingContext.Talks.Where(e => e.EngagementId == engagementId).ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.Talk>>(talks);
    }

    public async Task<bool> AddTalkToEngagementAsync(Domain.Models.Engagement engagement, Domain.Models.Talk talk, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(engagement);
        ArgumentNullException.ThrowIfNull(talk);

        Models.Engagement? dbEngagement;
        if (engagement.Id == 0)
        {
            dbEngagement = mapper.Map<Models.Engagement>(engagement);
            broadcastingContext.Engagements.Add(dbEngagement);
        }
        else
        {
            dbEngagement = await broadcastingContext.Engagements.FindAsync(new object[] { engagement.Id }, cancellationToken);
            if (dbEngagement is null)
            {
                return false;
            }
        }
        
        // Save Talk
        var dbTalk = mapper.Map<Models.Talk>(talk);
        dbEngagement.Talks.Add(dbTalk);
        
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
    }

    public async Task<bool> AddTalkToEngagementAsync(int engagementId, Domain.Models.Talk talk, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(talk);
        if (engagementId <= 0)
        {
            throw new ApplicationException("EngagementId can not <= 0");
        }
        
        var dbEngagement = await broadcastingContext.Engagements.FindAsync(new object[] { engagementId }, cancellationToken);
        if (dbEngagement is null)
        {
            return false;
        }
        
        var dbTalk = mapper.Map<Models.Talk>(talk);
        dbEngagement.Talks.Add(dbTalk);
        
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
    }

    public async Task<OperationResult<Domain.Models.Talk>> SaveTalkAsync(Domain.Models.Talk talk, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(talk);

            var dbTalk = await broadcastingContext.Talks.FirstOrDefaultAsync(t => t.Id == talk.Id, cancellationToken) ?? new Models.Talk();
        
            if (talk.Id == 0)
            {
                dbTalk = mapper.Map<Models.Talk>(talk);
                broadcastingContext.Talks.Add(dbTalk);
            }
            else
            {
                broadcastingContext.Entry(dbTalk).CurrentValues.SetValues(talk);
            }
        
            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return OperationResult<Domain.Models.Talk>.Success(mapper.Map<Domain.Models.Talk>(dbTalk));
            }
            return OperationResult<Domain.Models.Talk>.Failure("Failed to save talk");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save talk {TalkId} for engagement {EngagementId}", talk?.Id, talk?.EngagementId);
            return OperationResult<Domain.Models.Talk>.Failure("An error occurred while saving the talk", ex);
        }
    }

    public async Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(int talkId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (talkId <= 0) return OperationResult<bool>.Failure("The TalkId can not be <=0");

            var dbTalk = await broadcastingContext.Talks.FindAsync(new object[] { talkId }, cancellationToken);
            if (dbTalk is null) return OperationResult<bool>.Success(true);

            broadcastingContext.Talks.Remove(dbTalk);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove talk {TalkId} from engagement", talkId);
            return OperationResult<bool>.Failure("An error occurred while removing the talk", ex);
        }
    }

    public async Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(Domain.Models.Talk talk, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(talk);
            var dbTalk = mapper.Map<Models.Talk>(talk);
            broadcastingContext.Talks.Remove(dbTalk);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove talk {TalkId} from engagement", talk?.Id);
            return OperationResult<bool>.Failure("An error occurred while removing the talk", ex);
        }
    }

    public async Task<Domain.Models.Talk?> GetTalkAsync(int talkId, CancellationToken cancellationToken = default)
    {
        if (talkId <= 0)
        {
            throw new ApplicationException("The TalkId can not be <=0");
        }

        var dbTalk = await broadcastingContext.Talks.FindAsync(new object[] { talkId }, cancellationToken);
        return dbTalk is null ? null : mapper.Map<Domain.Models.Talk>(dbTalk);
    }

    public async Task<Domain.Models.Engagement?> GetByNameAndUrlAndYearAsync(string name, string url, int year, CancellationToken cancellationToken = default)
    {
        var dbEngagement = await broadcastingContext.Engagements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == name && e.Url == url && e.StartDateTime.Year == year, cancellationToken);
        return dbEngagement is null ? null : mapper.Map<Domain.Models.Engagement>(dbEngagement);
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.Engagement>> GetAllAsync(int page, int pageSize, string sortBy = "startdate", bool sortDescending = true, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.Engagement> query = broadcastingContext.Engagements;
        
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(e => e.Name.ToLower().Contains(lowerFilter));
        }
        
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => sortDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "enddate" => sortDescending ? query.OrderByDescending(e => e.EndDateTime) : query.OrderBy(e => e.EndDateTime),
            _ => sortDescending ? query.OrderByDescending(e => e.StartDateTime) : query.OrderBy(e => e.StartDateTime),
        };
        
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.Engagement>
        {
            Items = mapper.Map<List<Domain.Models.Engagement>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.Talk>> GetTalksForEngagementAsync(int engagementId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.Talks.Where(e => e.EngagementId == engagementId);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.Talk>
        {
            Items = mapper.Map<List<Domain.Models.Talk>>(dbItems),
            TotalCount = totalCount
        };
    }
}