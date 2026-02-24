using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class EngagementsControllerTests
{
    private readonly Mock<IEngagementManager> _engagementManagerMock;
    private readonly Mock<ILogger<EngagementsController>> _loggerMock;

    public EngagementsControllerTests()
    {
        _engagementManagerMock = new Mock<IEngagementManager>();
        _loggerMock = new Mock<ILogger<EngagementsController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private EngagementsController CreateSut(string scopeClaimValue)
    {
        var controller = new EngagementsController(_engagementManagerMock.Object, _loggerMock.Object)
        {
            ControllerContext = CreateControllerContext(scopeClaimValue),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    /// <summary>
    /// Builds an HttpContext whose <see cref="ClaimsPrincipal"/> carries the given OAuth
    /// scope so that <c>HttpContext.VerifyUserHasAnyAcceptedScope</c> succeeds.
    /// Both the short "scp" claim and the full URI claim type are set for maximum
    /// compatibility with different versions of Microsoft.Identity.Web.
    /// </summary>
    private static ControllerContext CreateControllerContext(string scopeClaimValue)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scp", scopeClaimValue),
            new Claim("http://schemas.microsoft.com/identity/claims/scope", scopeClaimValue)
        ], "TestAuthentication"));

        var httpContext = new DefaultHttpContext { User = user };
        return new ControllerContext { HttpContext = httpContext };
    }

    // -------------------------------------------------------------------------
    // GetEngagementsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEngagementsAsync_WhenCalled_ReturnsAllEngagements()
    {
        // Arrange
        var engagements = new List<Engagement>
        {
            new() { Id = 1, Name = "Conference A", Url = "https://conf-a.example.com", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddDays(2), TimeZoneId = "UTC" },
            new() { Id = 2, Name = "Conference B", Url = "https://conf-b.example.com", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddDays(3), TimeZoneId = "UTC" }
        };
        _engagementManagerMock.Setup(m => m.GetAllAsync()).ReturnsAsync(engagements);

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(engagements);
        _engagementManagerMock.Verify(m => m.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEngagementsAsync_WhenNoEngagementsExist_ReturnsEmptyList()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<Engagement>());

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
        _engagementManagerMock.Verify(m => m.GetAllAsync(), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetEngagementAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetEngagementAsync_WhenEngagementExists_ReturnsEngagement()
    {
        // Arrange
        var engagement = new Engagement
        {
            Id = 1,
            Name = "Conference A",
            Url = "https://conf-a.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(2),
            TimeZoneId = "UTC"
        };
        _engagementManagerMock.Setup(m => m.GetAsync(1)).ReturnsAsync(engagement);

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementAsync(1);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().Be(engagement);
        _engagementManagerMock.Verify(m => m.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetEngagementAsync_WhenEngagementNotFound_ReturnsNullValue()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetAsync(99)).Returns(Task.FromResult<Engagement?>(null));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementAsync(99);

        // Assert
        result.Value.Should().BeNull();
        _engagementManagerMock.Verify(m => m.GetAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // SaveEngagementAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveEngagementAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        var sut = CreateSut(Domain.Scopes.Engagements.All);
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.SaveEngagementAsync(engagement);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
    }

    [Fact]
    public async Task SaveEngagementAsync_WhenSaveSucceeds_ReturnsCreatedAtActionWithEngagement()
    {
        // Arrange
        var engagement = new Engagement
        {
            Id = 0,
            Name = "New Conference",
            Url = "https://new-conf.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(2),
            TimeZoneId = "UTC"
        };
        var savedEngagement = new Engagement
        {
            Id = 42,
            Name = engagement.Name,
            Url = engagement.Url,
            StartDateTime = engagement.StartDateTime,
            EndDateTime = engagement.EndDateTime,
            TimeZoneId = engagement.TimeZoneId
        };
        _engagementManagerMock.Setup(m => m.SaveAsync(engagement)).ReturnsAsync(savedEngagement);

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.SaveEngagementAsync(engagement);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(EngagementsController.GetEngagementAsync));
        createdResult.RouteValues.Should().ContainKey("engagementId").WhoseValue.Should().Be(42);
        createdResult.Value.Should().Be(savedEngagement);
        _engagementManagerMock.Verify(m => m.SaveAsync(engagement), Times.Once);
    }

    [Fact]
    public async Task SaveEngagementAsync_WhenSaveFails_ReturnsInternalServerError()
    {
        // Arrange
        var engagement = new Engagement
        {
            Id = 1,
            Name = "Test",
            Url = "https://test.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC"
        };
        _engagementManagerMock.Setup(m => m.SaveAsync(engagement)).Returns(Task.FromResult<Engagement?>(null));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.SaveEngagementAsync(engagement);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _engagementManagerMock.Verify(m => m.SaveAsync(engagement), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteEngagementAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteEngagementAsync_WhenEngagementExists_ReturnsNoContent()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.DeleteAsync(1)).ReturnsAsync(true);

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.DeleteEngagementAsync(1);

        // Assert
        result.Result.Should().BeOfType<NoContentResult>();
        _engagementManagerMock.Verify(m => m.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteEngagementAsync_WhenEngagementNotFound_ReturnsNotFound()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.DeleteAsync(99)).ReturnsAsync(false);

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.DeleteEngagementAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _engagementManagerMock.Verify(m => m.DeleteAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetTalksForEngagementAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTalksForEngagementAsync_WhenCalled_ReturnsTalksForEngagement()
    {
        // Arrange
        var talks = new List<Talk>
        {
            new() { Id = 1, EngagementId = 10, Name = "Talk 1", UrlForConferenceTalk = "https://conf.example.com/talk1", UrlForTalk = "https://example.com/talk1", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddHours(1) },
            new() { Id = 2, EngagementId = 10, Name = "Talk 2", UrlForConferenceTalk = "https://conf.example.com/talk2", UrlForTalk = "https://example.com/talk2", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddHours(1) }
        };
        _engagementManagerMock.Setup(m => m.GetTalksForEngagementAsync(10)).ReturnsAsync(talks);

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalksForEngagementAsync(10);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(talks);
        _engagementManagerMock.Verify(m => m.GetTalksForEngagementAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetTalksForEngagementAsync_WhenNoTalksExist_ReturnsEmptyList()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetTalksForEngagementAsync(10)).ReturnsAsync(new List<Talk>());

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalksForEngagementAsync(10);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
        _engagementManagerMock.Verify(m => m.GetTalksForEngagementAsync(10), Times.Once);
    }

    // -------------------------------------------------------------------------
    // SaveTalkAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveTalkAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var talk = new Talk { Id = 1, EngagementId = 10 };
        var sut = CreateSut(Domain.Scopes.Talks.All);
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.SaveTalkAsync(talk);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Never);
    }

    [Fact]
    public async Task SaveTalkAsync_WhenSaveSucceeds_ReturnsCreatedAtActionWithTalk()
    {
        // Arrange
        var talk = new Talk
        {
            Id = 0,
            EngagementId = 10,
            Name = "New Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        var savedTalk = new Talk
        {
            Id = 55,
            EngagementId = talk.EngagementId,
            Name = talk.Name,
            UrlForConferenceTalk = talk.UrlForConferenceTalk,
            UrlForTalk = talk.UrlForTalk,
            StartDateTime = talk.StartDateTime,
            EndDateTime = talk.EndDateTime
        };
        _engagementManagerMock.Setup(m => m.SaveTalkAsync(talk)).ReturnsAsync(savedTalk);

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.SaveTalkAsync(talk);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(EngagementsController.GetTalkAsync));
        createdResult.RouteValues.Should().ContainKey("talkId").WhoseValue.Should().Be(talk.Id);
        createdResult.Value.Should().Be(savedTalk);
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(talk), Times.Once);
    }

    [Fact]
    public async Task SaveTalkAsync_WhenSaveFails_ReturnsInternalServerError()
    {
        // Arrange
        var talk = new Talk
        {
            Id = 1,
            EngagementId = 10,
            Name = "Failing Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        _engagementManagerMock.Setup(m => m.SaveTalkAsync(talk)).Returns(Task.FromResult<Talk?>(null));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.SaveTalkAsync(talk);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(talk), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetTalkAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTalkAsync_WhenTalkExists_ReturnsTalk()
    {
        // Arrange
        var talk = new Talk
        {
            Id = 5,
            EngagementId = 10,
            Name = "Talk 5",
            UrlForConferenceTalk = "https://conf.example.com/talk5",
            UrlForTalk = "https://example.com/talk5",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        _engagementManagerMock.Setup(m => m.GetTalkAsync(5)).ReturnsAsync(talk);

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalkAsync(10, 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(talk);
        _engagementManagerMock.Verify(m => m.GetTalkAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetTalkAsync_WhenTalkNotFound_ReturnsNull()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetTalkAsync(99)).Returns(Task.FromResult<Talk?>(null));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalkAsync(10, 99);

        // Assert
        result.Should().BeNull();
        _engagementManagerMock.Verify(m => m.GetTalkAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteTalkAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteTalkAsync_WhenTalkExists_ReturnsNoContent()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.RemoveTalkFromEngagementAsync(5)).ReturnsAsync(true);

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.DeleteTalkAsync(10, 5);

        // Assert
        result.Result.Should().BeOfType<NoContentResult>();
        _engagementManagerMock.Verify(m => m.RemoveTalkFromEngagementAsync(5), Times.Once);
    }

    [Fact]
    public async Task DeleteTalkAsync_WhenTalkNotFound_ReturnsNotFound()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.RemoveTalkFromEngagementAsync(99)).ReturnsAsync(false);

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.DeleteTalkAsync(10, 99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _engagementManagerMock.Verify(m => m.RemoveTalkFromEngagementAsync(99), Times.Once);
    }
}
