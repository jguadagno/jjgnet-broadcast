namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

/// <summary>
/// Sends a message to create a LinkedIn Post with an image
/// </summary>
public class LinkedInPostImage
{
    /// <summary>
    /// The access token to use to post the message
    /// </summary>
    public required string AccessToken { get; set; }
    /// <summary>
    /// The LinkedIn Author Id to use to post the message
    /// </summary>
    public required string AuthorId { get; set; }
    /// <summary>
    /// The text to use in the post
    /// </summary>
    public required string Text { get; set; }
    /// <summary>
    /// The image url to use. This must be a publicly accessible url
    /// </summary>
    public required string ImageUrl { get; set; }
    /// <summary>
    /// The title of the image (Optional)
    /// </summary>
    public required string Title { get; set; }
    /// <summary>
    /// The description of the image (Optional)
    /// </summary>
    public required string Description { get; set; }
}