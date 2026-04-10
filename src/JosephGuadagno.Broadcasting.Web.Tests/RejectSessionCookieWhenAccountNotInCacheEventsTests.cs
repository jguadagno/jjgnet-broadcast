using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests;

public class RejectSessionCookieWhenAccountNotInCacheEventsTests
{
    private readonly RejectSessionCookieWhenAccountNotInCacheEvents _sut;
    private Mock<ITokenAcquisition> _mockTokenAcquisition;
    private Mock<IAuthenticationService> _mockAuthService;

    public RejectSessionCookieWhenAccountNotInCacheEventsTests()
    {
        _sut = new RejectSessionCookieWhenAccountNotInCacheEvents();
        _mockTokenAcquisition = new Mock<ITokenAcquisition>();
        _mockAuthService = new Mock<IAuthenticationService>();
    }

    private CookieValidatePrincipalContext CreateContext(ClaimsPrincipal? principal = null)
    {
        principal ??= CreateAuthenticatedPrincipal();

        var services = new ServiceCollection();
        services.AddSingleton(_mockTokenAcquisition.Object);
        services.AddSingleton(_mockAuthService.Object);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            null,
            typeof(CookieAuthenticationHandler));
        var options = new CookieAuthenticationOptions();
        var ticket = new AuthenticationTicket(principal, scheme.Name);

        return new CookieValidatePrincipalContext(httpContext, scheme, options, ticket);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(string name = "Test User")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.NameIdentifier, "test-id")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static MicrosoftIdentityWebChallengeUserException CreateAccountNotInCacheException()
    {
        var msalEx = new MsalUiRequiredException("user_null", "Account not found in token cache");
        return new MicrosoftIdentityWebChallengeUserException(msalEx, ["profile"]);
    }

    [Fact]
    public async Task ValidatePrincipal_WithValidTokenCache_DoesNotRejectPrincipal()
    {
        // Arrange
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ReturnsAsync("valid-access-token");

        var context = CreateContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        context.Principal.Should().NotBeNull("principal should not be rejected when token acquisition succeeds");
    }

    [Fact]
    public async Task ValidatePrincipal_WhenAccountNotInCache_CallsRejectPrincipal()
    {
        // Arrange
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(CreateAccountNotInCacheException());

        var context = CreateContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        context.Principal.Should().BeNull("RejectPrincipal should have been called when account is not in token cache");
    }

    [Fact]
    public async Task ValidatePrincipal_WhenAccountNotInCache_DoesNotCallSignOutAsync()
    {
        // Arrange — critical: calling SignOutAsync causes an infinite OIDC redirect loop
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(CreateAccountNotInCacheException());

        var context = CreateContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        _mockAuthService.Verify(
            x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()),
            Times.Never,
            "SignOutAsync must never be called during cookie validation — it causes an infinite redirect loop");
    }

    [Fact]
    public async Task ValidatePrincipal_WithMultipleTokensMatchedError_RejectsPrincipal()
    {
        // Arrange
        var msalEx = new MsalServiceException(MsalError.MultipleTokensMatchedError, "Multiple matching tokens detected after app recycle");
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(msalEx);

        var context = CreateContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        context.Principal.Should().BeNull("RejectPrincipal should be called when multiple tokens match");
    }

    [Fact]
    public async Task ValidatePrincipal_WithNoTokensFoundError_RejectsPrincipal()
    {
        // Arrange
        var msalEx = new MsalClientException(MsalError.NoTokensFoundError, "No tokens found in cache");
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(msalEx);

        var context = CreateContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        context.Principal.Should().BeNull("RejectPrincipal should be called when no tokens are found");
    }

    [Fact]
    public async Task ValidatePrincipal_WithNullPrincipal_DoesNotCallTokenAcquisition()
    {
        // Arrange
        var context = CreateContext();
        context.Principal = null; // Simulate cleared principal (e.g. after SignOut)

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        _mockTokenAcquisition.Verify(
            x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()),
            Times.Never,
            "token acquisition must be skipped when principal is null to avoid infinite redirect loop");
    }

    [Fact]
    public async Task ValidatePrincipal_WithUnauthenticatedIdentity_DoesNotCallTokenAcquisition()
    {
        // Arrange — unauthenticated identity (no auth type set)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(principal);

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        _mockTokenAcquisition.Verify(
            x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidatePrincipal_WithNullIdentity_DoesNotCallTokenAcquisition()
    {
        // Arrange — principal with no identities, so Identity returns null
        var principal = new ClaimsPrincipal();
        var context = CreateContext(principal);

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert
        _mockTokenAcquisition.Verify(
            x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidatePrincipal_WithNonUserNullMsalException_DoesNotRejectPrincipal()
    {
        // Arrange — error code is not "user_null", so the when-clause does not match
        var msalEx = new MsalUiRequiredException("interaction_required", "User interaction required");
        var challengeEx = new MicrosoftIdentityWebChallengeUserException(msalEx, ["profile"]);
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(challengeEx);

        var context = CreateContext();

        // Act — exception should propagate since when-clause is false
        var act = async () => await _sut.ValidatePrincipal(context);

        // Assert
        await act.Should().ThrowAsync<MicrosoftIdentityWebChallengeUserException>();
        context.Principal.Should().NotBeNull("RejectPrincipal must not be called for non-user_null errors");
    }

    [Fact]
    public async Task ValidatePrincipal_WithOtherMsalServiceException_DoesNotRejectPrincipal()
    {
        // Arrange — error code not in the handled set, so when-clause is false
        var msalEx = new MsalServiceException("request_timeout", "Service request timed out");
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(msalEx);

        var context = CreateContext();

        // Act
        var act = async () => await _sut.ValidatePrincipal(context);

        // Assert
        await act.Should().ThrowAsync<MsalServiceException>();
        context.Principal.Should().NotBeNull("RejectPrincipal must not be called for unrecognised service errors");
    }

    [Fact]
    public async Task ValidatePrincipal_WithOtherException_Rethrows()
    {
        // Arrange
        _mockTokenAcquisition
            .Setup(x => x.GetAccessTokenForUserAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<TokenAcquisitionOptions>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected token service error"));

        var context = CreateContext();

        // Act
        var act = async () => await _sut.ValidatePrincipal(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected token service error");
    }
}
