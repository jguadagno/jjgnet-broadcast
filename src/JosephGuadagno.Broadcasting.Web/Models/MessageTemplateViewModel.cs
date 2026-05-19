using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// A Scriban template used to render broadcast messages for a specific platform and message type.
/// </summary>
public class MessageTemplateViewModel
{
    /// <summary>
    /// The social platform name, e.g. Twitter, Facebook, LinkedIn, Bluesky.
    /// </summary>
    [Required]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// The message category, e.g. RandomPost, NewPost.
    /// </summary>
    [Required]
    [Display(Name = "Message Type")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The Scriban template string used to render the broadcast message.
    /// </summary>
    [Required]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what the template is for.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The Entra Object ID of the user who owns this template.
    /// Carried as a hidden field so admins can route edits to the correct user's record.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the user who owns this template.
    /// "Default" for system templates. Populated on the Index page only.
    /// </summary>
    public string? OwnerDisplayName { get; set; }
}
