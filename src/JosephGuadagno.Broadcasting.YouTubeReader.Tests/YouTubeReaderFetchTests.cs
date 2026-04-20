using System.Net;
using System.Text;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.YouTubeReader.Tests;

public class YouTubeReaderFetchTests
{
    private const string OwnerEntraOid = "owner-entra-oid";

    private class QueueMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses;
        private readonly bool _throwOnSend;

        public QueueMessageHandler(IEnumerable<Func<HttpResponseMessage>> responses)
        {
            _responses = new Queue<Func<HttpResponseMessage>>(responses);
            _throwOnSend = false;
        }

        public QueueMessageHandler(bool throwOnSend)
        {
            _responses = new Queue<Func<HttpResponseMessage>>();
            _throwOnSend = throwOnSend;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throwOnSend)
            {
                throw new HttpRequestException("Network failure (simulated)");
            }

            if (_responses.Count == 0)
            {
                var emptyJson = "{ \"kind\": \"youtube#playlistItemListResponse\", \"items\": [], \"nextPageToken\": null }";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(emptyJson, Encoding.UTF8, "application/json")
                });
            }

            var factory = _responses.Dequeue();
            return Task.FromResult(factory());
        }
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public TestHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            return new ConfigurableHttpClient(new ConfigurableMessageHandler(_handler));
        }
    }

    private static YouTubeService BuildService(HttpMessageHandler handler)
    {
        var initializer = new BaseClientService.Initializer
        {
            ApiKey = "test-api-key",
            ApplicationName = "YouTubeReaderTests",
            HttpClientFactory = new TestHttpClientFactory(handler)
        };
        return new YouTubeService(initializer);
    }

    private static Mock<IYouTubeSettings> CreateSettings()
    {
        var settings = new Mock<IYouTubeSettings>();
        settings.SetupGet(s => s.ApiKey).Returns("test-api-key");
        settings.SetupGet(s => s.ChannelId).Returns("test-channel-id");
        settings.SetupGet(s => s.PlaylistId).Returns("test-playlist-id");
        settings.SetupProperty(s => s.ResultSetPageSize, 10);
        return settings;
    }

    private static Mock<ILogger<YouTubeReader>> CreateLogger() => new Mock<ILogger<YouTubeReader>>();

    [Fact]
    public async Task GetAsync_MultiPage_StopsWhenItemOlderThanSinceWhen_ReturnsExpected()
    {
        // Arrange
        var json1 = "{ \"kind\": \"youtube#playlistItemListResponse\", \"nextPageToken\": \"PAGE2\", \"items\": [ { \"kind\": \"youtube#playlistItem\", \"snippet\": { \"publishedAt\": \"2026-02-24T12:00:00Z\", \"title\": \"Video 1\", \"channelTitle\": \"MyChannel\", \"resourceId\": { \"kind\": \"youtube#video\", \"videoId\": \"vid1\" } } } ] }";
        var json2 = "{ \"kind\": \"youtube#playlistItemListResponse\", \"nextPageToken\": null, \"items\": [ { \"kind\": \"youtube#playlistItem\", \"snippet\": { \"publishedAt\": \"2026-02-20T12:00:00Z\", \"title\": \"Video 0\", \"channelTitle\": \"MyChannel\", \"resourceId\": { \"kind\": \"youtube#video\", \"videoId\": \"vid0\" } } } ] }";
        var page1 = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json1, Encoding.UTF8, "application/json") };
        var page2 = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json2, Encoding.UTF8, "application/json") };

        var handler = new QueueMessageHandler(new[] { page1, page2 });
        var service = BuildService(handler);
        var settings = CreateSettings();
        var logger = CreateLogger();
        var reader = new YouTubeReader(settings.Object, logger.Object, service);

        var sinceWhen = new DateTimeOffset(2026, 2, 22, 0, 0, 0, TimeSpan.Zero);

        // Act
        var results = await reader.GetAsync(OwnerEntraOid, sinceWhen);

        // Assert
        Assert.Single(results);
        var item = results[0];
        Assert.Equal("vid1", item.VideoId);
        Assert.Equal("MyChannel", item.Author);
        Assert.Equal("Video 1", item.Title);
        Assert.Equal("https://www.youtube.com/watch?v=vid1", item.Url);
        Assert.Equal(new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Utc), item.PublicationDate);
        Assert.Equal(item.PublicationDate, item.LastUpdatedOn);
    }

    [Fact]
    public async Task GetAsync_SkipsItemsWithWrongKind()
    {
        // Arrange
        var json = "{ \"nextPageToken\": null, \"items\": [ { \"kind\": \"youtube#notPlaylistItem\", \"snippet\": { \"publishedAt\": \"2026-02-24T12:00:00Z\", \"title\": \"X\", \"channelTitle\": \"C\", \"resourceId\": { \"kind\": \"youtube#video\", \"videoId\": \"vid\" } } } ] }";
        var page = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var handler = new QueueMessageHandler(new[] { page });
        var service = BuildService(handler);
        var settings = CreateSettings();
        var logger = CreateLogger();
        var reader = new YouTubeReader(settings.Object, logger.Object, service);

        // Act
        var results = await reader.GetAsync(OwnerEntraOid, DateTimeOffset.UtcNow.AddDays(-10));

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAsync_SkipsItemsWithoutPublishedAt()
    {
        // Arrange
        var json = "{ \"nextPageToken\": null, \"items\": [ { \"kind\": \"youtube#playlistItem\", \"snippet\": { \"title\": \"X\", \"channelTitle\": \"C\", \"resourceId\": { \"kind\": \"youtube#video\", \"videoId\": \"vid\" } } } ] }";
        var page = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var handler = new QueueMessageHandler(new[] { page });
        var service = BuildService(handler);
        var settings = CreateSettings();
        var logger = CreateLogger();
        var reader = new YouTubeReader(settings.Object, logger.Object, service);

        // Act
        var results = await reader.GetAsync(OwnerEntraOid, DateTimeOffset.UtcNow.AddDays(-10));

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAsync_OnHttpFailure_ThrowsAndLogs()
    {
        // Arrange
        var handler = new QueueMessageHandler(throwOnSend: true);
        var service = BuildService(handler);
        var settings = CreateSettings();
        var logger = CreateLogger();
        var reader = new YouTubeReader(settings.Object, logger.Object, service);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => reader.GetAsync(OwnerEntraOid, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public void GetSinceDate_UsesGetAsync()
    {
        // Arrange: empty response so no items
        var json = "{ \"nextPageToken\": null, \"items\": [] }";
        var page = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var handler = new QueueMessageHandler(new[] { page });
        var service = BuildService(handler);
        var settings = CreateSettings();
        var logger = CreateLogger();
        var reader = new YouTubeReader(settings.Object, logger.Object, service);

        // Act
        var results = reader.GetSinceDate(OwnerEntraOid, DateTimeOffset.UtcNow.AddDays(-30));

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAsync_WithOwnerOid_ShouldApplyNonEmptyOwnerToEveryItem()
    {
        // Arrange
        var json = "{ \"kind\": \"youtube#playlistItemListResponse\", \"nextPageToken\": null, \"items\": [ { \"kind\": \"youtube#playlistItem\", \"snippet\": { \"publishedAt\": \"2026-02-24T12:00:00Z\", \"title\": \"Owned Video\", \"channelTitle\": \"MyChannel\", \"resourceId\": { \"kind\": \"youtube#video\", \"videoId\": \"owned-vid\" } } } ] }";
        var page = () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var handler = new QueueMessageHandler(new[] { page });
        var service = BuildService(handler);
        var settings = CreateSettings();
        var logger = CreateLogger();
        var reader = new YouTubeReader(settings.Object, logger.Object, service);

        // Act
        var results = await reader.GetAsync(OwnerEntraOid, new DateTimeOffset(2026, 2, 22, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Single(results);
        Assert.All(results, item =>
        {
            Assert.Equal(OwnerEntraOid, item.CreatedByEntraOid);
            Assert.False(string.IsNullOrWhiteSpace(item.CreatedByEntraOid));
        });
    }
}
