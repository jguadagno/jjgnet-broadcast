using Moq;
using Moq.Protected;

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Azure.Security.KeyVault.Secrets;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
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
    private readonly Mock<IKeyVault> _keyVault;
    private readonly Mock<ILinkedInSettings> _linkedInSettings;
    private readonly Mock<ILogger<LinkedInController>> _logger;

    public LinkedInControllerTests()
    {
        _keyVault = new Mock<IKeyVault>();
        _linkedInSettings = new Mock<ILinkedInSettings>();
        _logger = new Mock<ILogger<LinkedInController>>();

        // Default LinkedIn settings
        _linkedInSettings.SetupGet(s => s.ClientId).Returns("test-client-id");
        _linkedInSettings.SetupGet(s => s.ClientSecret).Returns("test-client-secret");
        _linkedInSettings.SetupGet(s => s.Scopes).Returns("openid profile email");
        _linkedInSettings.SetupGet(s => s.AuthorizationUrl).Returns("https://www.linkedin.com/oauth/v2/authorization");
        _linkedInSettings.SetupGet(s => s.AccessTokenUrl).Returns("https://www.linkedin.com/oauth/v2/accessToken");
    }

    private LinkedInController CreateController(HttpClient? httpClient = null)
    {
        httpClient ??= new HttpClient();
        return new LinkedInController(httpClient, _keyVault.Object, _linkedInSettings.Object, _logger.Object);
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithSavedTokenInfo()
    {
        // Arrange
        var secret = new KeyVaultSecret("jjg-net-linkedin-access-token", "test-access-token-value");
        _keyVault.Setup(k => k.GetSecretAsync("jjg-net-linkedin-access-token")).ReturnsAsync(secret);

        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SavedTokenInfo>(viewResult.Model);
        Assert.Equal("test-access-token-value", model.AccessToken);
        Assert.Equal("jjg-net-linkedin-access-token", model.KeyVaultSecretName);
        _keyVault.Verify(k => k.GetSecretAsync("jjg-net-linkedin-access-token"), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WhenCallbackUrlIsValid_ShouldRedirectToLinkedInAuthUrl()
    {
        // Arrange
        var controller = CreateController();

        var session = new TestSession();
        var httpContext = new DefaultHttpContext();
        httpContext.Session = session;

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

        var httpContext = new DefaultHttpContext();
        httpContext.Session = session;
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

        var httpContext = new DefaultHttpContext();
        httpContext.Session = session;
        // State matches, but no code in query string
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
    public async Task Callback_WhenValid_ShouldSaveTokenAndRedirectToIndex()
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

        var httpContext = new DefaultHttpContext();
        httpContext.Session = session;
        httpContext.Request.QueryString = new QueryString($"?code={authCode}&state={stateValue}");

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://myapp.example.com/LinkedIn/Callback");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.Url = mockUrlHelper.Object;

        _keyVault
            .Setup(k => k.UpdateSecretValueAndPropertiesAsync(
                "jjg-net-linkedin-access-token",
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await controller.Callback();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _keyVault.Verify(k => k.UpdateSecretValueAndPropertiesAsync(
            "jjg-net-linkedin-access-token",
            "new-access-token",
            It.IsAny<DateTime>()), Times.Once);
    }
}
