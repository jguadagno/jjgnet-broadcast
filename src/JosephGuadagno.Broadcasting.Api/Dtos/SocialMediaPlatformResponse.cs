namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a social media platform.
/// </summary>
public class SocialMediaPlatformResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public string? CredentialSetupDocumentationUrl { get; set; }
}
