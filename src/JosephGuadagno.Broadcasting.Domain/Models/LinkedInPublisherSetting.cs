namespace JosephGuadagno.Broadcasting.Domain.Models;

public class LinkedInPublisherSetting
{
    public string? AuthorId { get; set; }

    public string? ClientId { get; set; }

    public bool HasClientSecret { get; set; }

    public bool HasAccessToken { get; set; }
}
