namespace JosephGuadagno.Broadcasting.Domain.Enums;

/// <summary>
/// Represents an action taken on a user's approval status
/// </summary>
public enum ApprovalAction
{
    /// <summary>
    /// User was registered in the system
    /// </summary>
    Registered,
    
    /// <summary>
    /// User was approved by an administrator
    /// </summary>
    Approved,
    
    /// <summary>
    /// User was rejected by an administrator
    /// </summary>
    Rejected,
    
    /// <summary>
    /// A role was assigned to the user
    /// </summary>
    RoleAssigned,
    
    /// <summary>
    /// A role was removed from the user
    /// </summary>
    RoleRemoved
}
