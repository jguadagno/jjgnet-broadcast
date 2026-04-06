using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEngagementManager: IManager<Engagement>
{
    public Task<List<Talk>> GetTalksForEngagementAsync(int engagementId, CancellationToken cancellationToken = default);
    public Task<Talk> SaveTalkAsync(Talk talk, CancellationToken cancellationToken = default);
    public Task<bool> RemoveTalkFromEngagementAsync(int talkId, CancellationToken cancellationToken = default);
    public Task<bool> RemoveTalkFromEngagementAsync(Talk talk, CancellationToken cancellationToken = default);
    public Task<Talk> GetTalkAsync(int talkId, CancellationToken cancellationToken = default);
    public Task<Engagement?> GetByNameAndUrlAndYearAsync(string name, string url, int year, CancellationToken cancellationToken = default);
    
    Task<PagedResult<Engagement>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Talk>> GetTalksForEngagementAsync(int engagementId, int page, int pageSize, CancellationToken cancellationToken = default);
}