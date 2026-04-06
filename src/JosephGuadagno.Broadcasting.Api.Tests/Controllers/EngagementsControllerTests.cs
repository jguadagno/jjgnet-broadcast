using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain;
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
    private readonly IMapper _mapper;

    public EngagementsControllerTests()
    {
        _engagementManagerMock = new Mock<IEngagementManager>();
        _loggerMock = new Mock<ILogger<EngagementsController>>();
        
        // Configure AutoMapper with the API profile
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<JosephGuadagno.Broadcasting.Api.MappingProfiles.ApiBroadcastingProfile>();
        }, new LoggerFactory());
        _mapper = mapperConfig.CreateMapper();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private EngagementsController CreateSut(string scopeClaimValue)
    {
        var controller = new EngagementsController(_engagementManagerMock.Object, _loggerMock.Object, _mapper)
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
        _engagementManagerMock.Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Engagement> { Items = engagements, TotalCount = engagements.Count });

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().BeEquivalentTo(engagements, opts => opts
            .ExcludingMissingMembers()
            .Excluding(e => e.Talks));
        result.Value!.TotalCount.Should().Be(2);
        _engagementManagerMock.Verify(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetEngagementsAsync_WhenNoEngagementsExist_ReturnsEmptyList()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Engagement> { Items = new List<Engagement>(), TotalCount = 0 });

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
        _engagementManagerMock.Verify(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
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
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(engagement, opts => opts
            .ExcludingMissingMembers()
            .Excluding(e => e.Talks));
        _engagementManagerMock.Verify(m => m.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetEngagementAsync_WhenEngagementNotFound_ReturnsNotFound()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetAsync(99)).Returns(Task.FromResult<Engagement?>(null));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.GetEngagementAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _engagementManagerMock.Verify(m => m.GetAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CreateEngagementAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateEngagementAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new EngagementRequest { Name = "New Conference", Url = "https://new-conf.example.com", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddDays(2), TimeZoneId = "UTC" };
        var sut = CreateSut(Domain.Scopes.Engagements.All);
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.CreateEngagementAsync(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
    }

    [Fact]
    public async Task CreateEngagementAsync_WhenSaveSucceeds_ReturnsCreatedAtActionWithEngagement()
    {
        // Arrange
        var request = new EngagementRequest
        {
            Name = "New Conference",
            Url = "https://new-conf.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(2),
            TimeZoneId = "UTC"
        };
        var savedEngagement = new Engagement
        {
            Id = 42,
            Name = request.Name,
            Url = request.Url,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            TimeZoneId = request.TimeZoneId
        };
        _engagementManagerMock.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Success(savedEngagement));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.CreateEngagementAsync(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(EngagementsController.GetEngagementAsync));
        createdResult.RouteValues.Should().ContainKey("engagementId").WhoseValue.Should().Be(42);
        createdResult.Value.Should().BeEquivalentTo(savedEngagement, opts => opts
            .ExcludingMissingMembers()
            .Excluding(e => e.Talks));
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
    }

    [Fact]
    public async Task CreateEngagementAsync_WhenSaveFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new EngagementRequest
        {
            Name = "Test",
            Url = "https://test.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC"
        };
        _engagementManagerMock.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Failure("Save failed"));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.CreateEngagementAsync(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
    }
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateEngagementAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new EngagementRequest { Name = "Conference A", Url = "https://conf-a.example.com", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddDays(2), TimeZoneId = "UTC" };
        var sut = CreateSut(Domain.Scopes.Engagements.All);
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.UpdateEngagementAsync(1, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEngagementAsync_WhenUpdateSucceeds_ReturnsOkWithEngagement()
    {
        // Arrange
        var request = new EngagementRequest
        {
            Name = "Updated Conference",
            Url = "https://updated-conf.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(2),
            TimeZoneId = "UTC"
        };
        var savedEngagement = new Engagement
        {
            Id = 1,
            Name = request.Name,
            Url = request.Url,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            TimeZoneId = request.TimeZoneId
        };
        _engagementManagerMock.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Success(savedEngagement));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.UpdateEngagementAsync(1, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(savedEngagement, opts => opts
            .ExcludingMissingMembers()
            .Excluding(e => e.Talks));
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEngagementAsync_WhenUpdateFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new EngagementRequest
        {
            Name = "Test",
            Url = "https://test.example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC"
        };
        _engagementManagerMock.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Failure("Save failed"));

        var sut = CreateSut(Domain.Scopes.Engagements.All);

        // Act
        var result = await sut.UpdateEngagementAsync(1, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _engagementManagerMock.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteEngagementAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteEngagementAsync_WhenEngagementExists_ReturnsNoContent()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.DeleteAsync(1)).ReturnsAsync(OperationResult<bool>.Success(true));

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
        _engagementManagerMock.Setup(m => m.DeleteAsync(99)).ReturnsAsync(OperationResult<bool>.Failure("Not found"));

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
        _engagementManagerMock.Setup(m => m.GetTalksForEngagementAsync(10, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Talk> { Items = talks, TotalCount = talks.Count });

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalksForEngagementAsync(10);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().BeEquivalentTo(talks, opts => opts.ExcludingMissingMembers());
        result.Value!.TotalCount.Should().Be(2);
        _engagementManagerMock.Verify(m => m.GetTalksForEngagementAsync(10, It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetTalksForEngagementAsync_WhenNoTalksExist_ReturnsEmptyList()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetTalksForEngagementAsync(10, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Talk> { Items = new List<Talk>(), TotalCount = 0 });

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalksForEngagementAsync(10);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
        _engagementManagerMock.Verify(m => m.GetTalksForEngagementAsync(10, It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CreateTalkAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTalkAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new TalkRequest { Name = "Test Talk", UrlForConferenceTalk = "https://conf.example.com/talk", UrlForTalk = "https://example.com/talk", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddHours(1) };
        var sut = CreateSut(Domain.Scopes.Talks.All);
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.CreateTalkAsync(10, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Never);
    }

    [Fact]
    public async Task CreateTalkAsync_WhenSaveSucceeds_ReturnsCreatedAtActionWithTalk()
    {
        // Arrange
        var request = new TalkRequest
        {
            Name = "New Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        var savedTalk = new Talk
        {
            Id = 55,
            EngagementId = 10,
            Name = request.Name,
            UrlForConferenceTalk = request.UrlForConferenceTalk,
            UrlForTalk = request.UrlForTalk,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime
        };
        _engagementManagerMock.Setup(m => m.SaveTalkAsync(It.IsAny<Talk>())).ReturnsAsync(OperationResult<Talk>.Success(savedTalk));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.CreateTalkAsync(10, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(EngagementsController.GetTalkAsync));
        createdResult.RouteValues.Should().ContainKey("talkId").WhoseValue.Should().Be(savedTalk.Id);
        createdResult.Value.Should().BeEquivalentTo(savedTalk, opts => opts.ExcludingMissingMembers());
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Once);
    }

    [Fact]
    public async Task CreateTalkAsync_WhenSaveFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new TalkRequest
        {
            Name = "Failing Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        _engagementManagerMock.Setup(m => m.SaveTalkAsync(It.IsAny<Talk>())).ReturnsAsync(OperationResult<Talk>.Failure("Save failed"));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.CreateTalkAsync(10, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateTalkAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateTalkAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new TalkRequest { Name = "Test Talk", UrlForConferenceTalk = "https://conf.example.com/talk", UrlForTalk = "https://example.com/talk", StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow.AddHours(1) };
        var sut = CreateSut(Domain.Scopes.Talks.All);
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.UpdateTalkAsync(10, 5, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTalkAsync_WhenUpdateSucceeds_ReturnsOkWithTalk()
    {
        // Arrange
        var request = new TalkRequest
        {
            Name = "Updated Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        var savedTalk = new Talk
        {
            Id = 5,
            EngagementId = 10,
            Name = request.Name,
            UrlForConferenceTalk = request.UrlForConferenceTalk,
            UrlForTalk = request.UrlForTalk,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime
        };
        _engagementManagerMock.Setup(m => m.SaveTalkAsync(It.IsAny<Talk>())).ReturnsAsync(OperationResult<Talk>.Success(savedTalk));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.UpdateTalkAsync(10, 5, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(savedTalk, opts => opts.ExcludingMissingMembers());
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTalkAsync_WhenUpdateFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new TalkRequest
        {
            Name = "Failing Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };
        _engagementManagerMock.Setup(m => m.SaveTalkAsync(It.IsAny<Talk>())).ReturnsAsync(OperationResult<Talk>.Failure("Save failed"));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.UpdateTalkAsync(10, 5, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _engagementManagerMock.Verify(m => m.SaveTalkAsync(It.IsAny<Talk>()), Times.Once);
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
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(talk, opts => opts.ExcludingMissingMembers());
        _engagementManagerMock.Verify(m => m.GetTalkAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetTalkAsync_WhenTalkNotFound_ReturnsNotFound()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.GetTalkAsync(99)).Returns(Task.FromResult<Talk?>(null));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.GetTalkAsync(10, 99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _engagementManagerMock.Verify(m => m.GetTalkAsync(99), Times.Once);
    }

    [Fact]
    public async Task GetTalkAsync_WithViewScope_ReturnsTalk()
    {
        // Arrange - verifies Talks.View (fine-grained) is accepted, not just Talks.All
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

        var sut = CreateSut(Domain.Scopes.Talks.View);

        // Act
        var result = await sut.GetTalkAsync(10, 5);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(talk, opts => opts.ExcludingMissingMembers());
    }

    // -------------------------------------------------------------------------
    // DeleteTalkAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteTalkAsync_WhenTalkExists_ReturnsNoContent()
    {
        // Arrange
        _engagementManagerMock.Setup(m => m.RemoveTalkFromEngagementAsync(5)).ReturnsAsync(OperationResult<bool>.Success(true));

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
        _engagementManagerMock.Setup(m => m.RemoveTalkFromEngagementAsync(99)).ReturnsAsync(OperationResult<bool>.Failure("Not found"));

        var sut = CreateSut(Domain.Scopes.Talks.All);

        // Act
        var result = await sut.DeleteTalkAsync(10, 99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _engagementManagerMock.Verify(m => m.RemoveTalkFromEngagementAsync(99), Times.Once);
    }
}
