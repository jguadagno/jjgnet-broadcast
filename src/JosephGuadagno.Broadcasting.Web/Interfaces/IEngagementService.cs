using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IEngagementService
{
    Task<PagedResult<Engagement>> GetEngagementsAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<Engagement?> GetEngagementAsync(int engagementId);
    Task<Engagement?> SaveEngagementAsync(Engagement engagement);
    Task<bool> DeleteEngagementAsync(int engagementId);
    Task<PagedResult<Talk>> GetEngagementTalksAsync(int engagementId, int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<Talk?> SaveEngagementTalkAsync(Talk talk);
    Task<Talk?> GetEngagementTalkAsync(int engagementId, int talkId);
    Task<bool> DeleteEngagementTalkAsync(int engagementId, int talkId);
}