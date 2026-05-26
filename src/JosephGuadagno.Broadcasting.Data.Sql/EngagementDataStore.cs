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
            ArgumentNullException.ThrowIfNull(engagement);

            Models.Engagement dbEngagement;
            if (engagement.Id == 0)
            {
                dbEngagement = new Models.Engagement();
                mapper.Map(engagement, dbEngagement);
                if (dbEngagement.CreatedOn == default)
                    dbEngagement.CreatedOn = DateTimeOffset.UtcNow;
                dbEngagement.LastUpdatedOn = DateTimeOffset.UtcNow;
                broadcastingContext.Engagements.Add(dbEngagement);
            }
            else
            {
                dbEngagement = await broadcastingContext.Engagements
                    .Include(e => e.Talks)
                    .FirstOrDefaultAsync(e => e.Id == engagement.Id, cancellationToken);

                if (dbEngagement is null)
                {
                    return OperationResult<Domain.Models.Engagement>.Failure($"Engagement '{engagement.Id}' not found");
                }

                mapper.Map(engagement, dbEngagement);
                if (dbEngagement.CreatedOn == default)
                    dbEngagement.CreatedOn = DateTimeOffset.UtcNow;
                dbEngagement.LastUpdatedOn = DateTimeOffset.UtcNow;
            }

            SyncTalks(dbEngagement, engagement.Talks);

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

    private void SyncTalks(Models.Engagement destination, IReadOnlyCollection<Domain.Models.Talk>? talks)
    {
        if (talks is null)
        {
            return;
        }

        foreach (var talk in talks)
        {
            var existingTalk = FindMatchingTalk(destination, talk);
            if (existingTalk is null)
            {
                var dbTalk = mapper.Map<Models.Talk>(talk);
                dbTalk.CreatedByEntraOid ??= destination.CreatedByEntraOid;
                if (destination.Id != 0)
                {
                    dbTalk.EngagementId = destination.Id;
                }

                destination.Talks.Add(dbTalk);
                continue;
            }

            ApplyTalkValues(talk, existingTalk, destination.CreatedByEntraOid);
        }
    }

    private static Models.Talk? FindMatchingTalk(Models.Engagement destination, Domain.Models.Talk talk)
    {
        if (talk.Id > 0)
        {
            return destination.Talks.FirstOrDefault(existingTalk => existingTalk.Id == talk.Id);
        }

        return destination.Talks.FirstOrDefault(existingTalk =>
            existingTalk.Name == talk.Name
            && existingTalk.UrlForConferenceTalk == talk.UrlForConferenceTalk
            && existingTalk.UrlForTalk == talk.UrlForTalk
            && existingTalk.StartDateTime == talk.StartDateTime
            && existingTalk.EndDateTime == talk.EndDateTime);
    }

    private static void ApplyTalkValues(Domain.Models.Talk source, Models.Talk destination, string? ownerEntraOid)
    {
        destination.Name = source.Name;
        destination.UrlForConferenceTalk = source.UrlForConferenceTalk;
        destination.UrlForTalk = source.UrlForTalk;
        destination.StartDateTime = source.StartDateTime;
        destination.EndDateTime = source.EndDateTime;
        destination.TalkLocation = source.TalkLocation;
        destination.Comments = source.Comments;
        destination.CreatedByEntraOid = source.CreatedByEntraOid ?? ownerEntraOid;

        if (source.EngagementId > 0)
        {
            destination.EngagementId = source.EngagementId;
        }
    }

    public async Task<List<Domain.Models.Engagement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbEngagements = await broadcastingContext.Engagements.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.Engagement>>(dbEngagements);
    }

    public async Task<List<Domain.Models.Engagement>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbEngagements = await broadcastingContext.Engagements
            .Where(e => e.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);
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

    public async Task<bool> IsEngagementUniqueToUser(string name, string url, int year, string ownerOid, CancellationToken cancellationToken = default)
    {
        return !await broadcastingContext.Engagements.AsNoTracking()
            .AnyAsync(e => e.Name == name && e.Url == url && e.StartDateTime.Year == year && e.CreatedByEntraOid == ownerOid, cancellationToken);
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.Engagement>> GetAllAsync(int page, int pageSize, string sortBy = "startdate", bool sortDescending = true, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.Engagement> query = broadcastingContext.Engagements
            .AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(e => e.Name.ToLower().Contains(lowerFilter));
        }
        
        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.Engagement.Name).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name);
        }
        else if (sortByLower == nameof(Models.Engagement.EndDateTime).ToLowerInvariant().Replace("datetime", "date"))
        {
            query = sortDescending ? query.OrderByDescending(e => e.EndDateTime) : query.OrderBy(e => e.EndDateTime);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(e => e.StartDateTime) : query.OrderBy(e => e.StartDateTime);
        }
        
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

    public async Task<Domain.Models.PagedResult<Domain.Models.Engagement>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "startdate", bool sortDescending = true, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.Engagement> query = broadcastingContext.Engagements
            .AsNoTracking()
            .Where(e => e.CreatedByEntraOid == ownerEntraOid);
        
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(e => e.Name.ToLower().Contains(lowerFilter));
        }
        
        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.Engagement.Name).ToLowerInvariant())
        {
            query = sortDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name);
        }
        else if (sortByLower == nameof(Models.Engagement.EndDateTime).ToLowerInvariant().Replace("datetime", "date"))
        {
            query = sortDescending ? query.OrderByDescending(e => e.EndDateTime) : query.OrderBy(e => e.EndDateTime);
        }
        else
        {
            query = sortDescending ? query.OrderByDescending(e => e.StartDateTime) : query.OrderBy(e => e.StartDateTime);
        }
        
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