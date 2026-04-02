namespace JosephGuadagno.Broadcasting.Domain.Constants;

/// <summary>
/// Standard role names for the application
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// Administrator role - full access to all resources
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
