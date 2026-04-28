using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating/updating a social media platform.
/// </summary>
public class SocialMediaPlatformRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Url]
    public string? Url { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? CredentialSetupDocumentationUrl { get; set; }
}
