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

public class AdminControllerTests
{
    private readonly Mock<IUserApprovalManager> _mockUserApprovalManager;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AdminController>> _mockLogger;
    private readonly AdminController _sut;
    private const string EntraObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public AdminControllerTests()
    {
        _mockUserApprovalManager = new Mock<IUserApprovalManager>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AdminController>>();
        _sut = new AdminController(
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
        var allUsers = new List<ApplicationUser>
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
            },
            new ApplicationUser
            {
                Id = 2,
                EntraObjectId = "oid2",
                DisplayName = "Approved User",
                Email = "approved@example.com",
                ApprovalStatus = ApprovalStatus.Approved.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
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

        _mockUserApprovalManager
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(allUsers);

        _mockMapper
            .Setup(x => x.Map<List<ApplicationUserViewModel>>(It.Is<List<ApplicationUser>>(
                list => list.All(u => u.ApprovalStatus == ApprovalStatus.Pending.ToString()))))
            .Returns(pendingViewModels);

        _mockMapper
            .Setup(x => x.Map<List<ApplicationUserViewModel>>(It.Is<List<ApplicationUser>>(
                list => list.All(u => u.ApprovalStatus == ApprovalStatus.Approved.ToString()))))
            .Returns(approvedViewModels);

        _mockMapper
            .Setup(x => x.Map<List<ApplicationUserViewModel>>(It.Is<List<ApplicationUser>>(
                list => list.All(u => u.ApprovalStatus == ApprovalStatus.Rejected.ToString()))))
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
        
        _mockUserApprovalManager.Verify(x => x.GetAllUsersAsync(), Times.Once);
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
}
