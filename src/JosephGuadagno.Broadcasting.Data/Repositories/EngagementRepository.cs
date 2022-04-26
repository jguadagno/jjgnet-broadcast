using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class EngagementRepository: IEngagementRepository
{

    private readonly IEngagementDataStore _engagementDataStore;

    public EngagementRepository(IEngagementDataStore engagementDataStore)
    {
        _engagementDataStore = engagementDataStore;
    }
        
    public async Task<Engagement> GetAsync(int primaryKey)
    {
        return await _engagementDataStore.GetAsync(primaryKey);
    }

    public async Task<Engagement> SaveAsync(Engagement entity)
    {
        return await _engagementDataStore.SaveAsync(entity);
    }

    public async Task<List<Engagement>> GetAllAsync()
    {
        return await _engagementDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(Engagement entity)
    {
        return await _engagementDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _engagementDataStore.DeleteAsync(primaryKey);
    }

    public async Task<List<Talk>> GetTalksForEngagementAsync(int engagementId)
    {
        return await _engagementDataStore.GetTalksForEngagementAsync(engagementId);
    }

    public async Task<bool> AddTalkToEngagementAsync(Engagement engagement, Talk talk)
    {
        return await _engagementDataStore.AddTalkToEngagementAsync(engagement, talk);
    }

    public async Task<bool> AddTalkToEngagementAsync(int engagementId, Talk talk)
    {
        return await _engagementDataStore.AddTalkToEngagementAsync(engagementId, talk);
    }

    public async Task<bool> SaveTalkAsync(Talk talk)
    {
        return await _engagementDataStore.SaveTalkAsync(talk);
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(int talkId)
    {
        return await _engagementDataStore.RemoveTalkFromEngagementAsync(talkId);
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(Talk talk)
    {
        return await _engagementDataStore.RemoveTalkFromEngagementAsync(talk);
    }

    public async Task<Talk> GetTalkAsync(int talkId)
    {
        return await _engagementDataStore.GetTalkAsync(talkId);
    }
}