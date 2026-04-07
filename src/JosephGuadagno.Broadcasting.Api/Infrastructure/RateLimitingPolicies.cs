namespace JosephGuadagno.Broadcasting.Api.Infrastructure;

/// <summary>
/// Named rate limiting policy constants for the Broadcasting API.
/// </summary>
public static class RateLimitingPolicies
{
    /// <summary>
    /// Fixed window policy: 100 requests per minute, applied globally to all API endpoints.
    /// </summary>
    public const string FixedWindow = "FixedWindow";
}
