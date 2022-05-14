using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IEngagementService
{
    Task<List<Engagement>?> GetEngagementsAsync();
    Task<Engagement?> GetEngagementAsync(int engagementId);
    Task<Engagement?> SaveEngagementAsync(Engagement engagement);
    Task<bool> DeleteEngagementAsync(int engagementId);
    Task<List<Talk>?> GetEngagementTalksAsync(int engagementId);
    Task<Talk?> SaveEngagementTalkAsync(Talk talk);
    Task<Talk?> GetEngagementTalkAsync(int engagementId, int talkId);
    Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId);
}
