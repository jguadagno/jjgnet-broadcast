using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EngagementDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IEngagementDataStore
{
    public async Task<Domain.Models.Engagement> GetAsync(int primaryKey)
    {
        var dbEngagement =
            await broadcastingContext.Engagements.Include(e => e.Talks)
                .FirstOrDefaultAsync(e => e.Id == primaryKey);
        return mapper.Map<Domain.Models.Engagement>(dbEngagement);
    }

    public async Task<Domain.Models.Engagement> SaveAsync(Domain.Models.Engagement engagement)
    {
        var dbEngagement = mapper.Map<Models.Engagement>(engagement);
        broadcastingContext.Entry(dbEngagement).State =
            dbEngagement.Id == 0 ? EntityState.Added : EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.Engagement>(dbEngagement);
        }

        throw new ApplicationException("Failed to save engagement");
    }

    public async Task<List<Domain.Models.Engagement>> GetAllAsync()
    {
        var dbEngagements = await broadcastingContext.Engagements.ToListAsync();
        return mapper.Map<List<Domain.Models.Engagement>>(dbEngagements);
    }

    public async Task<bool> DeleteAsync(Domain.Models.Engagement engagement)
    {
        return await DeleteAsync(engagement.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbEngagement = await broadcastingContext.Engagements
            .Include(e => e.Talks)
            .FirstOrDefaultAsync(e => e.Id == primaryKey);

        if (dbEngagement == null)
        {
            return true;
        }

        foreach (var talk in dbEngagement.Talks)
        {
            broadcastingContext.Talks.Remove(talk);
        }
        broadcastingContext.Engagements.Remove(dbEngagement);

        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<List<Domain.Models.Talk>> GetTalksForEngagementAsync(int engagementId)
    {
        var talks = await broadcastingContext.Talks.Where(e => e.EngagementId == engagementId).ToListAsync();
        return mapper.Map<List<Domain.Models.Talk>>(talks);
    }

    public async Task<bool> AddTalkToEngagementAsync(Domain.Models.Engagement engagement, Domain.Models.Talk talk)
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
            dbEngagement = await broadcastingContext.Engagements.FindAsync(engagement.Id);
            if (dbEngagement is null)
            {
                return false;
            }
        }
        
        // Save Talk
        var dbTalk = mapper.Map<Models.Talk>(talk);
        dbEngagement.Talks.Add(dbTalk);
        
        // Save
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> AddTalkToEngagementAsync(int engagementId, Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(talk);
        if (engagementId <= 0)
        {
            throw new ApplicationException("EngagementId can not <= 0");
        }
        
        var dbEngagement = await broadcastingContext.Engagements.FindAsync(engagementId);
        if (dbEngagement is null)
        {
            return false;
        }
        
        // Save Talk
        var dbTalk = mapper.Map<Models.Talk>(talk);
        dbEngagement.Talks.Add(dbTalk);
        
        // Save
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Domain.Models.Talk> SaveTalkAsync(Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(talk);

        var dbTalk = await broadcastingContext.Talks.FirstOrDefaultAsync(t => t.Id == talk.Id) ?? new Models.Talk();
        
        if (talk.Id == 0)
        {
            dbTalk = mapper.Map<Models.Talk>(talk);
            broadcastingContext.Talks.Add(dbTalk);
        }
        else
        {
            broadcastingContext.Entry(dbTalk).CurrentValues.SetValues(talk);
        }
        
        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.Talk>(dbTalk);
        }

        throw new ApplicationException("Failed to save talk");
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(int talkId)
    {
        if (talkId <= 0)
        {
            throw new ApplicationException("The TalkId can not be <=0");
        }

        var dbTalk = await broadcastingContext.Talks.FindAsync(talkId);
        if (dbTalk is null)
        {
            return true;
        }

        broadcastingContext.Talks.Remove(dbTalk);

        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(Domain.Models.Talk talk)
    {
        ArgumentNullException.ThrowIfNull(talk);

        var dbTalk = mapper.Map<Models.Talk>(talk);
        broadcastingContext.Talks.Remove(dbTalk);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Domain.Models.Talk?> GetTalkAsync(int talkId)
    {
        if (talkId <= 0)
        {
            throw new ApplicationException("The TalkId can not be <=0");
        }

        var dbTalk = await broadcastingContext.Talks.FindAsync(talkId);
        return dbTalk is null ? null : mapper.Map<Domain.Models.Talk>(dbTalk);
    }

    public async Task<Domain.Models.Engagement?> GetByNameAndUrlAndYearAsync(string name, string url, int year)
    {
        var dbEngagement = await broadcastingContext.Engagements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == name && e.Url == url && e.StartDateTime.Year == year);
        return dbEngagement is null ? null : mapper.Map<Domain.Models.Engagement>(dbEngagement);
    }
}