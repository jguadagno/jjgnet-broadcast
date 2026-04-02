using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class AccountControllerTests
{
    private readonly AccountController _sut;

    public AccountControllerTests()
    {
        _sut = new AccountController();
    }

    [Fact]
    public void PendingApproval_ReturnsView()
    {
        // Act
        var result = _sut.PendingApproval();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
    }

    [Fact]
    public void Rejected_WithApprovalNotes_PassesNotesToView()
    {
        // Arrange
        var approvalNotes = "Your registration does not meet our requirements";
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.ApprovalNotes, approvalNotes)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _sut.Rejected();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.ViewData["ApprovalNotes"].Should().Be(approvalNotes);
    }

    [Fact]
    public void Rejected_WithNoApprovalNotes_ReturnsView()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _sut.Rejected();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.ViewData["ApprovalNotes"].Should().BeNull();
    }
}
