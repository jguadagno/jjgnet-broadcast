using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.FixSourceDataShortUrl.Models;

public class Settings
{
    public string BitlyToken { get; set; } = null!;
    public string BitlyShortenedDomain { get; set; } = null!;
    public string BitlyApiRootUri { get; set; } = null!;

    public AutoMapperSettings AutoMapper { get; init; } = null!;
}