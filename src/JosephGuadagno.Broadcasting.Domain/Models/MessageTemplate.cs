using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// A Scriban template used to render broadcast messages for a specific platform and message type.
/// </summary>
public class MessageTemplate
{
    /// <summary>
    /// The social platform ID, e.g. references SocialMediaPlatforms table.
    /// </summary>
    [Required]
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// The message category, e.g. RandomPost.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The Scriban template string used to render the broadcast message.
    /// </summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what the template is for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who created this message template
    /// </summary>
    public string? CreatedByEntraOid { get; set; }
}
