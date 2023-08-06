using System.Net.Http;
using System.Threading.Tasks;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IFacebookManager
{
    /// <summary>
    /// Posts a message with a link to a Facebook Page
    /// </summary>
    /// <param name="pageId">The id of the page</param>
    /// <param name="message">The message/Facebook status to post</param>
    /// <param name="link">The link for the post</param>
    /// <param name="accessToken">The access token to use</param>
    /// <returns></returns>
    Task<string> PostMessageAndLinkToPage(string pageId, string message, string link, string accessToken);
}