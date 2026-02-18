using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.FixSourceDataShortUrl.Models;

public class Settings
{
    public string BitlyToken { get; set; }
    public string BitlyShortenedDomain { get; set; }
    public string BitlyApiRootUri { get; set; }

    public AutoMapperSettings AutoMapper { get; init; }
}