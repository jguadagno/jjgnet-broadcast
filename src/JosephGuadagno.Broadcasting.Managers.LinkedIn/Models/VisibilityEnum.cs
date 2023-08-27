namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;

/// <summary>
/// Defines any visibility restrictions for the share
/// </summary>
public enum VisibilityEnum
{
    /// <summary>
    /// The share will be viewable by anyone on LinkedIn
    /// </summary>
    Anyone,
    /// <summary>
    /// The share will be viewable by 1st-degree connections only
    /// </summary>
    ConnectionsOnly
}