using System.Threading.Tasks;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUrlShortener
{
    public Task<string> GetShortenedUrlAsync(string url, string domain);
}