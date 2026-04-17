using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class SiteAdminControllerTests
{
    private readonly Mock<IUserApprovalManager> _mockUserApprovalManager;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<SiteAdminController>> _mockLogger;
    private readonly SiteAdminController _sut;
    private const string EntraObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public SiteAdminControllerTests()
    {
        _mockUserApprovalManager = new Mock<IUserApprovalManager>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<SiteAdminController>>();
        _sut = new SiteAdminController(
            _mockUserApprovalManager.Object,
            _mockMapper.Object,
            _mockLogger.Object);

        // Setup TempData for controller
        _sut.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Users_ReturnsViewWithUserList()
    {
        // Arrange
        var pendingUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = 1,
                EntraObjectId = "oid1",
                DisplayName = "Pending User",
                Email = "pending@example.com",
                ApprovalStatus = ApprovalStatus.Pending.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var approvedUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = 2,
                EntraObjectId = "oid2",
                DisplayName = "Approved User",
                Email = "approved@example.com",
                ApprovalStatus = ApprovalStatus.Approved.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var rejectedUsers = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = 3,
                EntraObjectId = "oid3",
                DisplayName = "Rejected User",
                Email = "rejected@example.com",
                ApprovalStatus = ApprovalStatus.Rejected.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var pendingViewModels = new List<ApplicationUserViewModel>
        {
            new ApplicationUserViewModel { Id = 1, DisplayName = "Pending User", ApprovalStatus = ApprovalStatus.Pending.ToString() }
        };

        var approvedViewModels = new List<ApplicationUserViewModel>
        {
            new ApplicationUserViewModel { Id = 2, DisplayName = "Approved User", ApprovalStatus = ApprovalStatus.Approved.ToString() }
        };

        var rejectedViewModels = new List<ApplicationUserViewModel>
        {
            new ApplicationUserViewModel { Id = 3, DisplayName = "Rejected User", ApprovalStatus = ApprovalStatus.Rejected.ToString() }
        };

        // Mock the status-specific methods (DB-level filtering)
        _mockUserApprovalManager
            .Setup(x => x.GetUsersByStatusAsync(ApprovalStatus.Pending))
            .ReturnsAsync(pendingUsers);

        _mockUserApprovalManager
            .Setup(x => x.GetUsersByStatusAsync(ApprovalStatus.Approved))
            .ReturnsAsync(approvedUsers);

        _mockUserApprovalManager
            .Setup(x => x.GetUsersByStatusAsync(ApprovalStatus.Rejected))
            .ReturnsAsync(rejectedUsers);

        _mockMapper
            .Setup(x => x.Map<List<ApplicationUserViewModel>>(pendingUsers))
            .Returns(pendingViewModels);

        _mockMapper
            .Setup(x => x.Map<List<ApplicationUserViewModel>>(approvedUsers))
            .Returns(approvedViewModels);

        _mockMapper
            .Setup(x => x.Map<List<ApplicationUserViewModel>>(rejectedUsers))
            .Returns(rejectedViewModels);

        // Act
        var result = await _sut.Users();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        
        var model = viewResult!.Model as UserListViewModel;
        model.Should().NotBeNull();
        model!.PendingUsers.Should().HaveCount(1);
        model.ApprovedUsers.Should().HaveCount(1);
        model.RejectedUsers.Should().HaveCount(1);
        
        // Verify DB-level filtering is used
        _mockUserApprovalManager.Verify(x => x.GetUsersByStatusAsync(ApprovalStatus.Pending), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUsersByStatusAsync(ApprovalStatus.Approved), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUsersByStatusAsync(ApprovalStatus.Rejected), Times.Once);
    }

    [Fact]
    public async Task ApproveUser_WithValidUser_RedirectsToUsers()
    {
        // Arrange
        var userId = 1;
        var adminUserId = 5;
        var adminEntraOid = "admin-oid-12345";
        
        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var approvedUser = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "user-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.ApproveUserAsync(userId, adminUserId))
            .ReturnsAsync(approvedUser);

        // Act
        var result = await _sut.ApproveUser(userId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");
        
        _sut.TempData["SuccessMessage"].Should().Be("User approved successfully.");
        
        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.ApproveUserAsync(userId, adminUserId), Times.Once);
    }

    [Fact]
    public async Task RejectUser_WithValidUserAndNotes_RedirectsToUsers()
    {
        // Arrange
        var userId = 1;
        var adminUserId = 5;
        var adminEntraOid = "admin-oid-67890";
        var rejectionNotes = "Does not meet requirements";
        
        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var rejectedUser = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "user-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Rejected.ToString(),
            ApprovalNotes = rejectionNotes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.RejectUserAsync(userId, adminUserId, rejectionNotes))
            .ReturnsAsync(rejectedUser);

        // Act
        var result = await _sut.RejectUser(userId, rejectionNotes);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");
        
        _sut.TempData["SuccessMessage"].Should().Be("User rejected successfully.");
        
        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.RejectUserAsync(userId, adminUserId, rejectionNotes), Times.Once);
    }

    [Fact]
    public async Task RejectUser_WithEmptyNotes_ReturnsRedirectWithError()
    {
        // Arrange
        var userId = 1;
        var rejectionNotes = string.Empty;

        // Act
        var result = await _sut.RejectUser(userId, rejectionNotes);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");
        
        _sut.TempData["ErrorMessage"].Should().Be("Rejection notes are required.");
        
        _mockUserApprovalManager.Verify(x => x.GetUserAsync(It.IsAny<string>()), Times.Never);
        _mockUserApprovalManager.Verify(x => x.RejectUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RejectUser_WithWhitespaceNotes_ReturnsRedirectWithError()
    {
        // Arrange
        var userId = 1;
        var rejectionNotes = "   ";

        // Act
        var result = await _sut.RejectUser(userId, rejectionNotes);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");
        
        _sut.TempData["ErrorMessage"].Should().Be("Rejection notes are required.");
        
        _mockUserApprovalManager.Verify(x => x.GetUserAsync(It.IsAny<string>()), Times.Never);
        _mockUserApprovalManager.Verify(x => x.RejectUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApproveUser_WithMissingAdminUser_ReturnsRedirectWithError()
    {
        // Arrange
        var userId = 1;
        var adminEntraOid = "missing-admin-oid";
        
        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.ApproveUser(userId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");
        
        _sut.TempData["ErrorMessage"].Should().Be("Could not identify current administrator.");
        
        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.ApproveUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ManageRoles_WithValidUser_ReturnsViewWithViewModel()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "user-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var currentRoles = new List<Role>
        {
            new Role { Id = 1, Name = "Contributor", Description = "Can contribute" }
        };

        var allRoles = new List<Role>
        {
            new Role { Id = 1, Name = "Contributor", Description = "Can contribute" },
            new Role { Id = 2, Name = "Site Administrator", Description = "Full access" }
        };

        var currentRoleViewModels = new List<RoleViewModel>
        {
            new RoleViewModel { Id = 1, Name = "Contributor", Description = "Can contribute" }
        };

        var availableRoleViewModels = new List<RoleViewModel>
        {
            new RoleViewModel { Id = 2, Name = "Site Administrator", Description = "Full access" }
        };

        var userViewModel = new ApplicationUserViewModel { Id = userId, DisplayName = "Test User" };

        _mockUserApprovalManager
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(currentRoles);

        _mockUserApprovalManager
            .Setup(x => x.GetAllRolesAsync())
            .ReturnsAsync(allRoles);

        _mockMapper
            .Setup(x => x.Map<ApplicationUserViewModel>(user))
            .Returns(userViewModel);

        _mockMapper
            .Setup(x => x.Map<List<RoleViewModel>>(currentRoles))
            .Returns(currentRoleViewModels);

        _mockMapper
            .Setup(x => x.Map<List<RoleViewModel>>(It.Is<List<Role>>(r => r.Count == 1 && r.First().Id == 2)))
            .Returns(availableRoleViewModels);

        // Act
        var result = await _sut.ManageRoles(userId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();

        var model = viewResult!.Model as ManageRolesViewModel;
        model.Should().NotBeNull();
        model!.User.Should().Be(userViewModel);
        model.CurrentRoles.Should().HaveCount(1);
        model.AvailableRoles.Should().HaveCount(1);
        model.AvailableRoles.First().Id.Should().Be(2);

        _mockUserApprovalManager.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetAllRolesAsync(), Times.Once);
    }

    [Fact]
    public async Task ManageRoles_WithInvalidUser_RedirectsToUsers()
    {
        // Arrange
        var userId = 999;

        _mockUserApprovalManager
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.ManageRoles(userId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");

        _sut.TempData["ErrorMessage"].Should().Be("User not found.");

        _mockUserApprovalManager.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AssignRole_WithValidAdmin_AssignsRoleSuccessfully()
    {
        // Arrange
        var userId = 1;
        var roleId = 2;
        var adminUserId = 5;
        var adminEntraOid = "admin-oid-12345";

        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.AssignRoleAsync(userId, roleId, adminUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AssignRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("ManageRoles");
        redirectResult.RouteValues.Should().ContainKey("userId");
        redirectResult.RouteValues!["userId"].Should().Be(userId);

        _sut.TempData["SuccessMessage"].Should().Be("Role assigned successfully.");

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.AssignRoleAsync(userId, roleId, adminUserId), Times.Once);
    }

    [Fact]
    public async Task AssignRole_WithMissingAdmin_ReturnsRedirectWithError()
    {
        // Arrange
        var userId = 1;
        var roleId = 2;
        var adminEntraOid = "missing-admin-oid";

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.AssignRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");

        _sut.TempData["ErrorMessage"].Should().Be("Could not identify current administrator.");

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRole_WithValidAdmin_RemovesRoleSuccessfully()
    {
        // Arrange
        var userId = 1;
        var roleId = 2;
        var adminUserId = 5;
        var adminEntraOid = "admin-oid-67890";

        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.RemoveRoleAsync(userId, roleId, adminUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RemoveRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("ManageRoles");
        redirectResult.RouteValues.Should().ContainKey("userId");
        redirectResult.RouteValues!["userId"].Should().Be(userId);

        _sut.TempData["SuccessMessage"].Should().Be("Role removed successfully.");

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.RemoveRoleAsync(userId, roleId, adminUserId), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_WithMissingAdmin_ReturnsRedirectWithError()
    {
        // Arrange
        var userId = 1;
        var roleId = 2;
        var adminEntraOid = "missing-admin-oid";

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.RemoveRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("Users");

        _sut.TempData["ErrorMessage"].Should().Be("Could not identify current administrator.");

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.RemoveRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRole_WhenAdminRemovesOwnSiteAdministratorRole_ReturnsRedirectWithError()
    {
        // Arrange
        var adminUserId = 5;
        var userId = 5; // Same user (self)
        var roleId = 1;
        var adminEntraOid = "admin-oid-12345";

        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var userRoles = new List<Role>
        {
            new Role { Id = 1, Name = "Site Administrator", Description = "Full access" }
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(userRoles);

        // Act
        var result = await _sut.RemoveRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("ManageRoles");
        redirectResult.RouteValues.Should().ContainKey("userId");
        redirectResult.RouteValues!["userId"].Should().Be(userId);

        _sut.TempData["ErrorMessage"].Should().NotBeNull();

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.RemoveRoleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRole_WhenAdminRemovesOwnNonSiteAdministratorRole_ProceedsNormally()
    {
        // Arrange
        var adminUserId = 5;
        var userId = 5; // Same user (self)
        var roleId = 2;
        var adminEntraOid = "admin-oid-12345";

        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var userRoles = new List<Role>
        {
            new Role { Id = 2, Name = "Contributor", Description = "Can contribute" }
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(userRoles);

        _mockUserApprovalManager
            .Setup(x => x.RemoveRoleAsync(userId, roleId, adminUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RemoveRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("ManageRoles");

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.RemoveRoleAsync(userId, roleId, adminUserId), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_WhenAdminRemovesDifferentUsersAdministratorRole_ProceedsNormally()
    {
        // Arrange
        var adminUserId = 5;
        var userId = 10; // Different user
        var roleId = 1;
        var adminEntraOid = "admin-oid-12345";

        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            EntraObjectId = adminEntraOid,
            DisplayName = "Admin User",
            Email = "admin@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var claims = new List<Claim>
        {
            new Claim(EntraObjectIdClaimType, adminEntraOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetUserAsync(adminEntraOid))
            .ReturnsAsync(adminUser);

        _mockUserApprovalManager
            .Setup(x => x.RemoveRoleAsync(userId, roleId, adminUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RemoveRole(userId, roleId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.Should().NotBeNull();
        redirectResult!.ActionName.Should().Be("ManageRoles");

        _mockUserApprovalManager.Verify(x => x.GetUserAsync(adminEntraOid), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(It.IsAny<int>()), Times.Never);
        _mockUserApprovalManager.Verify(x => x.RemoveRoleAsync(userId, roleId, adminUserId), Times.Once);
    }

    [Fact]
    public async Task ManageRoles_MapsRolesToRoleViewModel()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            EntraObjectId = "user-oid",
            DisplayName = "Test User",
            Email = "test@example.com",
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var currentRoles = new List<Role>
        {
            new Role { Id = 1, Name = "Contributor", Description = "Can contribute" }
        };

        var allRoles = new List<Role>
        {
            new Role { Id = 1, Name = "Contributor", Description = "Can contribute" },
            new Role { Id = 2, Name = "Site Administrator", Description = "Full access" }
        };

        var currentRoleViewModels = new List<RoleViewModel>
        {
            new RoleViewModel { Id = 1, Name = "Contributor", Description = "Can contribute" }
        };

        var allRoleViewModels = new List<RoleViewModel>
        {
            new RoleViewModel { Id = 1, Name = "Contributor", Description = "Can contribute" },
            new RoleViewModel { Id = 2, Name = "Site Administrator", Description = "Full access" }
        };

        var userViewModel = new ApplicationUserViewModel { Id = userId, DisplayName = "Test User" };

        _mockUserApprovalManager
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(userId))
            .ReturnsAsync(currentRoles);

        _mockUserApprovalManager
            .Setup(x => x.GetAllRolesAsync())
            .ReturnsAsync(allRoles);

        _mockMapper
            .Setup(x => x.Map<ApplicationUserViewModel>(user))
            .Returns(userViewModel);

        _mockMapper
            .Setup(x => x.Map<List<RoleViewModel>>(currentRoles))
            .Returns(currentRoleViewModels);

        _mockMapper
            .Setup(x => x.Map<List<RoleViewModel>>(It.Is<List<Role>>(r => r.Count == 1 && r.First().Id == 2)))
            .Returns(new List<RoleViewModel> { new RoleViewModel { Id = 2, Name = "Site Administrator", Description = "Full access" } });

        // Act
        var result = await _sut.ManageRoles(userId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();

        var model = viewResult!.Model as ManageRolesViewModel;
        model.Should().NotBeNull();
        model!.User.Should().Be(userViewModel);
        model.CurrentRoles.Should().BeOfType<List<RoleViewModel>>();
        model.CurrentRoles.Should().HaveCount(1);
        model.CurrentRoles.First().Should().BeOfType<RoleViewModel>();
        model.AvailableRoles.Should().BeOfType<List<RoleViewModel>>();
        model.AvailableRoles.Should().HaveCount(1);
        model.AvailableRoles.First().Should().BeOfType<RoleViewModel>();
        model.AvailableRoles.First().Id.Should().Be(2);

        _mockUserApprovalManager.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(userId), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetAllRolesAsync(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<RoleViewModel>>(currentRoles), Times.Once);
        _mockMapper.Verify(x => x.Map<List<RoleViewModel>>(It.IsAny<List<Role>>()), Times.AtLeastOnce);
    }
}
