using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with user collector feed sources in the API.
/// </summary>
public interface IUserCollectorFeedSourceService
{
    Task<List<UserCollectorFeedSource>> GetCurrentUserAsync();
    Task<List<UserCollectorFeedSource>> GetByUserAsync(string ownerOid);
    Task<UserCollectorFeedSource?> GetByIdAsync(int id);
    Task<UserCollectorFeedSource?> AddCurrentUserAsync(UserCollectorFeedSource feedSource);
    Task<UserCollectorFeedSource?> AddByUserAsync(string ownerOid, UserCollectorFeedSource feedSource);
    Task<UserCollectorFeedSource?> UpdateCurrentUserAsync(UserCollectorFeedSource feedSource);
    Task<UserCollectorFeedSource?> UpdateByUserAsync(string ownerOid, UserCollectorFeedSource feedSource);
    Task<bool> DeleteCurrentUserAsync(int id);
    Task<bool> DeleteByUserAsync(string ownerOid, int id);
}
