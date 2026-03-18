namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Models;

public class BlueskyPostMessage
{
    /// <summary>
    /// The text portion of the post
    /// </summary>
    public required string Text { get; set; }
    /// <summary>
    /// A url for the post
    /// </summary>
    public string? Url { get; set; }
    /// <summary>
    /// The shortened url for the post
    /// </summary>
    public string? ShortenedUrl { get; set; }
    /// <summary>
    /// A string list of Hashtags to use
    /// </summary>
    public List<string>? Hashtags { get; set; }
    /// <summary>
    /// An optional URL for an explicit thumbnail image. When both <see cref="Url"/> and
    /// <see cref="ImageUrl"/> are set, <see cref="ImageUrl"/> is used as the link-card thumbnail
    /// instead of fetching the og:image from the page. When only <see cref="ImageUrl"/> is set,
    /// it is used as the embed source URL.
    /// </summary>
    public string? ImageUrl { get; set; }
}