using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Utilities.Web.Shortener;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data;

public class UrlShortener: IUrlShortener
{
    private readonly Bitly _bitly;
    private readonly ILogger _logger;
        
    public UrlShortener(Bitly bitly, ILogger<UrlShortener> logger)
    {
        _bitly = bitly;
        _logger = logger;
    }

    public string GetShortenedUrl(string url, string domain)
    {
        return GetShortenedUrlAsync(url, domain).Result;
    }

    public async Task<string> GetShortenedUrlAsync(string url, string domain)
    {
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogDebug("Url was null or empty.");
            return null;
        }

        var result = await _bitly.Shorten(url, domain);

        if (result == null)
        {
            _logger.LogDebug("Could not shorten the url of '{url}'", url);
            return url;
        }
        _logger.LogDebug("Shortened the url of '{url}' to '{result.Link}'", url, result.Link);
        return result.Link;
    }
}