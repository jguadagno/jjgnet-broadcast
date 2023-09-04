namespace JosephGuadagno.Broadcasting.Domain.Models.Messages;

/// <summary>
/// Sends a message to create a LinkedIn Post just text
/// </summary>
public class LinkedInPostText
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
}