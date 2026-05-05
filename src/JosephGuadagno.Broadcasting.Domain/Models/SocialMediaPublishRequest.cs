namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// Common publish request used to route pre-composed content through platform-specific publishers.
/// </summary>
public class SocialMediaPublishRequest
{
    /// <summary>
    /// The primary text content for the post.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// An optional canonical link for the post.
    /// </summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// An optional shortened link used by platforms that want a separate display URL.
    /// </summary>
    public string? ShortenedUrl { get; set; }

    /// <summary>
    /// An optional public image URL or link-preview thumbnail URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Optional image bytes for platforms that upload media directly.
    /// </summary>
    public byte[]? ImageBytes { get; set; }

    /// <summary>
    /// Optional title for a linked article or uploaded media.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional description for a linked article or uploaded media.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional OAuth access token for user-scoped publishing.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Optional provider-specific author identifier.
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// Optional hashtags appended by platforms that support them.
    /// </summary>
    public IReadOnlyCollection<string>? Hashtags { get; set; }
}
