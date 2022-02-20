using System.Threading.Tasks;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUrlShortener
{
    public string GetShortenedUrl(string url, string domain);
    public Task<string> GetShortenedUrlAsync(string url, string domain);
}