namespace JosephGuadagno.Broadcasting.Domain.Enums;

/// <summary>
/// Represents the approval status of an application user
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// User has registered but is awaiting administrator approval
    /// </summary>
    Pending,
    
    /// <summary>
    /// User has been approved by an administrator
    /// </summary>
    Approved,
    
    /// <summary>
    /// User registration has been rejected by an administrator
    /// </summary>
    Rejected
}
