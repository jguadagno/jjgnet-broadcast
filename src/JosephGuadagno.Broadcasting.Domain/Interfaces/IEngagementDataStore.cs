using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEngagementDataStore : IDataStore<Engagement>
{
    public Task<List<Talk>> GetTalksForEngagementAsync(int engagementId);
    public Task<bool> AddTalkToEngagementAsync(Engagement engagement, Talk talk);
    public Task<bool> AddTalkToEngagementAsync(int engagementId, Talk talk);
    public Task<Talk> SaveTalkAsync(Talk talk);
    public Task<bool> RemoveTalkFromEngagementAsync(int talkId);
    public Task<bool> RemoveTalkFromEngagementAsync(Talk talk);
    public Task<Talk> GetTalkAsync(int talkId);
}