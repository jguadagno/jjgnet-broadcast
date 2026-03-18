namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

/// <summary>
/// Represents a message to send as a tweet, optionally with an image URL.
/// </summary>
public class TwitterTweetMessage
{
    /// <summary>
    /// The text of the tweet
    /// </summary>
    public required string Text { get; set; }
    /// <summary>
    /// An optional URL for an image to attach to the tweet.
    /// Full media upload via the Twitter v1.1 media API is required for actual image attachment.
    /// </summary>
    public string? ImageUrl { get; set; }
}
