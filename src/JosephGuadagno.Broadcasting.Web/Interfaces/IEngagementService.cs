using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IEngagementService
{
    Task<List<Engagement>> GetEngagementsAsync(int? page = 1, int? pageSize = 25);
    Task<Engagement?> GetEngagementAsync(int engagementId);
    Task<Engagement?> SaveEngagementAsync(Engagement engagement);
    Task<bool> DeleteEngagementAsync(int engagementId);
    Task<List<Talk>> GetEngagementTalksAsync(int engagementId, int? page = 1, int? pageSize = 25);
    Task<Talk?> SaveEngagementTalkAsync(Talk talk);
    Task<Talk?> GetEngagementTalkAsync(int engagementId, int talkId);
    Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId);
}