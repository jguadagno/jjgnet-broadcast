using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service for checking and reporting user onboarding setup completion status.
/// </summary>
public interface ISetupService
{
    /// <summary>
    /// Gets the current user's setup status.
    /// </summary>
    /// <param name="forceRefresh">
    /// When <see langword="true"/>, bypasses the cached result and fetches fresh data from the API.
    /// Pass <see langword="true"/> when the user has just configured something and the UI must reflect
    /// the updated state immediately.
    /// </param>
    Task<SetupStatus> GetSetupStatusAsync(bool forceRefresh = false);

    /// <summary>
    /// Invalidates all setup-related cache entries for the current user.
    /// Call this after any mutation to collectors, publishers, or message templates so the
    /// next page render reflects the updated state without waiting for the 5-minute TTL to expire.
    /// </summary>
    Task InvalidateAsync();
}
