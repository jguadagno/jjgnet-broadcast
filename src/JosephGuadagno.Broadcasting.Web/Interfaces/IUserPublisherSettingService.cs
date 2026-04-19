using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with user publisher settings in the API.
/// </summary>
public interface IUserPublisherSettingService
{
    Task<List<UserPublisherSetting>> GetCurrentUserAsync();
    Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid);
    Task<UserPublisherSetting?> SaveCurrentUserAsync(UserPublisherSetting setting);
    Task<UserPublisherSetting?> SaveByUserAsync(string ownerOid, UserPublisherSetting setting);
}
