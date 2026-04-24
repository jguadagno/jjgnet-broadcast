using Moq;
using Moq.Protected;

using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Models.LinkedIn;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

/// <summary>
/// A simple in-memory ISession implementation for testing.
/// </summary>
internal class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public string Id => "test-session-id";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var bytes))
        {
            value = bytes;
            return true;
        }

        value = Array.Empty<byte>();
        return false;
    }
}

public class LinkedInControllerTests
{
    private const string TestOwnerOid = "test-owner-oid-12345";

    private readonly Mock<IUserOAuthTokenManager> _userOAuthTokenManager;
    private readonly Mock<ISocialMediaPlatformManager> _socialMediaPlatformManager;
    private readonly LinkedInSettings _linkedInSettings;
    private readonly Mock<ILogger<LinkedInController>> _logger;
    private readonly SocialMediaPlatform _linkedInPlatform;

    public LinkedInControllerTests()
    {
        _userOAuthTokenManager = new Mock<IUserOAuthTokenManager>();
        _socialMediaPlatformManager = new Mock<ISocialMediaPlatformManager>();
        _linkedInSettings = new LinkedInSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Scopes = "openid profile email",
            AuthorizationUrl = "https://www.linkedin.com/oauth/v2/authorization",
            AccessTokenUrl = "https://www.linkedin.com/oauth/v2/accessToken"
        };
        _logger = new Mock<ILogger<LinkedInController>>();
        _linkedInPlatform = new SocialMediaPlatform { Id = 3, Name = "LinkedIn", IsActive = true };

        // Default: platform lookup returns LinkedIn
        _socialMediaPlatformManager
            .Setup(m => m.GetByNameAsync("LinkedIn", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_linkedInPlatform);
    }

    private LinkedInController CreateController(HttpClient? httpClient = null)
    {
        httpClient ??= new HttpClient();
        return new LinkedInController(
            httpClient,
            _userOAuthTokenManager.Object,
            _socialMediaPlatformManager.Object,
            Options.Create(_linkedInSettings),
            _logger.Object);
    }

    /// <summary>
    /// Builds an HttpContext with a ClaimsPrincipal containing an "oid" claim.
    /// </summary>
    private static DefaultHttpContext BuildAuthenticatedHttpContext(string? oid = TestOwnerOid, ISession? session = null)
    {
        var claims = new List<Claim>();
        if (oid is not null)
            claims.Add(new Claim("oid", oid));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        if (session is not null)
            httpContext.Session = session;

        return httpContext;
    }

    [Fact]
    public async Task Index_WhenTokenExists_ShouldReturnViewWithMaskedTokenInfo()
    {
        // Arrange
        var storedToken = new UserOAuthToken
        {
            CreatedByEntraOid = TestOwnerOid,
            SocialMediaPlatformId = 3,
            AccessToken = "abcd1234efgh5678",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        _userOAuthTokenManager
            .Setup(m => m.GetByUserAndPlatformAsync(TestOwnerOid, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildAuthenticatedHttpContext()
        };

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SavedTokenInfo>(viewResult.Model);
        Assert.True(model.HasToken);
        Assert.NotNull(model.MaskedAccessToken);
        Assert.DoesNotContain("abcd1234efgh5678", model.MaskedAccessToken!); // raw token never exposed
        Assert.Equal(storedToken.AccessTokenExpiresAt, model.ExpiresOn);
        _userOAuthTokenManager.Verify(m => m.GetByUserAndPlatformAsync(TestOwnerOid, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Index_WhenNoTokenStored_ShouldReturnViewWithHasTokenFalse()
    {
        // Arrange
        _userOAuthTokenManager
            .Setup(m => m.GetByUserAndPlatformAsync(TestOwnerOid, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserOAuthToken?)null);

        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildAuthenticatedHttpContext()
        };

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SavedTokenInfo>(viewResult.Model);
        Assert.False(model.HasToken);
    }

    [Fact]
    public async Task Index_WhenOidClaimMissing_ShouldReturnViewWithHasTokenFalse()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildAuthenticatedHttpContext(oid: null)
        };

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SavedTokenInfo>(viewResult.Model);
        Assert.False(model.HasToken);
    }

    [Fact]
    public async Task RefreshToken_WhenCallbackUrlIsValid_ShouldRedirectToLinkedInAuthUrl()
    {
        // Arrange
        var controller = CreateController();

        var session = new TestSession();
        var httpContext = BuildAuthenticatedHttpContext(session: session);

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://myapp.example.com/LinkedIn/Callback");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.Url = mockUrlHelper.Object;

        // Act
        var result = await controller.RefreshToken();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("https://www.linkedin.com/oauth/v2/authorization", redirectResult.Url);
        Assert.Contains("test-client-id", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WhenStateMismatch_ShouldReturnUnauthorized()
    {
        // Arrange
        var controller = CreateController();

        var session = new TestSession();
        session.Set("state", Encoding.UTF8.GetBytes("session-state-value"));

        var httpContext = BuildAuthenticatedHttpContext(session: session);
        httpContext.Request.QueryString = new QueryString("?code=auth-code&state=different-state");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.Callback();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Callback_WhenCodeMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var controller = CreateController();

        const string stateValue = "matching-state";
        var session = new TestSession();
        session.Set("state", Encoding.UTF8.GetBytes(stateValue));

        var httpContext = BuildAuthenticatedHttpContext(session: session);
        httpContext.Request.QueryString = new QueryString($"?state={stateValue}");

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://myapp.example.com/LinkedIn/Callback");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.Url = mockUrlHelper.Object;

        // Act
        var result = await controller.Callback();

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Callback_WhenValid_ShouldStoreTokenViaManagerAndRedirectToIndex()
    {
        // Arrange
        const string stateValue = "valid-state";
        const string authCode = "valid-auth-code";

        var tokenResponse = new
        {
            access_token = "new-access-token",
            expires_in = 5183944,
            scope = "openid profile email"
        };
        var tokenJson = JsonSerializer.Serialize(tokenResponse);

        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(tokenJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var controller = CreateController(httpClient);

        var session = new TestSession();
        session.Set("state", Encoding.UTF8.GetBytes(stateValue));

        var httpContext = BuildAuthenticatedHttpContext(session: session);
        httpContext.Request.QueryString = new QueryString($"?code={authCode}&state={stateValue}");

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://myapp.example.com/LinkedIn/Callback");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.Url = mockUrlHelper.Object;

        _userOAuthTokenManager
            .Setup(m => m.StoreOAuthCallbackTokenAsync(
                TestOwnerOid,
                3,
                "new-access-token",
                It.IsAny<string?>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserOAuthToken?)null);

        // Act
        var result = await controller.Callback();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        // Token is stored per-user via manager — no Key Vault
        _userOAuthTokenManager.Verify(m => m.StoreOAuthCallbackTokenAsync(
            TestOwnerOid,
            3,
            "new-access-token",
            It.IsAny<string?>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void LinkedInController_HasRequireContributorPolicy()
    {
        // Arrange & Act
        var controllerType = typeof(LinkedInController);
        var attributes = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false);

        // Assert
        Assert.NotEmpty(attributes);
        var authorizeAttribute = attributes.First() as AuthorizeAttribute;
        Assert.NotNull(authorizeAttribute);
        Assert.Equal(AuthorizationPolicyNames.RequireContributor, authorizeAttribute!.Policy);
    }
}