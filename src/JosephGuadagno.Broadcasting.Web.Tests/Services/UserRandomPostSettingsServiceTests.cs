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

public class UserRandomPostSettingsServiceTests
{
    private readonly Mock<IDownstreamApi> _apiClient = new();
    private readonly Mock<ILogger<UserRandomPostSettingsService>> _logger = new();

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<WebMappingProfile>(), new NullLoggerFactory()).CreateMapper();

    [Fact]
    public async Task AddAsync_ShouldSendExpectedApiContract()
    {
        var settings = new UserRandomPostSettings
        {
            SocialMediaPlatformId = 7,
            CronExpression = "0 * * * *",
            CutoffDate = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            ExcludedCategories = ["Announcements", "Events"],
            IsActive = true
        };

        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);
        RandomPostSettingsApiRequest? capturedRequest = null;

        _apiClient
            .Setup(api => api.PostForUserAsync<RandomPostSettingsApiRequest, UserRandomPostSettings>(
                It.IsAny<string>(),
                It.IsAny<RandomPostSettingsApiRequest>(),
                It.IsAny<Action<DownstreamApiOptionsReadOnlyHttpMethod>>(),
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, RandomPostSettingsApiRequest, Action<DownstreamApiOptionsReadOnlyHttpMethod>, System.Security.Claims.ClaimsPrincipal, CancellationToken>((_, request, configure, _, _) =>
            {
                capturedRequest = request;
                capturedOptions = new DownstreamApiOptionsReadOnlyHttpMethod(new DownstreamApiOptions(), HttpMethod.Post.Method);
                configure(capturedOptions);
            })
            .ReturnsAsync(new UserRandomPostSettings { Id = 11, SocialMediaPlatformId = 7, CronExpression = settings.CronExpression });

        var sut = new UserRandomPostSettingsService(_apiClient.Object, _logger.Object, CreateMapper());

        var result = await sut.AddAsync(settings);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/Publishers/RandomPostSettings");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SocialMediaPlatformId.Should().Be(7);
        capturedRequest.CronExpression.Should().Be("0 * * * *");
        capturedRequest.ExcludedCategories.Should().BeEquivalentTo(["Announcements", "Events"]);
        result!.Id.Should().Be(11);
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

        var sut = new UserRandomPostSettingsService(_apiClient.Object, _logger.Object, CreateMapper());

        var result = await sut.DeleteAsync(42);

        result.Should().BeTrue();
    }
}
