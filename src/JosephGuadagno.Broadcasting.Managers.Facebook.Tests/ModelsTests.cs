using System;
using JosephGuadagno.Broadcasting.Managers.Facebook.Models;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

public class ModelsTests
{
    [Fact]
    public void FacebookApplicationSettings_Constructor_SetsDefaults()
    {
        // Act
        var sut = new FacebookApplicationSettings();

        // Assert
        Assert.Equal(string.Empty, sut.PageId);
        Assert.Equal(string.Empty, sut.PageAccessToken);
        Assert.Equal(string.Empty, sut.AppId);
        Assert.Equal(string.Empty, sut.AppSecret);
        Assert.Equal(string.Empty, sut.ClientToken);
        Assert.Equal(string.Empty, sut.ShortLivedAccessToken);
        Assert.Equal(string.Empty, sut.LongLivedAccessToken);
        Assert.Equal("https://graph.facebook.com", sut.GraphApiRootUrl);
        Assert.Equal("v20.0", sut.GraphApiVersion);
    }

    [Fact]
    public void FacebookApplicationSettings_Properties_CanSetAndGet()
    {
        // Arrange
        var sut = new FacebookApplicationSettings();

        // Act
        sut.PageId = "page-id";
        sut.PageAccessToken = "page-token";
        sut.AppId = "app-id";
        sut.AppSecret = "app-secret";
        sut.ClientToken = "client-token";
        sut.ShortLivedAccessToken = "short-token";
        sut.LongLivedAccessToken = "long-token";
        sut.GraphApiRootUrl = "https://new-graph.facebook.com";
        sut.GraphApiVersion = "v21.0";

        // Assert
        Assert.Equal("page-id", sut.PageId);
        Assert.Equal("page-token", sut.PageAccessToken);
        Assert.Equal("app-id", sut.AppId);
        Assert.Equal("app-secret", sut.AppSecret);
        Assert.Equal("client-token", sut.ClientToken);
        Assert.Equal("short-token", sut.ShortLivedAccessToken);
        Assert.Equal("long-token", sut.LongLivedAccessToken);
        Assert.Equal("https://new-graph.facebook.com", sut.GraphApiRootUrl);
        Assert.Equal("v21.0", sut.GraphApiVersion);
    }

    [Fact]
    public void TokenInfo_Properties_CanSetAndGet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var sut = new TokenInfo
        {
            AccessToken = "token",
            TokenType = "bearer",
            ExpiresOn = now
        };

        // Assert
        Assert.Equal(now, sut.ExpiresOn);
        Assert.Equal("token", sut.AccessToken);
        Assert.Equal("bearer", sut.TokenType);
    }

    [Fact]
    public void TokenResponse_Properties_CanSetAndGet()
    {
        // Arrange
        var sut = new TokenResponse
        {
            AccessToken = "token",
            TokenType = "bearer",
            ExpiresIn = 3600
        };

        // Assert
        Assert.Equal("token", sut.AccessToken);
        Assert.Equal("bearer", sut.TokenType);
        Assert.Equal(3600, sut.ExpiresIn);
    }

    [Fact]
    public void FacebookPostError_Properties_CanSetAndGet()
    {
        // Arrange
        var sut = new FacebookPostError
        {
            Message = "error message",
            Type = "OAuthException",
            Code = 100,
            SubCode = 200,
            FacebookTraceId = "trace-id"
        };

        // Assert
        Assert.Equal("error message", sut.Message);
        Assert.Equal("OAuthException", sut.Type);
        Assert.Equal(100, sut.Code);
        Assert.Equal(200, sut.SubCode);
        Assert.Equal("trace-id", sut.FacebookTraceId);
    }

    [Fact]
    public void PostStatusResponse_Properties_CanSetAndGet()
    {
        // Arrange
        var error = new FacebookPostError { Message = "error" };
        var sut = new PostStatusResponse
        {
            Id = "post-id",
            Error = error
        };

        // Assert
        Assert.Equal("post-id", sut.Id);
        Assert.Equal(error, sut.Error);
    }
}