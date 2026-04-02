namespace JosephGuadagno.Broadcasting.Domain.Models;

/// <summary>
/// An email template used for sending notification emails (e.g., user approval/rejection).
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// The unique identifier for this email template.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The unique name identifying this template, e.g. "UserApproved", "UserRejected".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email subject line.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The HTML body of the email.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// The date and time this template was created.
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// The date and time this template was last updated.
    /// </summary>
    public DateTimeOffset UpdatedDate { get; set; }
}
