using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Web.Services;
using Microsoft.Identity.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests.Services;

public class EngagementServiceTests
{
    private readonly Mock<IDownstreamApi> _apiClient = new();

    [Fact]
    public async Task AddPlatformToEngagementAsync_ShouldSendExpectedApiContractAndMapCreatedResource()
    {
        // Arrange
        const int engagementId = 42;
        const int socialMediaPlatformId = 7;
        const string handle = "@switch";

        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);
        EngagementSocialMediaPlatformApiRequest? capturedRequest = null;

        _apiClient
            .Setup(api => api.PostForUserAsync<EngagementSocialMediaPlatformApiRequest, EngagementSocialMediaPlatformApiResponse>(
                It.IsAny<string>(),
                It.IsAny<EngagementSocialMediaPlatformApiRequest>(),
                It.IsAny<Action<DownstreamApiOptionsReadOnlyHttpMethod>>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, EngagementSocialMediaPlatformApiRequest, Action<DownstreamApiOptionsReadOnlyHttpMethod>, ClaimsPrincipal, CancellationToken>(
                (_, request, configureOptions, _, _) =>
                {
                    capturedRequest = request;
                    capturedOptions = new DownstreamApiOptionsReadOnlyHttpMethod(new DownstreamApiOptions(), HttpMethod.Post.Method);
                    configureOptions(capturedOptions);
                })
            .ReturnsAsync(new EngagementSocialMediaPlatformApiResponse
            {
                EngagementId = engagementId,
                SocialMediaPlatformId = socialMediaPlatformId,
                Handle = handle,
                SocialMediaPlatform = new SocialMediaPlatformApiResponse
                {
                    Id = socialMediaPlatformId,
                    Name = "Twitter",
                    Url = "https://twitter.com",
                    Icon = "bi-twitter-x",
                    IsActive = true
                }
            });

        var sut = new EngagementService(_apiClient.Object);

        // Act
        var result = await sut.AddPlatformToEngagementAsync(engagementId, socialMediaPlatformId, handle);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SocialMediaPlatformId.Should().Be(socialMediaPlatformId);
        capturedRequest.Handle.Should().Be(handle);
        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/engagements/42/platforms");

        result.Should().NotBeNull();
        result!.EngagementId.Should().Be(engagementId);
        result.SocialMediaPlatformId.Should().Be(socialMediaPlatformId);
        result.Handle.Should().Be(handle);
        result.SocialMediaPlatform.Should().NotBeNull();
        result.SocialMediaPlatform!.Name.Should().Be("Twitter");
    }

    [Fact]
    public async Task AddPlatformToEngagementAsync_WhenHandleIsNull_ShouldKeepOptionalContractValue()
    {
        // Arrange
        EngagementSocialMediaPlatformApiRequest? capturedRequest = null;
        _apiClient
            .Setup(api => api.PostForUserAsync<EngagementSocialMediaPlatformApiRequest, EngagementSocialMediaPlatformApiResponse>(
                It.IsAny<string>(),
                It.IsAny<EngagementSocialMediaPlatformApiRequest>(),
                It.IsAny<Action<DownstreamApiOptionsReadOnlyHttpMethod>>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, EngagementSocialMediaPlatformApiRequest, Action<DownstreamApiOptionsReadOnlyHttpMethod>, ClaimsPrincipal, CancellationToken>(
                (_, request, _, _, _) => capturedRequest = request)
            .ReturnsAsync((EngagementSocialMediaPlatformApiResponse?)null);

        var sut = new EngagementService(_apiClient.Object);

        // Act
        var result = await sut.AddPlatformToEngagementAsync(42, 9, null);

        // Assert
        result.Should().BeNull();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SocialMediaPlatformId.Should().Be(9);
        capturedRequest.Handle.Should().BeNull();
    }

    [Fact]
    public async Task GetPlatformsForEngagementAsync_ShouldMapApiResponseShape()
    {
        // Arrange
        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);
        _apiClient
            .Setup(api => api.GetForUserAsync<List<EngagementSocialMediaPlatformApiResponse>>(
                It.IsAny<string>(),
                It.IsAny<Action<DownstreamApiOptionsReadOnlyHttpMethod>>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Action<DownstreamApiOptionsReadOnlyHttpMethod>, ClaimsPrincipal, CancellationToken>(
                (_, configureOptions, _, _) =>
                {
                    capturedOptions = new DownstreamApiOptionsReadOnlyHttpMethod(new DownstreamApiOptions(), HttpMethod.Get.Method);
                    configureOptions(capturedOptions);
                })
            .ReturnsAsync(
            [
                new EngagementSocialMediaPlatformApiResponse
                {
                    EngagementId = 42,
                    SocialMediaPlatformId = 5,
                    Handle = "@frontend",
                    SocialMediaPlatform = new SocialMediaPlatformApiResponse
                    {
                        Id = 5,
                        Name = "BlueSky",
                        Url = "https://bsky.app",
                        Icon = "bi-bluesky",
                        IsActive = true
                    }
                }
            ]);

        var sut = new EngagementService(_apiClient.Object);

        // Act
        var result = await sut.GetPlatformsForEngagementAsync(42);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/engagements/42/platforms");
        result.Should().ContainSingle();
        result[0].EngagementId.Should().Be(42);
        result[0].SocialMediaPlatformId.Should().Be(5);
        result[0].Handle.Should().Be("@frontend");
        result[0].SocialMediaPlatform.Should().NotBeNull();
        result[0].SocialMediaPlatform!.Name.Should().Be("BlueSky");
    }
}
