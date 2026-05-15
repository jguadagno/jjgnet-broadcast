using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for a user collector YouTube channel.
/// </summary>
public class UserCollectorYouTubeChannelViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Channel ID is required.")]
    [StringLength(50, ErrorMessage = "Channel ID cannot exceed 50 characters.")]
    [Display(Name = "Channel ID")]
    public string ChannelId { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Playlist ID cannot exceed 255 characters.")]
    [Display(Name = "Playlist ID")]
    public string PlaylistId { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "API Key cannot exceed 255 characters.")]
    [Display(Name = "API Key")]
    [DataType(DataType.Password)]
    public string? ApiKey { get; set; }

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(255, ErrorMessage = "Display name cannot exceed 255 characters.")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Range(1, 200, ErrorMessage = "Results Per Page must be between 1 and 200.")]
    [Display(Name = "Results Per Page")]
    public int ResultSetPageSize { get; set; } = 50;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }

    public bool IsManagedBySiteAdmin { get; set; }

    /// <summary>
    /// Gets or sets whether a Google API key is currently stored in Key Vault for this channel.
    /// When false on Edit, ApiKey is required to supply a new one.
    /// </summary>
    public bool HasApiKey { get; set; }
}
