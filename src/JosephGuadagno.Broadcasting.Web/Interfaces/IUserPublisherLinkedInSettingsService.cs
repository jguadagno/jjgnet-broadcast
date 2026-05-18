using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IUserPublisherLinkedInSettingsService
{
    Task<UserPublisherLinkedInSettings?> GetCurrentUserAsync();
    Task<UserPublisherLinkedInSettings?> SaveCurrentUserAsync(
        UserPublisherLinkedInSettings settings,
        string? clientSecret = null,
        string? accessToken = null);
}
