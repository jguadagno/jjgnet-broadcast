using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with user collector speaking engagements in the API.
/// </summary>
public interface IUserCollectorSpeakingEngagementService
{
    Task<List<UserCollectorSpeakingEngagement>> GetCurrentUserAsync();
    Task<List<UserCollectorSpeakingEngagement>> GetByUserAsync(string ownerOid);
    Task<UserCollectorSpeakingEngagement?> GetByIdAsync(int id);
    Task<UserCollectorSpeakingEngagement?> AddCurrentUserAsync(UserCollectorSpeakingEngagement engagement);
    Task<UserCollectorSpeakingEngagement?> AddByUserAsync(string ownerOid, UserCollectorSpeakingEngagement engagement);
    Task<UserCollectorSpeakingEngagement?> UpdateCurrentUserAsync(UserCollectorSpeakingEngagement engagement);
    Task<UserCollectorSpeakingEngagement?> UpdateByUserAsync(string ownerOid, UserCollectorSpeakingEngagement engagement);
    Task<bool> DeleteCurrentUserAsync(int id);
    Task<bool> DeleteByUserAsync(string ownerOid, int id);
    Task<bool> ToggleActiveAsync(int id);
}
