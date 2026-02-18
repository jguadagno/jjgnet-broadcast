namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public class LinkedInApplicationSettings : ILinkedInApplicationSettings
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string AccessToken { get; set; }
    public required string AuthorId { get; set; }
}