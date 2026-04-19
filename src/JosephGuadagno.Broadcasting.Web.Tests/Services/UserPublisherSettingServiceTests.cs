using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests.Services;

public class UserPublisherSettingServiceTests
{
    private readonly Mock<IDownstreamApi> _apiClient = new();
    private readonly Mock<ILogger<UserPublisherSettingService>> _logger = new();

    [Fact]
    public async Task GetCurrentUserAsync_ShouldUseApiControllerRouteAndMapMaskedSecrets()
    {
        // Arrange
        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);

        _apiClient
            .Setup(api => api.GetForUserAsync<List<UserPublisherSettingApiResponse>>(
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
                new UserPublisherSettingApiResponse
                {
                    Id = 12,
                    CreatedByEntraOid = "current-user-oid",
                    SocialMediaPlatformId = 2,
                    SocialMediaPlatform = new UserPublisherSettingSocialMediaPlatformApiResponse
                    {
                        Id = 2,
                        Name = "BlueSky",
                        Icon = "bi-cloud",
                        Url = "https://bsky.app",
                        IsActive = true
                    },
                    IsEnabled = true,
                    Bluesky = new BlueskyPublisherSettingApiResponse
                    {
                        UserName = "@switch",
                        HasAppPassword = true
                    }
                }
            ]);

        var sut = new UserPublisherSettingService(_apiClient.Object, _logger.Object);

        // Act
        var result = await sut.GetCurrentUserAsync();

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/UserPublisherSettings");
        result.Should().ContainSingle();
        result[0].Bluesky!.UserName.Should().Be("@switch");
        result[0].WriteOnlyFields.Should().Contain(nameof(BlueskyPublisherSettings.BlueskyPassword));
        result[0].Settings[nameof(BlueskyPublisherSettings.BlueskyPassword)].Should().Be("••••••••");
    }

    [Fact]
    public async Task GetByUserAsync_ShouldAppendOwnerQueryToApiControllerRoute()
    {
        // Arrange
        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);

        _apiClient
            .Setup(api => api.GetForUserAsync<List<UserPublisherSettingApiResponse>>(
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
            .ReturnsAsync([]);

        var sut = new UserPublisherSettingService(_apiClient.Object, _logger.Object);

        // Act
        await sut.GetByUserAsync("target-user-oid");

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/UserPublisherSettings?ownerOid=target-user-oid");
    }

    [Fact]
    public async Task SaveByUserAsync_ShouldSendTypedApiPayloadToControllerRoute()
    {
        // Arrange
        var capturedOptions = default(DownstreamApiOptionsReadOnlyHttpMethod);
        UserPublisherSettingApiRequest? capturedRequest = null;

        _apiClient
            .Setup(api => api.PutForUserAsync<UserPublisherSettingApiRequest, UserPublisherSettingApiResponse>(
                It.IsAny<string>(),
                It.IsAny<UserPublisherSettingApiRequest>(),
                It.IsAny<Action<DownstreamApiOptionsReadOnlyHttpMethod>>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, UserPublisherSettingApiRequest, Action<DownstreamApiOptionsReadOnlyHttpMethod>, ClaimsPrincipal, CancellationToken>(
                (_, request, configureOptions, _, _) =>
                {
                    capturedRequest = request;
                    capturedOptions = new DownstreamApiOptionsReadOnlyHttpMethod(new DownstreamApiOptions(), HttpMethod.Put.Method);
                    configureOptions(capturedOptions);
                })
            .ReturnsAsync(new UserPublisherSettingApiResponse
            {
                Id = 3,
                CreatedByEntraOid = "target-user-oid",
                SocialMediaPlatformId = 7,
                SocialMediaPlatform = new UserPublisherSettingSocialMediaPlatformApiResponse
                {
                    Id = 7,
                    Name = "Twitter/X",
                    IsActive = true
                },
                IsEnabled = true,
                Twitter = new TwitterPublisherSettingApiResponse
                {
                    HasConsumerKey = true,
                    HasConsumerSecret = true,
                    HasAccessToken = true,
                    HasAccessTokenSecret = true
                }
            });

        var sut = new UserPublisherSettingService(_apiClient.Object, _logger.Object);

        var setting = new UserPublisherSetting
        {
            CreatedByEntraOid = "target-user-oid",
            SocialMediaPlatformId = 7,
            SocialMediaPlatformName = "Twitter/X",
            IsEnabled = true,
            Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(TwitterPublisherSettings.ConsumerKey)] = "consumer-key",
                [nameof(TwitterPublisherSettings.ConsumerSecret)] = "consumer-secret",
                [nameof(TwitterPublisherSettings.OAuthToken)] = "access-token",
                [nameof(TwitterPublisherSettings.OAuthTokenSecret)] = "access-token-secret"
            }
        };

        // Act
        var result = await sut.SaveByUserAsync("target-user-oid", setting);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.RelativePath.Should().Be("/UserPublisherSettings/7?ownerOid=target-user-oid");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Twitter.Should().NotBeNull();
        capturedRequest.Twitter!.ConsumerKey.Should().Be("consumer-key");
        capturedRequest.Twitter.AccessToken.Should().Be("access-token");
        capturedRequest.Twitter.AccessTokenSecret.Should().Be("access-token-secret");
        result.Should().NotBeNull();
        result!.WriteOnlyFields.Should().Contain([
            nameof(TwitterPublisherSettings.ConsumerKey),
            nameof(TwitterPublisherSettings.ConsumerSecret),
            nameof(TwitterPublisherSettings.OAuthToken),
            nameof(TwitterPublisherSettings.OAuthTokenSecret)
        ]);
    }
}
