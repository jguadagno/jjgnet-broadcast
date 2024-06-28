namespace JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;

public interface IFacebookManager
{
    /// <summary>
    /// Posts a message with a link to a Facebook Page
    /// </summary>
    /// <param name="message">The message/Facebook status to post</param>
    /// <param name="link">The link for the post</param>
    /// <returns>The id of the newly created status</returns>
    Task<string> PostMessageAndLinkToPage(string message, string link);
    
    /// <summary>
    /// Returns the Graph API Root with the version
    /// </summary>
    string GraphApiRoot { get; }
}