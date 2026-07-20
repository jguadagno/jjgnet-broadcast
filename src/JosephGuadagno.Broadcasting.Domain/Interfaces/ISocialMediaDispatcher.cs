using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Common dispatch contract implemented by each social media platform manager.
/// </summary>
public interface ISocialMediaDispatcher
{
    /// <summary>
    /// Dispatches a pre-composed social media request using the current platform manager.
    /// </summary>
    /// <param name="request">The pre-composed content and optional provider-specific metadata to publish.</param>
    /// <returns>The provider-specific post identifier when available.</returns>
    Task<string?> DispatchAsync(SocialMediaPublishRequest request);
}
