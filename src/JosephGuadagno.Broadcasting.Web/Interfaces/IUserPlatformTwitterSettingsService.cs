using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPlatformTwitterSettingsService
{
    Task<UserPlatformTwitterSettings?> GetCurrentUserAsync();
    Task<UserPlatformTwitterSettings?> SaveCurrentUserAsync(
        UserPlatformTwitterSettings settings,
        string? consumerKey = null,
        string? consumerSecret = null,
        string? accessToken = null,
        string? accessTokenSecret = null);
}

