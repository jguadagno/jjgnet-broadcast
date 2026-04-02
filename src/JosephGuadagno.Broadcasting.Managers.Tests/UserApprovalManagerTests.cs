using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserApprovalManagerTests
{
    private readonly Mock<IApplicationUserDataStore> _mockApplicationUserDataStore;
    private readonly Mock<IRoleDataStore> _mockRoleDataStore;
    private readonly Mock<IUserApprovalLogDataStore> _mockUserApprovalLogDataStore;
    private readonly UserApprovalManager _sut;

    public UserApprovalManagerTests()
    {
        _mockApplicationUserDataStore = new Mock<IApplicationUserDataStore>();
        _mockRoleDataStore = new Mock<IRoleDataStore>();
        _mockUserApprovalLogDataStore = new Mock<IUserApprovalLogDataStore>();
        _sut = new UserApprovalManager(
            _mockApplicationUserDataStore.Object,
            _mockRoleDataStore.Object,
            _mockUserApprovalLogDataStore.Object);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var entraObjectId = "test-oid-12345";
        var displayName = "John Doe";
        var email = "john@example.com";
        var existingUser = new ApplicationUser
        {
            Id = 1,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockApplicationUserDataStore
            .Setup(x => x.GetByEntraObjectIdAsync(entraObjectId))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.GetOrCreateUserAsync(entraObjectId, displayName, email);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.EntraObjectId.Should().Be(entraObjectId);
        _mockApplicationUserDataStore.Verify(x => x.GetByEntraObjectIdAsync(entraObjectId), Times.Once);
        _mockApplicationUserDataStore.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WithNewUser_CreatesAndReturnsUser()
    {
        // Arrange
        var entraObjectId = "new-oid-67890";
        var displayName = "Jane Smith";
        var email = "jane@example.com";
        var createdUser = new ApplicationUser
        {
            Id = 2,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Pending.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockApplicationUserDataStore
            .Setup(x => x.GetByEntraObjectIdAsync(entraObjectId))
            .ReturnsAsync((ApplicationUser?)null);

        _mockApplicationUserDataStore
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(createdUser);

        _mockUserApprovalLogDataStore
            .Setup(x => x.CreateAsync(It.IsAny<UserApprovalLog>()))
            .ReturnsAsync(new UserApprovalLog { Id = 1 });

        // Act
        var result = await _sut.GetOrCreateUserAsync(entraObjectId, displayName, email);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(2);
        result.EntraObjectId.Should().Be(entraObjectId);
        result.ApprovalStatus.Should().Be(ApprovalStatus.Pending.ToString());
        
        _mockApplicationUserDataStore.Verify(x => x.GetByEntraObjectIdAsync(entraObjectId), Times.Once);
        _mockApplicationUserDataStore.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u =>
            u.EntraObjectId == entraObjectId &&
            u.DisplayName == displayName &&
            u.Email == email &&
            u.ApprovalStatus == ApprovalStatus.Pending.ToString())), Times.Once);
        
        _mockUserApprovalLogDataStore.Verify(x => x.CreateAsync(It.Is<UserApprovalLog>(log =>
            log.UserId == createdUser.Id &&
            log.AdminUserId == null &&
            log.Action == ApprovalAction.Registered.ToString() &&
            log.Notes == "User self-registered")), Times.Once);
    }

    [Fact]
    public async Task ApproveUserAsync_WithPendingUser_ApprovesAndLogsAction()
    {
        // Arrange
        var userId = 1;
        var adminUserId = 5;
        var user = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "test-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Pending.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var approvedUser = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "test-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockApplicationUserDataStore
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockApplicationUserDataStore
            .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(approvedUser);

        _mockUserApprovalLogDataStore
            .Setup(x => x.CreateAsync(It.IsAny<UserApprovalLog>()))
            .ReturnsAsync(new UserApprovalLog { Id = 1 });

        // Act
        var result = await _sut.ApproveUserAsync(userId, adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.ApprovalStatus.Should().Be(ApprovalStatus.Approved.ToString());
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockApplicationUserDataStore.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u =>
            u.Id == userId &&
            u.ApprovalStatus == ApprovalStatus.Approved.ToString())), Times.Once);
        
        _mockUserApprovalLogDataStore.Verify(x => x.CreateAsync(It.Is<UserApprovalLog>(log =>
            log.UserId == userId &&
            log.AdminUserId == adminUserId &&
            log.Action == ApprovalAction.Approved.ToString() &&
            log.Notes == "User approved by administrator")), Times.Once);
    }

    [Fact]
    public async Task ApproveUserAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var userId = 999;
        var adminUserId = 5;

        _mockApplicationUserDataStore
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Func<Task> act = async () => await _sut.ApproveUserAsync(userId, adminUserId);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage($"User with id '{userId}' not found");
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockApplicationUserDataStore.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserApprovalLogDataStore.Verify(x => x.CreateAsync(It.IsAny<UserApprovalLog>()), Times.Never);
    }

    [Fact]
    public async Task RejectUserAsync_WithPendingUser_RejectsAndLogsAction()
    {
        // Arrange
        var userId = 1;
        var adminUserId = 5;
        var rejectionNotes = "Account does not meet requirements";
        var user = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "test-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Pending.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var rejectedUser = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "test-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Rejected.ToString(),
            ApprovalNotes = rejectionNotes,
            CreatedAt = user.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockApplicationUserDataStore
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockApplicationUserDataStore
            .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(rejectedUser);

        _mockUserApprovalLogDataStore
            .Setup(x => x.CreateAsync(It.IsAny<UserApprovalLog>()))
            .ReturnsAsync(new UserApprovalLog { Id = 1 });

        // Act
        var result = await _sut.RejectUserAsync(userId, adminUserId, rejectionNotes);

        // Assert
        result.Should().NotBeNull();
        result.ApprovalStatus.Should().Be(ApprovalStatus.Rejected.ToString());
        result.ApprovalNotes.Should().Be(rejectionNotes);
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockApplicationUserDataStore.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u =>
            u.Id == userId &&
            u.ApprovalStatus == ApprovalStatus.Rejected.ToString() &&
            u.ApprovalNotes == rejectionNotes)), Times.Once);
        
        _mockUserApprovalLogDataStore.Verify(x => x.CreateAsync(It.Is<UserApprovalLog>(log =>
            log.UserId == userId &&
            log.AdminUserId == adminUserId &&
            log.Action == ApprovalAction.Rejected.ToString() &&
            log.Notes == rejectionNotes)), Times.Once);
    }

    [Fact]
    public async Task RejectUserAsync_WithNullNotes_ThrowsArgumentException()
    {
        // Arrange
        var userId = 1;
        var adminUserId = 5;
        string rejectionNotes = null!;

        // Act
        Func<Task> act = async () => await _sut.RejectUserAsync(userId, adminUserId, rejectionNotes);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("rejectionNotes");
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockApplicationUserDataStore.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserApprovalLogDataStore.Verify(x => x.CreateAsync(It.IsAny<UserApprovalLog>()), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_WithValidUserAndRole_AssignsRole()
    {
        // Arrange
        var userId = 1;
        var roleId = 2;
        var adminUserId = 5;
        var user = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "test-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var role = new Role
        {
            Id = roleId,
            Name = "Editor",
            Description = "Can edit content"
        };

        _mockApplicationUserDataStore
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockRoleDataStore
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync(role);

        _mockRoleDataStore
            .Setup(x => x.AssignRoleToUserAsync(userId, roleId))
            .ReturnsAsync(true);

        _mockUserApprovalLogDataStore
            .Setup(x => x.CreateAsync(It.IsAny<UserApprovalLog>()))
            .ReturnsAsync(new UserApprovalLog { Id = 1 });

        // Act
        var result = await _sut.AssignRoleAsync(userId, roleId, adminUserId);

        // Assert
        result.Should().BeTrue();
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRoleDataStore.Verify(x => x.GetByIdAsync(roleId), Times.Once);
        _mockRoleDataStore.Verify(x => x.AssignRoleToUserAsync(userId, roleId), Times.Once);
        
        _mockUserApprovalLogDataStore.Verify(x => x.CreateAsync(It.Is<UserApprovalLog>(log =>
            log.UserId == userId &&
            log.AdminUserId == adminUserId &&
            log.Action == ApprovalAction.RoleAssigned.ToString() &&
            log.Notes == $"Role '{role.Name}' assigned")), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var userId = 999;
        var roleId = 2;
        var adminUserId = 5;

        _mockApplicationUserDataStore
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Func<Task> act = async () => await _sut.AssignRoleAsync(userId, roleId, adminUserId);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage($"User with id '{userId}' not found");
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRoleDataStore.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockRoleDataStore.Verify(x => x.AssignRoleToUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_WithNonExistentRole_ThrowsException()
    {
        // Arrange
        var userId = 1;
        var roleId = 999;
        var adminUserId = 5;
        var user = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "test-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockApplicationUserDataStore
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockRoleDataStore
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync((Role?)null);

        // Act
        Func<Task> act = async () => await _sut.AssignRoleAsync(userId, roleId, adminUserId);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage($"Role with id '{roleId}' not found");
        
        _mockApplicationUserDataStore.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockRoleDataStore.Verify(x => x.GetByIdAsync(roleId), Times.Once);
        _mockRoleDataStore.Verify(x => x.AssignRoleToUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithExistingUser_ReturnsRoles()
    {
        // Arrange
        var userId = 1;
        var roles = new List<Role>
        {
            new Role { Id = 1, Name = "Administrator", Description = "Full access" },
            new Role { Id = 2, Name = "Editor", Description = "Can edit content" }
        };

        _mockRoleDataStore
            .Setup(x => x.GetRolesForUserAsync(userId))
            .ReturnsAsync(roles);

        // Act
        var result = await _sut.GetUserRolesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Administrator");
        result.Should().Contain(r => r.Name == "Editor");
        
        _mockRoleDataStore.Verify(x => x.GetRolesForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithUserWithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        _mockRoleDataStore
            .Setup(x => x.GetRolesForUserAsync(userId))
            .ReturnsAsync(new List<Role>());

        // Act
        var result = await _sut.GetUserRolesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        
        _mockRoleDataStore.Verify(x => x.GetRolesForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithInvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var userId = 0;

        // Act
        Func<Task> act = async () => await _sut.GetUserRolesAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("userId");
        
        _mockRoleDataStore.Verify(x => x.GetRolesForUserAsync(It.IsAny<int>()), Times.Never);
    }
}
