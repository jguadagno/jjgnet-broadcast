using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEngagementDataStore : IDataStore<Engagement>
{
    public Task<List<Talk>> GetTalksForEngagementAsync(int engagementId, CancellationToken cancellationToken = default);
    public Task<bool> AddTalkToEngagementAsync(Engagement engagement, Talk talk, CancellationToken cancellationToken = default);
    public Task<bool> AddTalkToEngagementAsync(int engagementId, Talk talk, CancellationToken cancellationToken = default);
    public Task<OperationResult<Talk>> SaveTalkAsync(Talk talk, CancellationToken cancellationToken = default);
    public Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(int talkId, CancellationToken cancellationToken = default);
    public Task<OperationResult<bool>> RemoveTalkFromEngagementAsync(Talk talk, CancellationToken cancellationToken = default);
    public Task<Talk?> GetTalkAsync(int talkId, CancellationToken cancellationToken = default);
    public Task<Engagement?> GetByNameAndUrlAndYearAsync(string name, string url, int year, CancellationToken cancellationToken = default);
    
    Task<PagedResult<Engagement>> GetAllAsync(int page, int pageSize, string sortBy = "startdate", bool sortDescending = true, string? filter = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Talk>> GetTalksForEngagementAsync(int engagementId, int page, int pageSize, CancellationToken cancellationToken = default);
}