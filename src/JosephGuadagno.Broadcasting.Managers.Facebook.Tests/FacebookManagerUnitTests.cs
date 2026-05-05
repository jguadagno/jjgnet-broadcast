using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

/// <summary>
/// Unit tests for FacebookManager using Moq to isolate external dependencies
/// </summary>
public class FacebookManagerUnitTests
{
    private readonly Mock<ILogger<FacebookManager>> _mockLogger;
    private readonly Mock<IFacebookApplicationSettings> _mockFacebookSettings;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public FacebookManagerUnitTests()
    {
        _mockLogger = new Mock<ILogger<FacebookManager>>();
        _mockFacebookSettings = new Mock<IFacebookApplicationSettings>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        // Setup default settings
        _mockFacebookSettings.Setup(s => s.GraphApiRootUrl).Returns("https://graph.facebook.com");
        _mockFacebookSettings.Setup(s => s.GraphApiVersion).Returns("v21.0");
        _mockFacebookSettings.Setup(s => s.PageId).Returns("testPageId");
        _mockFacebookSettings.Setup(s => s.PageAccessToken).Returns("testAccessToken");
    }

    #region PostMessageAndLinkToPage Tests

    [Fact]
    public async Task PostMessageAndLinkToPage_WithEmptyMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostMessageAndLinkToPage("", "https://example.com"));

        Assert.Equal("message", exception.ParamName);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_WithEmptyLink_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.PostMessageAndLinkToPage("Test Message", ""));

        Assert.Equal("link", exception.ParamName);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_Success_ReturnsId()
    {
        // Arrange
        var expectedId = "12345_67890";
        var jsonResponse = $"{{\"id\": \"{expectedId}\"}}";
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, jsonResponse);
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act
        var result = await sut.PostMessageAndLinkToPage("Test Message", "https://example.com");

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_FacebookError_ThrowsException()
    {
        // Arrange
        var errorMessage = "Some Facebook Error";
        var jsonResponse = $"{{\"error\": {{\"message\": \"{errorMessage}\", \"type\": \"OAuthException\", \"code\": 100, \"error_subcode\": 33, \"fbtrace_id\": \"trace123\"}}}}";
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, jsonResponse);
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FacebookPostException>(
            () => sut.PostMessageAndLinkToPage("Test Message", "https://example.com"));
        Assert.Equal($"Failed to post status. Reason {errorMessage}", exception.Message);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_NullResponse_ThrowsException()
    {
        // Arrange
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, "null");
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FacebookPostException>(
            () => sut.PostMessageAndLinkToPage("Test Message", "https://example.com"));
        Assert.Contains("Failed to post status. Could not deserialized the response.", exception.Message);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_HttpError_ThrowsException()
    {
        // Arrange
        SetupHttpMessageHandler(System.Net.HttpStatusCode.BadRequest, "Error");
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FacebookPostException>(
            () => sut.PostMessageAndLinkToPage("Test Message", "https://example.com"));
        Assert.Contains("Failed to post status. Response status code was not successful.", exception.Message);
    }

    [Fact]
    public async Task PostMessageAndLinkToPage_EmptyId_ThrowsException()
    {
        // Arrange
        var jsonResponse = "{\"id\": \"\"}";
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, jsonResponse);
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FacebookPostException>(
            () => sut.PostMessageAndLinkToPage("Test Message", "https://example.com"));
        Assert.Contains("Failed to post status. Could not determine the reason.", exception.Message);
    }
    
    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_WithEmptyToken_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.RefreshToken(""));

        Assert.Equal("tokenToRefresh", exception.ParamName);
    }

    [Fact]
    public async Task RefreshToken_Success_ReturnsTokenInfo()
    {
        // Arrange
        var expectedToken = "new_access_token";
        var jsonResponse = $"{{\"access_token\": \"{expectedToken}\", \"token_type\": \"bearer\", \"expires_in\": 3600}}";
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, jsonResponse);
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act
        var result = await sut.RefreshToken("old_token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.AccessToken);
        Assert.Equal("bearer", result.TokenType);
        Assert.True(result.ExpiresOn > DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_NullResponse_ThrowsException()
    {
        // Arrange
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, "null");
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FacebookPostException>(
            () => sut.RefreshToken("old_token"));
        Assert.Contains("Failed to refresh the token. Could not deserialize the response.", exception.Message);
    }

    [Fact]
    public async Task RefreshToken_HttpError_ThrowsException()
    {
        // Arrange
        SetupHttpMessageHandler(System.Net.HttpStatusCode.InternalServerError, "Error");
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FacebookPostException>(
            () => sut.RefreshToken("old_token"));
        Assert.Contains("Failed to refresh the token. Response status code was not successful.", exception.Message);
    }

    #endregion

    [Fact]
    public void GraphApiRoot_ReturnsCorrectUrl()
    {
        // Arrange
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act
        var result = sut.GraphApiRoot;

        // Assert
        Assert.Equal("https://graph.facebook.com/v21.0/", result);
    }

    [Fact]
    public async Task PublishAsync_WithImageUrl_UsesPicturePublishingPath()
    {
        // Arrange
        var expectedId = "12345_67890";
        var jsonResponse = $"{{\"id\": \"{expectedId}\"}}";
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, jsonResponse);
        ISocialMediaPublisher sut =
            new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act
        var result = await sut.PublishAsync(new SocialMediaPublishRequest
        {
            Text = "Test Message",
            LinkUrl = "https://example.com",
            ImageUrl = "https://example.com/image.png"
        });

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    public async Task PublishAsync_WithoutLinkOrImage_PostsTextOnlyStatus()
    {
        // Arrange
        var expectedId = "12345_67890";
        var jsonResponse = $"{{\"id\": \"{expectedId}\"}}";
        SetupHttpMessageHandler(System.Net.HttpStatusCode.OK, jsonResponse);
        ISocialMediaPublisher sut =
            new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act
        var result = await sut.PublishAsync(new SocialMediaPublishRequest
        {
            Text = "Test Message"
        });

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    public async Task PublishAsync_WithImageUrlAndNoLink_ThrowsArgumentNullException()
    {
        // Arrange
        ISocialMediaPublisher sut =
            new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        // Act
        var act = () => sut.PublishAsync(new SocialMediaPublishRequest
        {
            Text = "Test Message",
            ImageUrl = "https://example.com/image.png"
        });

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public void IFacebookManager_Implements_ISocialMediaPublisher()
    {
        Assert.True(typeof(ISocialMediaPublisher).IsAssignableFrom(typeof(IFacebookManager)));
    }

    #region ComposeMessageAsync Tests

    [Fact]
    public async Task ComposeMessageAsync_WithoutScopeFactory_ThrowsInvalidOperationException()
    {
        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object);

        var act = () => sut.ComposeMessageAsync(new ScheduledItem
        {
            Id = 1,
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 42,
            Message = "fallback",
            SendOnDateTime = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenTemplateExists_RendersSyndicationContent()
    {
        var scheduledItem = new ScheduledItem
        {
            Id = 1,
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 42,
            Message = "fallback",
            SendOnDateTime = DateTimeOffset.UtcNow,
            ImageUrl = "https://cdn.example.com/image.jpg"
        };

        var scopeFactory = BuildScopeFactory(
            messageTemplate: new MessageTemplate
            {
                SocialMediaPlatformId = SocialMediaPlatformIds.Facebook,
                MessageType = MessageTemplates.MessageTypes.NewSyndicationFeedItem,
                Template = "{{ title }} - {{ url }} {{ image_url }}"
            },
            feedSource: new SyndicationFeedSource
            {
                Id = 42,
                FeedIdentifier = "feed-1",
                Title = "Post Title",
                Url = "https://example.com/post",
                PublicationDate = DateTimeOffset.UtcNow,
                AddedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow,
                CreatedByEntraOid = "test-oid"
            });

        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object, scopeFactory);

        var result = await sut.ComposeMessageAsync(scheduledItem);

        Assert.Equal("Post Title - https://example.com/post https://cdn.example.com/image.jpg", result);
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenTemplateMissing_ReturnsScheduledMessage()
    {
        var scheduledItem = new ScheduledItem
        {
            Id = 1,
            ItemType = Domain.Enums.ScheduledItemType.Engagements,
            ItemPrimaryKey = 42,
            Message = "fallback message",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        var scopeFactory = BuildScopeFactory(
            messageTemplate: null,
            engagement: new Engagement
            {
                Id = 42,
                Name = "Tech Conference 2026",
                Url = "https://conf.example.com",
                StartDateTime = DateTimeOffset.UtcNow,
                EndDateTime = DateTimeOffset.UtcNow.AddHours(1),
                TimeZoneId = "UTC"
            });

        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object, scopeFactory);

        var result = await sut.ComposeMessageAsync(scheduledItem);

        Assert.Equal("fallback message", result);
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenPlatformNotFound_ReturnsScheduledMessage()
    {
        var scheduledItem = new ScheduledItem
        {
            Id = 1,
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 42,
            Message = "fallback message",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        var scopeFactory = BuildScopeFactory(messageTemplate: null, platformFound: false);

        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object, scopeFactory);

        var result = await sut.ComposeMessageAsync(scheduledItem);

        Assert.Equal("fallback message", result);
    }

    [Fact]
    public async Task ComposeMessageAsync_WhenRenderingThrows_ReturnsScheduledMessage()
    {
        var scheduledItem = new ScheduledItem
        {
            Id = 1,
            ItemType = Domain.Enums.ScheduledItemType.YouTubeSources,
            ItemPrimaryKey = 42,
            Message = "fallback message",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        var failingYouTubeSourceManager = new Mock<IYouTubeSourceManager>();
        failingYouTubeSourceManager
            .Setup(m => m.GetAsync(42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var scopeFactory = BuildScopeFactory(
            messageTemplate: new MessageTemplate
            {
                SocialMediaPlatformId = SocialMediaPlatformIds.Facebook,
                MessageType = MessageTemplates.MessageTypes.NewYouTubeItem,
                Template = "{{ title }}"
            },
            youTubeSourceManager: failingYouTubeSourceManager);

        var sut = new FacebookManager(_httpClient, _mockFacebookSettings.Object, _mockLogger.Object, scopeFactory);

        var result = await sut.ComposeMessageAsync(scheduledItem);

        Assert.Equal("fallback message", result);
    }

    #endregion

    #region Exception Inheritance Tests

    [Fact]
    public void FacebookPostException_IsA_BroadcastingException()
    {
        var exception = new FacebookPostException("test message");

        Assert.IsAssignableFrom<BroadcastingException>(exception);
    }

    [Fact]
    public void BroadcastingException_PreservesApiErrorCode_AndApiErrorMessage()
    {
        var exception = new FacebookPostException("test message", apiErrorCode: 190, apiErrorMessage: "Invalid OAuth access token");

        Assert.Equal(190, exception.ApiErrorCode);
        Assert.Equal("Invalid OAuth access token", exception.ApiErrorMessage);
        Assert.Equal("test message", exception.Message);
    }

    #endregion

    private void SetupHttpMessageHandler(System.Net.HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }

    private static IServiceScopeFactory BuildScopeFactory(
        MessageTemplate? messageTemplate,
        SyndicationFeedSource? feedSource = null,
        Engagement? engagement = null,
        Talk? talk = null,
        YouTubeSource? youTubeSource = null,
        Mock<IYouTubeSourceManager>? youTubeSourceManager = null,
        bool platformFound = true)
    {
        var messageTemplateDataStore = new Mock<IMessageTemplateDataStore>();
        messageTemplateDataStore
            .Setup(m => m.GetAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageTemplate);

        var socialMediaPlatformManager = new Mock<ISocialMediaPlatformManager>();
        socialMediaPlatformManager
            .Setup(m => m.GetByNameAsync(
                MessageTemplates.Platforms.Facebook,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(platformFound
                ? new SocialMediaPlatform
                {
                    Id = SocialMediaPlatformIds.Facebook,
                    Name = MessageTemplates.Platforms.Facebook,
                    IsActive = true
                }
                : null);

        var engagementManager = new Mock<IEngagementManager>();
        engagementManager
            .Setup(m => m.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(engagement!);
        engagementManager
            .Setup(m => m.GetTalkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(talk!);

        var syndicationFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        syndicationFeedSourceManager
            .Setup(m => m.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedSource!);

        youTubeSourceManager ??= new Mock<IYouTubeSourceManager>();
        youTubeSourceManager
            .Setup(m => m.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(youTubeSource!);

        var services = new ServiceCollection();
        services.AddScoped(_ => messageTemplateDataStore.Object);
        services.AddScoped(_ => socialMediaPlatformManager.Object);
        services.AddScoped(_ => engagementManager.Object);
        services.AddScoped(_ => syndicationFeedSourceManager.Object);
        services.AddScoped(_ => youTubeSourceManager.Object);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}
