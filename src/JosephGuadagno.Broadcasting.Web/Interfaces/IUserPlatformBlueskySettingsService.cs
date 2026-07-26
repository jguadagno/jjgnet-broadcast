using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPlatformBlueskySettingsService
{
    Task<UserPlatformBlueskySettings?> GetCurrentUserAsync();
    Task<UserPlatformBlueskySettings?> SaveCurrentUserAsync(UserPlatformBlueskySettings settings, string? appPassword = null);
}

