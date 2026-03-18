using JosephGuadagno.Broadcasting.Managers.Facebook.Models;

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
    /// Posts a message with a link and a custom picture thumbnail to a Facebook Page.
    /// The <paramref name="picture"/> URL is passed as the <c>picture</c> parameter to the
    /// Graph API <c>/feed</c> endpoint to override the link-preview thumbnail.
    /// </summary>
    /// <param name="message">The message/Facebook status to post</param>
    /// <param name="link">The link for the post</param>
    /// <param name="picture">The URL of the image to use as the link thumbnail</param>
    /// <returns>The id of the newly created status</returns>
    Task<string> PostMessageLinkAndPictureToPage(string message, string link, string picture);

    /// <summary>
    /// Refreshes the token
    /// </summary>
    /// <param name="tokenToRefresh">The token to refresh</param>
    /// <returns></returns>
    Task<TokenInfo> RefreshToken(string tokenToRefresh);

    /// <summary>
    /// Returns the Graph API Root with the version
    /// </summary>
    string GraphApiRoot { get; }
}