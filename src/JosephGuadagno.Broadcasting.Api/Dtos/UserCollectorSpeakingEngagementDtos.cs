using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Api.Dtos;

public class UserCollectorSpeakingEngagementRequest
{
    [Required]
    [StringLength(2048)]
    public string SpeakingEngagementsFile { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UserCollectorSpeakingEngagementResponse
{
    public int Id { get; set; }
    public string SpeakingEngagementsFile { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}
