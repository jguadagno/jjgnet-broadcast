using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPlatformLinkedInSettingsService
{
    Task<UserPlatformLinkedInSettings?> GetCurrentUserAsync();
    Task<UserPlatformLinkedInSettings?> SaveCurrentUserAsync(
        UserPlatformLinkedInSettings settings,
        string? clientSecret = null,
        string? accessToken = null);
}

