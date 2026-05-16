using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPublisherTwitterSettingsService
{
    Task<UserPublisherTwitterSettings?> GetCurrentUserAsync();
    Task<UserPublisherTwitterSettings?> SaveCurrentUserAsync(
        UserPublisherTwitterSettings settings,
        string? consumerKey = null,
        string? consumerSecret = null,
        string? accessToken = null,
        string? accessTokenSecret = null);
}
