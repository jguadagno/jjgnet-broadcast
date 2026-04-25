using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api;
using JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Api.Tests;

public class ClaimsPrincipalExtensionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ClaimsPrincipal BuildPrincipal(
        string? oidFullUri = null,
        string? oidShort = null,
        bool isSiteAdmin = false)
    {
        var claims = new List<Claim>();

        if (oidFullUri is not null)
            claims.Add(new Claim(ApplicationClaimTypes.EntraObjectId, oidFullUri));

        if (oidShort is not null)
            claims.Add(new Claim(ApplicationClaimTypes.EntraObjectIdShort, oidShort));

        if (isSiteAdmin)
            claims.Add(new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    // -------------------------------------------------------------------------
    // GetOwnerOid
    // -------------------------------------------------------------------------

    [Fact]
    public void GetOwnerOid_WhenFullUriClaimPresent_ReturnsOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "full-uri-oid-abc");

        // Act
        var result = principal.GetOwnerOid();

        // Assert
        result.Should().Be("full-uri-oid-abc");
    }

    [Fact]
    public void GetOwnerOid_WhenOnlyShortOidClaimPresent_ReturnsOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidShort: "short-oid-xyz");

        // Act
        var result = principal.GetOwnerOid();

        // Assert
        result.Should().Be("short-oid-xyz");
    }

    [Fact]
    public void GetOwnerOid_WhenBothClaimsPresent_ReturnsFullUriClaimValue()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "full-uri-oid-abc", oidShort: "short-oid-xyz");

        // Act
        var result = principal.GetOwnerOid();

        // Assert
        result.Should().Be("full-uri-oid-abc");
    }

    [Fact]
    public void GetOwnerOid_WhenNoOidClaimPresent_ThrowsInvalidOperationException()
    {
        // Arrange
        var principal = BuildPrincipal(); // no OID claims

        // Act
        var act = () => principal.GetOwnerOid();

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Entra Object ID*");
    }

    // -------------------------------------------------------------------------
    // IsSiteAdministrator
    // -------------------------------------------------------------------------

    [Fact]
    public void IsSiteAdministrator_WhenUserHasSiteAdministratorRole_ReturnsTrue()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "admin-oid-123", isSiteAdmin: true);

        // Act
        var result = principal.IsSiteAdministrator();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSiteAdministrator_WhenUserLacksSiteAdministratorRole_ReturnsFalse()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "user-oid-123", isSiteAdmin: false);

        // Act
        var result = principal.IsSiteAdministrator();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSiteAdministrator_WhenPrincipalHasNoIdentity_ReturnsFalse()
    {
        // Arrange
        var principal = new ClaimsPrincipal(); // unauthenticated, no identities

        // Act
        var result = principal.IsSiteAdministrator();

        // Assert
        result.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // ResolveOwnerOid
    // -------------------------------------------------------------------------

    [Fact]
    public void ResolveOwnerOid_WhenRequestedOidIsNull_ReturnsCallerOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "caller-oid-123");

        // Act
        var result = principal.ResolveOwnerOid(null);

        // Assert
        result.Should().Be("caller-oid-123");
    }

    [Fact]
    public void ResolveOwnerOid_WhenRequestedOidIsEmpty_ReturnsCallerOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "caller-oid-123");

        // Act
        var result = principal.ResolveOwnerOid(string.Empty);

        // Assert
        result.Should().Be("caller-oid-123");
    }

    [Fact]
    public void ResolveOwnerOid_WhenRequestedOidMatchesCallerOid_ReturnsCallerOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "caller-oid-123");

        // Act
        var result = principal.ResolveOwnerOid("caller-oid-123");

        // Assert
        result.Should().Be("caller-oid-123");
    }

    [Fact]
    public void ResolveOwnerOid_WhenRequestedOidMatchesCallerOidCaseInsensitive_ReturnsCallerOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "CALLER-OID-123");

        // Act
        var result = principal.ResolveOwnerOid("caller-oid-123");

        // Assert
        result.Should().Be("CALLER-OID-123");
    }

    [Fact]
    public void ResolveOwnerOid_WhenNonAdminTargetsDifferentOid_ReturnsNull()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "caller-oid-123", isSiteAdmin: false);

        // Act
        var result = principal.ResolveOwnerOid("other-user-oid-999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ResolveOwnerOid_WhenAdminTargetsDifferentOidWithRequireAdmin_ReturnsRequestedOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "admin-oid-123", isSiteAdmin: true);

        // Act
        var result = principal.ResolveOwnerOid("other-user-oid-999", requireAdminWhenTargetingOtherUser: true);

        // Assert
        result.Should().Be("other-user-oid-999");
    }

    [Fact]
    public void ResolveOwnerOid_WhenNonAdminTargetsDifferentOidWithRequireAdminFalse_ReturnsRequestedOid()
    {
        // Arrange
        // requireAdminWhenTargetingOtherUser=false means any caller may target a different OID
        var principal = BuildPrincipal(oidFullUri: "caller-oid-123", isSiteAdmin: false);

        // Act
        var result = principal.ResolveOwnerOid("other-user-oid-999", requireAdminWhenTargetingOtherUser: false);

        // Assert
        result.Should().Be("other-user-oid-999");
    }

    [Fact]
    public void ResolveOwnerOid_WhenAdminAndRequestedOidIsNull_ReturnsCallerOid()
    {
        // Arrange
        var principal = BuildPrincipal(oidFullUri: "admin-oid-123", isSiteAdmin: true);

        // Act
        var result = principal.ResolveOwnerOid(null);

        // Assert
        result.Should().Be("admin-oid-123");
    }
}
