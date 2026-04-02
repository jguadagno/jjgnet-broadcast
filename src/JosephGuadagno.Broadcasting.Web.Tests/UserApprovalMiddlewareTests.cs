using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests;

public class UserApprovalMiddlewareTests
{
    private readonly Mock<ILogger<UserApprovalMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly UserApprovalMiddleware _sut;
    private const string ApprovalStatusClaimType = "approval_status";

    public UserApprovalMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<UserApprovalMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
        _sut = new UserApprovalMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthenticatedUser_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Home/Index";
        
        // User is not authenticated (default state)
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(200); // Not redirected
    }

    [Fact]
    public async Task InvokeAsync_WithApprovedUser_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Home/Index";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Approved.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithPendingUser_RedirectsToPendingApproval()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Home/Index";
        context.Response.Body = new MemoryStream(); // Required for redirect
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Pending User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Pending.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Never);
        context.Response.StatusCode.Should().Be(302); // Redirect
        context.Response.Headers["Location"].ToString().Should().Be("/Account/PendingApproval");
    }

    [Fact]
    public async Task InvokeAsync_WithRejectedUser_RedirectsToRejected()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Home/Index";
        context.Response.Body = new MemoryStream(); // Required for redirect
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Rejected User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Rejected.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Never);
        context.Response.StatusCode.Should().Be(302); // Redirect
        context.Response.Headers["Location"].ToString().Should().Be("/Account/Rejected");
    }

    [Fact]
    public async Task InvokeAsync_WithApprovalPage_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Account/PendingApproval";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Pending User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Pending.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithRejectedPage_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Account/Rejected";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Rejected User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Rejected.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Theory]
    [InlineData("/.well-known/openid-configuration")]
    [InlineData("/favicon.ico")]
    [InlineData("/robots.txt")]
    [InlineData("/css/site.css")]
    [InlineData("/js/site.js")]
    [InlineData("/lib/bootstrap/bootstrap.min.css")]
    [InlineData("/images/logo.png")]
    public async Task InvokeAsync_WithStaticFile_CallsNext(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Pending User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Pending.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMicrosoftIdentityPath_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/MicrosoftIdentity/SignIn";
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Pending User"),
            new Claim(ApprovalStatusClaimType, ApprovalStatus.Pending.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithNoApprovalStatusClaim_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/Home/Index";
        
        // User is authenticated but has no approval status claim yet
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "New User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await _sut.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
    }
}
