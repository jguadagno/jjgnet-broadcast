using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEngagementManager: IManager<Engagement>
{
    public Task<List<Talk>> GetTalksForEngagementAsync(int engagementId);
    public Task<Talk> SaveTalkAsync(Talk talk);
    public Task<bool> RemoveTalkFromEngagementAsync(int talkId);
    public Task<bool> RemoveTalkFromEngagementAsync(Talk talk);
    public Task<Talk> GetTalkAsync(int talkId);
}