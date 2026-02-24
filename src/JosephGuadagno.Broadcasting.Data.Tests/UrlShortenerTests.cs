using System.Net;
using System.Text;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Utilities.Web.Shortener;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests;

public class UrlShortenerTests
{
    private readonly Mock<ILogger<UrlShortener>> _loggerMock;

    public UrlShortenerTests()
    {
        _loggerMock = new Mock<ILogger<UrlShortener>>();
    }

    private static Bitly CreateBitly(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api-ssl.bitly.com/v4/") };
        var config = new BitlyConfiguration { Token = "fake-token", ApiRootUri = "https://api-ssl.bitly.com/v4/" };
        return new Bitly(httpClient, config);
    }

    [Fact]
    public async Task GetShortenedUrlAsync_NullUrl_ReturnsNull()
    {
        // Arrange
        var bitly = CreateBitly(new MockHttpMessageHandler(HttpStatusCode.OK, "{}"));
        var sut = new UrlShortener(bitly, _loggerMock.Object);

        // Act
        var result = await sut.GetShortenedUrlAsync(null!, "bit.ly");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetShortenedUrlAsync_EmptyUrl_ReturnsNull()
    {
        // Arrange
        var bitly = CreateBitly(new MockHttpMessageHandler(HttpStatusCode.OK, "{}"));
        var sut = new UrlShortener(bitly, _loggerMock.Object);

        // Act
        var result = await sut.GetShortenedUrlAsync(string.Empty, "bit.ly");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetShortenedUrlAsync_ValidUrl_BitlyReturnsResponse_ReturnsShortLink()
    {
        // Arrange
        const string shortUrl = "https://bit.ly/3xABCDE";
        var responseJson = $"{{\"link\":\"{shortUrl}\",\"id\":\"bit.ly/3xABCDE\",\"long_url\":\"https://example.com\"}}";
        var bitly = CreateBitly(new MockHttpMessageHandler(HttpStatusCode.Created, responseJson));
        var sut = new UrlShortener(bitly, _loggerMock.Object);

        // Act
        var result = await sut.GetShortenedUrlAsync("https://example.com", "bit.ly");

        // Assert
        Assert.Equal(shortUrl, result);
    }

    [Fact]
    public async Task GetShortenedUrlAsync_ValidUrl_BitlyReturnsNull_ReturnsOriginalUrl()
    {
        // Arrange
        const string originalUrl = "https://example.com";
        var bitly = CreateBitly(new MockHttpMessageHandler(HttpStatusCode.BadRequest, "{}"));
        var sut = new UrlShortener(bitly, _loggerMock.Object);

        // Act
        var result = await sut.GetShortenedUrlAsync(originalUrl, "bit.ly");

        // Assert
        Assert.Equal(originalUrl, result);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
