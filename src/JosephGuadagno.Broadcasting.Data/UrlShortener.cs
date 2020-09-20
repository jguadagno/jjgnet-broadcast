using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Utilities.Web.Shortener;

namespace JosephGuadagno.Broadcasting.Data
{
    public class UrlShortener: IUrlShortener
    {
        private readonly Bitly _bitly; 
        public UrlShortener(Bitly bitly)
        {
            _bitly = bitly;
        }

        public string GetShortenedUrl(string url, string domain)
        {
            return GetShortenedUrlAsync(url, domain).Result;
        }

        public async Task<string> GetShortenedUrlAsync(string url, string domain)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var result = await _bitly.Shorten(url, domain);
            return result == null ? url : result.Link;
        }
    }
}