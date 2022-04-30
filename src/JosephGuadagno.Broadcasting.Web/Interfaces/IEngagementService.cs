using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IEngagementService
{
    Task<List<Engagement>?> GetEngagementsAsync();
    Task<Engagement?> GetEngagementAsync(int engagementId);
    Task<Engagement?> SaveEngagementAsync(Engagement engagement);
    Task<bool> DeleteEngagementAsync(int engagementId);
    Task<List<Engagement>?> GetEngagementTalksAsync(int engagementId);
    Task<Talk?> SaveEngagementTalkAsync(Talk talk);
    Task<Engagement?> GetEngagementTalkAsync(int engagementId, int talkId);
    Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId);
}
