using System.Net;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.MappingProfiles;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests.Services;

public class UserEventDistributorMappingServiceTests
{
    private readonly Mock<IDownstreamApi> _apiClient = new();
    private readonly Mock<ILogger<UserEventDistributorMappingService>> _logger = new();

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<WebMappingProfile>(), new NullLoggerFactory()).CreateMapper();

    [Fact]
    public async Task AddAsync_ShouldSendExpectedApiContract()
    {
        var mapping = new UserEventDistributorMapping
        {
            EventType = "RandomPost",
            SocialMediaPlatformId = 4,
            IsActive = true
        };

        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);
        EventDistributorMappingApiRequest? capturedRequest = null;

        _apiClient
            .Setup(api => api.PostForUserAsync<EventDistributorMappingApiRequest, UserEventDistributorMapping>(
                It.IsAny<string>(),
                It.IsAny<EventDistributorMappingApiRequest>(),
                It.IsAny<Action<DownstreamApiOptionsReadOnlyHttpMethod>>(),
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, EventDistributorMappingApiRequest, Action<DownstreamApiOptionsReadOnlyHttpMethod>, System.Security.Claims.ClaimsPrincipal, CancellationToken>((_, request, configure, _, _) =>
            {
                capturedRequest = request;
                capturedOptions = new DownstreamApiOptionsReadOnlyHttpMethod(new DownstreamApiOptions(), HttpMethod.Post.Method);
                configure(capturedOptions);
            })
            .ReturnsAsync(new UserEventDistributorMapping { Id = 15, EventType = mapping.EventType, SocialMediaPlatformId = mapping.SocialMediaPlatformId });

        var sut = new UserEventDistributorMappingService(_apiClient.Object, _logger.Object, CreateMapper());

        var result = await sut.AddAsync(mapping);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/Distributors/EventDistributorMappings");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.EventType.Should().Be("RandomPost");
        capturedRequest.SocialMediaPlatformId.Should().Be(4);
        result!.Id.Should().Be(15);
    }

    [Fact]
    public async Task DeleteAsync_WhenApiReturnsNoContent_ShouldReturnTrue()
    {
        _apiClient
            .Setup(api => api.CallApiForUserAsync<HttpResponseMessage>(
                It.IsAny<string>(),
                It.IsAny<Action<DownstreamApiOptions>>(),
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var sut = new UserEventDistributorMappingService(_apiClient.Object, _logger.Object, CreateMapper());

        var result = await sut.DeleteAsync(12);

        result.Should().BeTrue();
    }
}
