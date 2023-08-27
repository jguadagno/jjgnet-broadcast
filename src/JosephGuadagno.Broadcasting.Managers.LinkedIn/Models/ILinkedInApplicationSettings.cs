namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

public interface ILinkedInApplicationSettings
{
    string? ClientId { get; set; }
    string? ClientSecret { get; set; }
    string? AccessToken { get; set; }
    string? AuthorId { get; set; }
}