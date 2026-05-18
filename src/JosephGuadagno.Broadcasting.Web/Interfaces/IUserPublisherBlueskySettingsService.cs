using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPublisherBlueskySettingsService
{
    Task<UserPublisherBlueskySettings?> GetCurrentUserAsync();
    Task<UserPublisherBlueskySettings?> SaveCurrentUserAsync(UserPublisherBlueskySettings settings, string? appPassword = null);
}
