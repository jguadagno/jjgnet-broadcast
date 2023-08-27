namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

/// <summary>
/// Represents the media assets attached to the share
/// </summary>
public enum ShareMediaCategoryEnum
{
    /// <summary>
    /// The share does not contain any media, and will only consist of text.
    /// </summary>
    None,
    /// <summary>
    /// The contains a URL.
    /// </summary>
    Article,
    /// <summary>
    /// The share contains an image
    /// </summary>
    Image
}