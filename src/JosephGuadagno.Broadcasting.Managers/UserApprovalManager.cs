using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
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
    public async Task<ApplicationUser> GetOrCreateUserAsync(string entraObjectId, string displayName, string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var existingUser = await applicationUserDataStore.GetByEntraObjectIdAsync(entraObjectId, cancellationToken);
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

        var createdUser = await applicationUserDataStore.CreateAsync(newUser, cancellationToken);

        // Log the registration
        await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
        {
            UserId = createdUser.Id,
            AdminUserId = null,
            Action = ApprovalAction.Registered.ToString(),
            Notes = "User self-registered",
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return createdUser;
    }

    public async Task<ApplicationUser?> GetUserAsync(string entraObjectId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraObjectId);
        return await applicationUserDataStore.GetByEntraObjectIdAsync(entraObjectId, cancellationToken);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        return await applicationUserDataStore.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<List<ApplicationUser>> GetPendingUsersAsync(CancellationToken cancellationToken = default)
    {
        return await applicationUserDataStore.GetByApprovalStatusAsync(ApprovalStatus.Pending.ToString(), cancellationToken);
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await applicationUserDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<List<ApplicationUser>> GetUsersByStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
    {
        return await applicationUserDataStore.GetByApprovalStatusAsync(status.ToString(), cancellationToken);
    }

    public async Task<ApplicationUser> ApproveUserAsync(int userId, int adminUserId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        if (adminUserId <= 0) throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));

        var user = await applicationUserDataStore.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new ApplicationException($"User with id '{userId}' not found");

        user.ApprovalStatus = ApprovalStatus.Approved.ToString();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedUser = await applicationUserDataStore.UpdateAsync(user, cancellationToken);

        await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
        {
            UserId = userId,
            AdminUserId = adminUserId,
            Action = ApprovalAction.Approved.ToString(),
            Notes = "User approved by administrator",
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await TrySendEmailNotificationAsync(updatedUser, "UserApproved", cancellationToken);
        return updatedUser;
    }

    public async Task<ApplicationUser> RejectUserAsync(int userId, int adminUserId, string rejectionNotes, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        if (adminUserId <= 0) throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(rejectionNotes, nameof(rejectionNotes));

        var user = await applicationUserDataStore.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new ApplicationException($"User with id '{userId}' not found");

        user.ApprovalStatus = ApprovalStatus.Rejected.ToString();
        user.ApprovalNotes = rejectionNotes;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedUser = await applicationUserDataStore.UpdateAsync(user, cancellationToken);

        await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
        {
            UserId = userId,
            AdminUserId = adminUserId,
            Action = ApprovalAction.Rejected.ToString(),
            Notes = rejectionNotes,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await TrySendEmailNotificationAsync(updatedUser, "UserRejected", cancellationToken);
        return updatedUser;
    }

    public async Task<bool> AssignRoleAsync(int userId, int roleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        if (roleId <= 0) throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));
        if (adminUserId <= 0) throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));

        var user = await applicationUserDataStore.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new ApplicationException($"User with id '{userId}' not found");

        var role = await roleDataStore.GetByIdAsync(roleId, cancellationToken);
        if (role is null) throw new ApplicationException($"Role with id '{roleId}' not found");

        var result = await roleDataStore.AssignRoleToUserAsync(userId, roleId, cancellationToken);

        if (result)
        {
            await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
            {
                UserId = userId,
                AdminUserId = adminUserId,
                Action = ApprovalAction.RoleAssigned.ToString(),
                Notes = $"Role '{role.Name}' assigned",
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return result;
    }

    public async Task<bool> RemoveRoleAsync(int userId, int roleId, int adminUserId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        if (roleId <= 0) throw new ArgumentException("Role ID must be greater than 0", nameof(roleId));
        if (adminUserId <= 0) throw new ArgumentException("Admin user ID must be greater than 0", nameof(adminUserId));

        var user = await applicationUserDataStore.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new ApplicationException($"User with id '{userId}' not found");

        var role = await roleDataStore.GetByIdAsync(roleId, cancellationToken);
        if (role is null) throw new ApplicationException($"Role with id '{roleId}' not found");

        var result = await roleDataStore.RemoveRoleFromUserAsync(userId, roleId, cancellationToken);

        if (result)
        {
            await userApprovalLogDataStore.CreateAsync(new UserApprovalLog
            {
                UserId = userId,
                AdminUserId = adminUserId,
                Action = ApprovalAction.RoleRemoved.ToString(),
                Notes = $"Role '{role.Name}' removed",
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return result;
    }

    public async Task<List<Role>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        return await roleDataStore.GetRolesForUserAsync(userId, cancellationToken);
    }

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await roleDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<List<UserApprovalLog>> GetUserAuditLogAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentException("User ID must be greater than 0", nameof(userId));
        return await userApprovalLogDataStore.GetByUserIdAsync(userId, cancellationToken);
    }

    private async Task TrySendEmailNotificationAsync(ApplicationUser user, string templateName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("Cannot send '{TemplateName}' email: user {UserId} has no email address.", templateName, user.Id);
            return;
        }

        var template = await emailTemplateManager.GetTemplateAsync(templateName, cancellationToken);
        if (template is null)
        {
            logger.LogWarning("Email template '{TemplateName}' not found. Skipping notification email for user {UserId}.", templateName, user.Id);
            return;
        }

        try
        {
            var toAddress = new MailAddress(user.Email, user.DisplayName ?? user.Email);
            await emailSender.QueueEmail(toAddress, template.Subject, template.Body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to queue '{TemplateName}' email for user {UserId}. The approval action was still processed.", templateName, user.Id);
        }
    }
}
