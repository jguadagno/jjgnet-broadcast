using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for user approval operations
/// </summary>
public class UserApprovalManager(
    IApplicationUserDataStore applicationUserDataStore,
    IRoleDataStore roleDataStore,
    IUserApprovalLogDataStore userApprovalLogDataStore,
    IEmailTemplateManager emailTemplateManager,
    IEmailSender emailSender,
    ILogger<UserApprovalManager> logger) : IUserApprovalManager
{
    public async Task<ApplicationUser> GetOrCreateUserAsync(string entraObjectId, string displayName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var existingUser = await applicationUserDataStore.GetByEntraObjectIdAsync(entraObjectId);
        if (existingUser is not null)
        {
            return existingUser;
        }

        // Create new user with Pending status
        var newUser = new ApplicationUser
        {
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Pending.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdUser = await applicationUserDataStore.CreateAsync(newUser);

        // Log the registration
        await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
        {
            UserId = createdUser.Id,
            AdminUserId = null,
            Action = ApprovalAction.Registered.ToString(),
            Notes = "User self-registered",
            CreatedAt = DateTimeOffset.UtcNow
        });

        return createdUser;
    }

    public async Task<ApplicationUser?> GetUserAsync(string entraObjectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);
        return await applicationUserDataStore.GetByEntraObjectIdAsync(entraObjectId);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(int userId)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        return await applicationUserDataStore.GetByIdAsync(userId);
    }

    public async Task<List<ApplicationUser>> GetPendingUsersAsync()
    {
        return await applicationUserDataStore.GetByApprovalStatusAsync(ApprovalStatus.Pending.ToString());
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return await applicationUserDataStore.GetAllAsync();
    }

    public async Task<List<ApplicationUser>> GetUsersByStatusAsync(ApprovalStatus status)
    {
        return await applicationUserDataStore.GetByApprovalStatusAsync(status.ToString());
    }

    public async Task<ApplicationUser> ApproveUserAsync(int userId, int adminUserId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        if (adminUserId <= 0)
        {
            throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));
        }

        var user = await applicationUserDataStore.GetByIdAsync(userId);
        if (user is null)
        {
            throw new ApplicationException($"User with id '{userId}' not found");
        }

        user.ApprovalStatus = ApprovalStatus.Approved.ToString();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedUser = await applicationUserDataStore.UpdateAsync(user);

        // Log the approval
        await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
        {
            UserId = userId,
            AdminUserId = adminUserId,
            Action = ApprovalAction.Approved.ToString(),
            Notes = "User approved by administrator",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await TrySendEmailNotificationAsync(updatedUser, "UserApproved");

        return updatedUser;
    }

    public async Task<ApplicationUser> RejectUserAsync(int userId, int adminUserId, string rejectionNotes)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        if (adminUserId <= 0)
        {
            throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rejectionNotes, nameof(rejectionNotes));

        var user = await applicationUserDataStore.GetByIdAsync(userId);
        if (user is null)
        {
            throw new ApplicationException($"User with id '{userId}' not found");
        }

        user.ApprovalStatus = ApprovalStatus.Rejected.ToString();
        user.ApprovalNotes = rejectionNotes;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedUser = await applicationUserDataStore.UpdateAsync(user);

        // Log the rejection
        await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
        {
            UserId = userId,
            AdminUserId = adminUserId,
            Action = ApprovalAction.Rejected.ToString(),
            Notes = rejectionNotes,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await TrySendEmailNotificationAsync(updatedUser, "UserRejected");

        return updatedUser;
    }

    public async Task<bool> AssignRoleAsync(int userId, int roleId, int adminUserId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        if (roleId <= 0)
        {
            throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));
        }

        if (adminUserId <= 0)
        {
            throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));
        }

        // Verify user exists
        var user = await applicationUserDataStore.GetByIdAsync(userId);
        if (user is null)
        {
            throw new ApplicationException($"User with id '{userId}' not found");
        }

        // Verify role exists
        var role = await roleDataStore.GetByIdAsync(roleId);
        if (role is null)
        {
            throw new ApplicationException($"Role with id '{roleId}' not found");
        }

        var result = await roleDataStore.AssignRoleToUserAsync(userId, roleId);

        if (result)
        {
            // Log the role assignment
            await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
            {
                UserId = userId,
                AdminUserId = adminUserId,
                Action = ApprovalAction.RoleAssigned.ToString(),
                Notes = $"Role '{role.Name}' assigned",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return result;
    }

    public async Task<bool> RemoveRoleAsync(int userId, int roleId, int adminUserId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        if (roleId <= 0)
        {
            throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));
        }

        if (adminUserId <= 0)
        {
            throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));
        }

        // Verify user exists
        var user = await applicationUserDataStore.GetByIdAsync(userId);
        if (user is null)
        {
            throw new ApplicationException($"User with id '{userId}' not found");
        }

        // Verify role exists
        var role = await roleDataStore.GetByIdAsync(roleId);
        if (role is null)
        {
            throw new ApplicationException($"Role with id '{roleId}' not found");
        }

        var result = await roleDataStore.RemoveRoleFromUserAsync(userId, roleId);

        if (result)
        {
            // Log the role removal
            await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
            {
                UserId = userId,
                AdminUserId = adminUserId,
                Action = ApprovalAction.RoleRemoved.ToString(),
                Notes = $"Role '{role.Name}' removed",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return result;
    }

    public async Task<List<Role>> GetUserRolesAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        return await roleDataStore.GetRolesForUserAsync(userId);
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await roleDataStore.GetAllAsync();
    }

    public async Task<List<UserApprovalLog>> GetUserAuditLogAsync(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        }

        return await userApprovalLogDataStore.GetByUserIdAsync(userId);
    }

    private async Task TrySendEmailNotificationAsync(ApplicationUser user, string templateName)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("Cannot send '{TemplateName}' email: user {UserId} has no email address.", templateName, user.Id);
            return;
        }

        var template = await emailTemplateManager.GetTemplateAsync(templateName);
        if (template is null)
        {
            logger.LogWarning("Email template '{TemplateName}' not found. Skipping notification email for user {UserId}.", templateName, user.Id);
            return;
        }

        try
        {
            var toAddress = new MailAddress(user.Email, user.DisplayName ?? user.Email);
            await emailSender.QueueEmail(toAddress, template.Subject, template.Body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to queue '{TemplateName}' email for user {UserId}. The approval action was still processed.", templateName, user.Id);
        }
    }
}
