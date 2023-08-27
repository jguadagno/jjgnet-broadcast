namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public interface ILinkedInManager
{
    /// <summary>
    /// Gets the current user's profile based on the access token
    /// </summary>
    /// <param name="accessToken">The access token to use for the call</param>
    /// <returns>A <see cref="LinkedInUser"/> upon success</returns>
    /// <remarks>
    /// Based on documentation: https://learn.microsoft.com/en-us/linkedin/shared/integrations/people/profile-api?context=linkedin%2Fconsumer%2Fcontext#retrieve-current-members-profile
    ///  
    /// Calling this method requires one of the following permissions:
    /// | Permission      |	Description |
    /// | r_liteprofile	  | Required to retrieve name and photo for the authenticated user. Please review Lite Profile Fields. |
    /// | r_basicprofile  |	Required to retrieve name, photo, headline, and vanity name for the authenticated user. Please review Basic Profile Fields. Note that the v2 r_basicprofile permission grants only a subset of fields provided in v1. |
    /// | r_compliance	  | [Private permission] Required to retrieve your activity for compliance monitoring and archiving. This is a private permission and access is granted to select developers. |
    /// Details: https://learn.microsoft.com/en-us/linkedin/shared/integrations/people/profile-api?context=linkedin%2Fconsumer%2Fcontext#permissions
    /// </remarks>
    Task<LinkedInUser> GetMyLinkedInUserProfile(string accessToken);

    /// <summary>
    /// Shares a text only post to LinkedIn
    /// </summary>
    /// <remarks>Based on the API https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/share-on-linkedin?context=linkedin%2Fconsumer%2Fcontext#create-a-text-share</remarks>
    /// <param name="accessToken">The access token to use</param>
    /// <param name="authorId">The LinkedIn URN of the person authoring this share</param>
    /// <param name="postText">The text of the share</param>
    /// <returns>A string with the LinkedIn share id</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    Task<string> PostShareText(string accessToken, string authorId, string postText);
    
    /// <summary>
    /// Post a text and link to LinkedIn
    /// </summary>
    /// <param name="accessToken">The access token to use</param>
    /// <param name="authorId">The LinkedIn URN of the person authoring this share</param>
    /// <param name="postText">The text of the share</param>
    /// <param name="link">The link to share. Note, there is no validation of this link</param>
    /// <param name="linkTitle">Optional, title of the link</param>
    /// <param name="linkDescription">Optional, description of the link</param>
    /// <returns>A string with the LinkedIn share id</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    Task<string> PostShareTextAndLink(string accessToken, string authorId, string postText, string link, string? linkTitle = null, string? linkDescription = null);
    
    /// <summary>
    /// Shares text and an image to LinkedIn
    /// </summary>
    /// <param name="accessToken">The access token to use</param>
    /// <param name="authorId">The LinkedIn URN of the person authoring this share</param>
    /// <param name="postText">The text of the share</param>
    /// <param name="image">The byte array for the image</param>
    /// <param name="imageTitle">Optional, title of the image</param>
    /// <param name="imageDescription">Optional, description of the image</param>
    /// <returns>A string with the LinkedIn share id</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ApplicationException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    Task<string> PostShareTextAndImage(string accessToken, string authorId, string postText, byte[] image, string? imageTitle = null, string? imageDescription = null);
}