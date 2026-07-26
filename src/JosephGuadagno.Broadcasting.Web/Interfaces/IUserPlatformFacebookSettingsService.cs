using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPlatformFacebookSettingsService
{
    Task<UserPlatformFacebookSettings?> GetCurrentUserAsync();
    Task<UserPlatformFacebookSettings?> SaveCurrentUserAsync(
        UserPlatformFacebookSettings settings,
        string? pageAccessToken = null,
        string? appSecret = null,
        string? clientToken = null,
        string? shortLivedAccessToken = null,
        string? longLivedAccessToken = null);
}

