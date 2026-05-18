using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPublisherFacebookSettingsService
{
    Task<UserPublisherFacebookSettings?> GetCurrentUserAsync();
    Task<UserPublisherFacebookSettings?> SaveCurrentUserAsync(
        UserPublisherFacebookSettings settings,
        string? pageAccessToken = null,
        string? appSecret = null,
        string? clientToken = null,
        string? shortLivedAccessToken = null,
        string? longLivedAccessToken = null);
}
