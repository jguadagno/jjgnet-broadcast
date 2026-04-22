using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests;

public class EntraClaimsTransformationTests
{
    private readonly Mock<IUserApprovalManager> _mockUserApprovalManager;
    private readonly Mock<ILogger<EntraClaimsTransformation>> _mockLogger;
    private readonly EntraClaimsTransformation _sut;

    public EntraClaimsTransformationTests()
    {
        _mockUserApprovalManager = new Mock<IUserApprovalManager>();
        _mockLogger = new Mock<ILogger<EntraClaimsTransformation>>();
        _sut = new EntraClaimsTransformation(
            _mockUserApprovalManager.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task TransformAsync_WithNewUser_RegistersUserAndAddsClaims()
    {
        // Arrange
        var entraObjectId = "new-oid-12345";
        var displayName = "John Doe";
        var email = "john@example.com";
        
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var newUser = new ApplicationUser
        {
            Id = 1,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Pending.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserApprovalManager
            .Setup(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email))
            .ReturnsAsync(newUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(newUser.Id))
            .ReturnsAsync(new List<Role>());

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
        result.Identity.Should().NotBeNull();
        result.Identity!.IsAuthenticated.Should().BeTrue();
        
        var approvalStatusClaim = result.FindFirst(ApplicationClaimTypes.ApprovalStatus);
        approvalStatusClaim.Should().NotBeNull();
        approvalStatusClaim!.Value.Should().Be(ApprovalStatus.Pending.ToString());
        
        _mockUserApprovalManager.Verify(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email), Times.Once);
        _mockUserApprovalManager.Verify(x => x.GetUserRolesAsync(newUser.Id), Times.Once);
    }

    [Fact]
    public async Task TransformAsync_WithPendingUser_AddsApprovalStatusClaim()
    {
        // Arrange
        var entraObjectId = "pending-oid-67890";
        var displayName = "Jane Smith";
        var email = "jane@example.com";
        
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var pendingUser = new ApplicationUser
        {
            Id = 2,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Pending.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserApprovalManager
            .Setup(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email))
            .ReturnsAsync(pendingUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(pendingUser.Id))
            .ReturnsAsync(new List<Role>());

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
        var approvalStatusClaim = result.FindFirst(ApplicationClaimTypes.ApprovalStatus);
        approvalStatusClaim.Should().NotBeNull();
        approvalStatusClaim!.Value.Should().Be(ApprovalStatus.Pending.ToString());
    }

    [Fact]
    public async Task TransformAsync_WithApprovedUser_AddsRoleClaims()
    {
        // Arrange
        var entraObjectId = "approved-oid-11111";
        var displayName = "Admin User";
        var email = "admin@example.com";
        
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var approvedUser = new ApplicationUser
        {
            Id = 3,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var roles = new List<Role>
        {
            new Role { Id = 1, Name = "Administrator", Description = "Full access" },
            new Role { Id = 2, Name = "Editor", Description = "Can edit content" }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email))
            .ReturnsAsync(approvedUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(approvedUser.Id))
            .ReturnsAsync(roles);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
        
        var approvalStatusClaim = result.FindFirst(ApplicationClaimTypes.ApprovalStatus);
        approvalStatusClaim.Should().NotBeNull();
        approvalStatusClaim!.Value.Should().Be(ApprovalStatus.Approved.ToString());
        
        var roleClaims = result.FindAll(ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(2);
        roleClaims.Should().Contain(c => c.Value == "Administrator");
        roleClaims.Should().Contain(c => c.Value == "Editor");
    }

    [Fact]
    public async Task TransformAsync_WithRejectedUser_AddsRejectedStatusAndApprovalNotesClaims()
    {
        // Arrange
        var entraObjectId = "rejected-oid-22222";
        var displayName = "Rejected User";
        var email = "rejected@example.com";
        var rejectionNotes = "Does not meet requirements";
        
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var rejectedUser = new ApplicationUser
        {
            Id = 4,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Rejected.ToString(),
            ApprovalNotes = rejectionNotes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _mockUserApprovalManager
            .Setup(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email))
            .ReturnsAsync(rejectedUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(rejectedUser.Id))
            .ReturnsAsync(new List<Role>());

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
        
        var approvalStatusClaim = result.FindFirst(ApplicationClaimTypes.ApprovalStatus);
        approvalStatusClaim.Should().NotBeNull();
        approvalStatusClaim!.Value.Should().Be(ApprovalStatus.Rejected.ToString());

        var approvalNotesClaim = result.FindFirst(ApplicationClaimTypes.ApprovalNotes);
        approvalNotesClaim.Should().NotBeNull();
        approvalNotesClaim!.Value.Should().Be(rejectionNotes);
    }

    [Fact]
    public async Task TransformAsync_WithShortFormOidClaim_AddsClaimsAndRoles()
    {
        // Arrange — mirrors the JWT bearer scenario where MI.Web v2+ (JsonWebTokenHandler)
        // delivers "oid" without mapping it to the full URI form.
        var entraObjectId = "short-oid-55555";
        var displayName = "JWT Bearer User";
        var email = "jwt@example.com";

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectIdShort, entraObjectId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var approvedUser = new ApplicationUser
        {
            Id = 5,
            EntraObjectId = entraObjectId,
            DisplayName = displayName,
            Email = email,
            ApprovalStatus = ApprovalStatus.Approved.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var roles = new List<Role>
        {
            new Role { Id = 1, Name = "SiteAdministrator", Description = "Site admin" }
        };

        _mockUserApprovalManager
            .Setup(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email))
            .ReturnsAsync(approvedUser);

        _mockUserApprovalManager
            .Setup(x => x.GetUserRolesAsync(approvedUser.Id))
            .ReturnsAsync(roles);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
        result.FindFirst(ApplicationClaimTypes.ApprovalStatus).Should().NotBeNull()
            .And.Subject.As<Claim>().Value.Should().Be(ApprovalStatus.Approved.ToString());

        result.FindAll(ClaimTypes.Role).Should().ContainSingle(c => c.Value == "SiteAdministrator");

        _mockUserApprovalManager.Verify(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email), Times.Once);
    }

    [Fact]
    public async Task TransformAsync_WithMissingOidClaim_ReturnsOriginalPrincipal()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "User Without OID"),
            new Claim(ClaimTypes.Email, "nooid@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().BeSameAs(principal);
        result.FindFirst(ApplicationClaimTypes.ApprovalStatus).Should().BeNull();
        
        _mockUserApprovalManager.Verify(x => x.GetOrCreateUserAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_WithUnauthenticatedUser_ReturnsOriginalPrincipal()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().BeSameAs(principal);
        
        _mockUserApprovalManager.Verify(x => x.GetOrCreateUserAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_WithAlreadyTransformedPrincipal_ReturnsOriginalPrincipal()
    {
        // Arrange
        var entraObjectId = "test-oid-33333";
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ApplicationClaimTypes.ApprovalStatus, ApprovalStatus.Approved.ToString()) // Already transformed
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().BeSameAs(principal);
        
        _mockUserApprovalManager.Verify(x => x.GetOrCreateUserAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_WithException_ReturnsOriginalPrincipal()
    {
        // Arrange
        var entraObjectId = "error-oid-44444";
        var displayName = "Error User";
        var email = "error@example.com";
        
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _mockUserApprovalManager
            .Setup(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.TransformAsync(principal);

        // Assert
        result.Should().BeSameAs(principal);
        result.FindFirst(ApplicationClaimTypes.ApprovalStatus).Should().BeNull();
        
        _mockUserApprovalManager.Verify(x => x.GetOrCreateUserAsync(entraObjectId, displayName, email), Times.Once);
    }
}
