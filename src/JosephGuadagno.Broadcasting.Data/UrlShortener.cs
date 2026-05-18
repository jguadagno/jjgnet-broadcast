using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Utilities.Web.Shortener;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data;

public class UrlShortener(Bitly bitly, ILogger<UrlShortener> logger) : IUrlShortener
{
	public async Task<string?> GetShortenedUrlAsync(string url, string domain)
    {
        if (string.IsNullOrEmpty(url))
        {
            logger.LogDebug("Url was null or empty");
            return null;
        }

        var result = await bitly.Shorten(url, domain);

        if (result == null)
        {
            logger.LogWarning("Could not shorten the url of '{Url}'. The response was null", url);
            return url;
        }
        logger.LogDebug("Shortened the url of '{Url}' to '{ResultLink}'", url, result.Link);
        return result.Link;
    }
}