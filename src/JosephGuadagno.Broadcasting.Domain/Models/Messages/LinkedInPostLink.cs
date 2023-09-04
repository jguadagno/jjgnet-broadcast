namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

/// <summary>
/// Sends a message to create a LinkedIn Post with a link
/// </summary>
public class LinkedInPostLink
{
    /// <summary>
    /// The access token to use to post the message
    /// </summary>
    public string AccessToken { get; set; }
    /// <summary>
    /// The LinkedIn Author Id to use to post the message
    /// </summary>
    public string AuthorId { get; set; }
    /// <summary>
    /// The text to use in the post
    /// </summary>
    public string Text { get; set; }
    /// <summary>
    /// The url to use for the post
    /// </summary>
    public string LinkUrl { get; set; }
    /// <summary>
    /// The title of the Url (Optional)
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// The description of the Url (Optional)
    /// </summary>
    public string Description { get; set; }
}