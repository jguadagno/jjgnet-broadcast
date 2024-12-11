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
    /// A string list of Hashtags to use
    /// </summary>
    public List<string>? Hashtags { get; set; }
}