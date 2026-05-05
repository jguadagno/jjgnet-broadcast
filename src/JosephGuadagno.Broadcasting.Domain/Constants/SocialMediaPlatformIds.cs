namespace JosephGuadagno.Broadcasting.Domain.Constants;

/// <summary>
/// Constant IDs for social media platforms matching database seed values.
/// These IDs are pinned via SET IDENTITY_INSERT in
/// <c>scripts/database/data-seed.sql</c>.
/// If a new platform is added, update BOTH that seed script and this file.
/// </summary>
public static class SocialMediaPlatformIds
{
    /// <summary>
    /// Twitter platform ID
    /// </summary>
    public const int Twitter = 1;

    /// <summary>
    /// Bluesky platform ID
    /// </summary>
    public const int Bluesky = 2;

    /// <summary>
    /// LinkedIn platform ID
    /// </summary>
    public const int LinkedIn = 3;

    /// <summary>
    /// Facebook platform ID
    /// </summary>
    public const int Facebook = 4;
}
