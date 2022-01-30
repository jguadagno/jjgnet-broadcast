using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class EngagementRepository: IEngagementRepository
{

    private readonly IEngagementDataStore _emgEngagementDataStore;

    public EngagementRepository(IEngagementDataStore emgEngagementDataStore)
    {
        _emgEngagementDataStore = emgEngagementDataStore;
    }
        
    public async Task<Engagement> GetAsync(int primaryKey)
    {
        return await _emgEngagementDataStore.GetAsync(primaryKey);
    }

    public async Task<bool> SaveAsync(Engagement entity)
    {
        return await _emgEngagementDataStore.SaveAsync(entity);
    }

    public async Task<bool> SaveAllAsync(List<Engagement> entities)
    {
        return await _emgEngagementDataStore.SaveAllAsync(entities);
    }

    public async Task<List<Engagement>> GetAllAsync()
    {
        return await _emgEngagementDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(Engagement entity)
    {
        return await _emgEngagementDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _emgEngagementDataStore.DeleteAsync(primaryKey);
    }

    public async Task<bool> AddTalkToEngagementAsync(Engagement engagement, Talk talk)
    {
        return await _emgEngagementDataStore.AddTalkToEngagementAsync(engagement, talk);
    }

    public async Task<bool> AddTalkToEngagementAsync(int engagementId, Talk talk)
    {
        return await _emgEngagementDataStore.AddTalkToEngagementAsync(engagementId, talk);
    }

    public async Task<bool> SaveTalkAsync(Talk talk)
    {
        return await _emgEngagementDataStore.SaveTalkAsync(talk);
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(int talkId)
    {
        return await _emgEngagementDataStore.RemoveTalkFromEngagementAsync(talkId);
    }

    public async Task<bool> RemoveTalkFromEngagementAsync(Talk talk)
    {
        return await _emgEngagementDataStore.RemoveTalkFromEngagementAsync(talk);
    }
}