namespace JosephGuadagno.Broadcasting.Domain.Constants;

/// <summary>
/// Claim type constants for Microsoft Entra ID and application-specific claims
/// </summary>
public static class ApplicationClaimTypes
{
    /// <summary>
    /// Microsoft Entra object identifier claim type
    /// </summary>
    public const string EntraObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// Application approval status claim type
    /// </summary>
    public const string ApprovalStatus = "approval_status";

    /// <summary>
    /// Application approval notes claim type (populated for rejected users)
    /// </summary>
    public const string ApprovalNotes = "approval_notes";
}
