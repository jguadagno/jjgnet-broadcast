namespace JosephGuadagno.Broadcasting.Domain.Constants;

/// <summary>
/// Standard authorization policy names for the application
/// </summary>
public static class AuthorizationPolicyNames
{
    /// <summary>
    /// Policy requiring the site administrator role
    /// </summary>
    public const string RequireSiteAdministrator = nameof(RequireSiteAdministrator);

    /// <summary>
    /// Policy requiring the administrator role or higher
    /// </summary>
    public const string RequireAdministrator = nameof(RequireAdministrator);

    /// <summary>
    /// Policy requiring the contributor role or higher
    /// </summary>
    public const string RequireContributor = nameof(RequireContributor);

    /// <summary>
    /// Policy requiring the viewer role or higher
    /// </summary>
    public const string RequireViewer = nameof(RequireViewer);
}
