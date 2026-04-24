using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with user collector YouTube channels in the API.
/// </summary>
public interface IUserCollectorYouTubeChannelService
{
    Task<List<UserCollectorYouTubeChannel>> GetCurrentUserAsync();
    Task<List<UserCollectorYouTubeChannel>> GetByUserAsync(string ownerOid);
    Task<UserCollectorYouTubeChannel?> GetByIdAsync(int id);
    Task<UserCollectorYouTubeChannel?> AddCurrentUserAsync(UserCollectorYouTubeChannel channel);
    Task<UserCollectorYouTubeChannel?> AddByUserAsync(string ownerOid, UserCollectorYouTubeChannel channel);
    Task<UserCollectorYouTubeChannel?> UpdateCurrentUserAsync(UserCollectorYouTubeChannel channel);
    Task<UserCollectorYouTubeChannel?> UpdateByUserAsync(string ownerOid, UserCollectorYouTubeChannel channel);
    Task<bool> DeleteCurrentUserAsync(int id);
    Task<bool> DeleteByUserAsync(string ownerOid, int id);
}
