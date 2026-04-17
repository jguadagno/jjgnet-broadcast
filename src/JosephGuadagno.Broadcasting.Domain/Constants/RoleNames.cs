namespace JosephGuadagno.Broadcasting.Domain.Constants;

/// <summary>
/// Standard role names for the application
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// Site Administrator role - full app admin: user approval, role management, global platform definitions
    /// </summary>
    public const string SiteAdministrator = "Site Administrator";

    /// <summary>
    /// Administrator role - personal content admin: own Message Templates and Platforms
    /// </summary>
    public const string Administrator = "Administrator";
    
    /// <summary>
    /// Contributor role - can create and modify own content
    /// </summary>
    public const string Contributor = "Contributor";
    
    /// <summary>
    /// Viewer role - read-only access to resources
    /// </summary>
    public const string Viewer = "Viewer";
}
