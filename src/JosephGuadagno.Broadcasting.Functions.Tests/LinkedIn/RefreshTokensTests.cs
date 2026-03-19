using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class RefreshTokensTests
{
    private readonly Mock<ILinkedInManager> _linkedInManager = new();
    private readonly Mock<ILinkedInApplicationSettings> _linkedInSettings = new();
    private readonly Mock<ITokenRefreshManager> _tokenRefreshManager = new();
    private readonly Mock<IKeyVault> _keyVault = new();

    private Functions.LinkedIn.RefreshTokens BuildSut() => new(
        _linkedInManager.Object,
        _linkedInSettings.Object,
        _tokenRefreshManager.Object,
        _keyVault.Object,
        NullLogger<Functions.LinkedIn.RefreshTokens>.Instance);

    private static KeyVaultSecret BuildRefreshTokenSecret(string value = "valid-refresh-token") =>
        new("jjg-net-linkedin-refresh-token", value);

    private static TokenRefresh BuildTokenInfo(DateTime expires) => new()
    {
        Id = 1,
        Name = "LinkedIn",
        Expires = expires,
        LastChecked = DateTime.UtcNow,
        LastRefreshed = DateTime.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };

    private static LinkedInTokenInfo BuildNewTokenInfo() => new()
    {
        AccessToken = "new-access-token-xyz",
        ExpiresOn = DateTime.UtcNow.AddDays(60)
    };

    // ── Token still valid (5+ days remaining) ────────────────────────────────

    [Fact]
    public async Task Run_WhenTokenExpiresInMoreThan5Days_DoesNotRefresh()
    {
        // Arrange — token expires in 6 days; above the 5-day threshold
        var tokenInfo = BuildTokenInfo(DateTime.UtcNow.AddDays(6));

        _keyVault.Setup(m => m.GetSecretAsync("jjg-net-linkedin-refresh-token"))
            .ReturnsAsync(BuildRefreshTokenSecret());
        _tokenRefreshManager.Setup(m => m.GetByNameAsync("LinkedIn")).ReturnsAsync(tokenInfo);
        _linkedInSettings.Setup(m => m.ClientId).Returns("client-id");
        _linkedInSettings.Setup(m => m.ClientSecret).Returns("client-secret");
        _linkedInSettings.Setup(m => m.AccessTokenUrl).Returns("https://www.linkedin.com/oauth/v2/accessToken");

        var sut = BuildSut();

        // Act
        await sut.Run(null!);

        // Assert — no refresh should be triggered
        _linkedInManager.Verify(
            m => m.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _keyVault.Verify(
            m => m.UpdateSecretValueAndPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    // ── Token expiring within 5 days ──────────────────────────────────────────

    [Fact]
    public async Task Run_WhenTokenExpiresInLessThan5Days_TriggersRefresh()
    {
        // Arrange — token expires in 4 days; within the 5-day threshold
        var tokenInfo = BuildTokenInfo(DateTime.UtcNow.AddDays(4));
        var newTokenInfo = BuildNewTokenInfo();

        _keyVault.Setup(m => m.GetSecretAsync("jjg-net-linkedin-refresh-token"))
            .ReturnsAsync(BuildRefreshTokenSecret("my-refresh-token"));
        _tokenRefreshManager.Setup(m => m.GetByNameAsync("LinkedIn")).ReturnsAsync(tokenInfo);
        _tokenRefreshManager.Setup(m => m.SaveAsync(It.IsAny<TokenRefresh>())).ReturnsAsync(tokenInfo);
        _linkedInSettings.Setup(m => m.ClientId).Returns("client-id");
        _linkedInSettings.Setup(m => m.ClientSecret).Returns("client-secret");
        _linkedInSettings.Setup(m => m.AccessTokenUrl).Returns("https://www.linkedin.com/oauth/v2/accessToken");
        _linkedInManager
            .Setup(m => m.RefreshTokenAsync("client-id", "client-secret", "my-refresh-token", "https://www.linkedin.com/oauth/v2/accessToken"))
            .ReturnsAsync(newTokenInfo);
        _keyVault
            .Setup(m => m.UpdateSecretValueAndPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var sut = BuildSut();

        // Act
        await sut.Run(null!);

        // Assert — refresh was triggered and new access token saved
        _linkedInManager.Verify(
            m => m.RefreshTokenAsync("client-id", "client-secret", "my-refresh-token", "https://www.linkedin.com/oauth/v2/accessToken"),
            Times.Once);
        _keyVault.Verify(
            m => m.UpdateSecretValueAndPropertiesAsync("jjg-net-linkedin-access-token", "new-access-token-xyz", It.IsAny<DateTime>()),
            Times.Once);
        _tokenRefreshManager.Verify(m => m.SaveAsync(It.IsAny<TokenRefresh>()), Times.Once);
    }

    // ── Token already expired ─────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenTokenAlreadyExpired_TriggersRefresh()
    {
        // Arrange — token expired yesterday
        var tokenInfo = BuildTokenInfo(DateTime.UtcNow.AddDays(-1));
        var newTokenInfo = BuildNewTokenInfo();

        _keyVault.Setup(m => m.GetSecretAsync("jjg-net-linkedin-refresh-token"))
            .ReturnsAsync(BuildRefreshTokenSecret());
        _tokenRefreshManager.Setup(m => m.GetByNameAsync("LinkedIn")).ReturnsAsync(tokenInfo);
        _tokenRefreshManager.Setup(m => m.SaveAsync(It.IsAny<TokenRefresh>())).ReturnsAsync(tokenInfo);
        _linkedInSettings.Setup(m => m.ClientId).Returns("client-id");
        _linkedInSettings.Setup(m => m.ClientSecret).Returns("client-secret");
        _linkedInSettings.Setup(m => m.AccessTokenUrl).Returns("https://www.linkedin.com/oauth/v2/accessToken");
        _linkedInManager
            .Setup(m => m.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(newTokenInfo);
        _keyVault
            .Setup(m => m.UpdateSecretValueAndPropertiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var sut = BuildSut();

        // Act
        await sut.Run(null!);

        // Assert — refresh was triggered
        _linkedInManager.Verify(
            m => m.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    // ── Refresh API throws ────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenRefreshApiThrows_DoesNotThrow()
    {
        // Arrange — token expired so refresh will be attempted, but API throws
        var tokenInfo = BuildTokenInfo(DateTime.MinValue);

        _keyVault.Setup(m => m.GetSecretAsync("jjg-net-linkedin-refresh-token"))
            .ReturnsAsync(BuildRefreshTokenSecret());
        _tokenRefreshManager.Setup(m => m.GetByNameAsync("LinkedIn")).ReturnsAsync(tokenInfo);
        _linkedInSettings.Setup(m => m.ClientId).Returns("client-id");
        _linkedInSettings.Setup(m => m.ClientSecret).Returns("client-secret");
        _linkedInSettings.Setup(m => m.AccessTokenUrl).Returns("https://www.linkedin.com/oauth/v2/accessToken");
        _linkedInManager
            .Setup(m => m.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("LinkedIn API unavailable"));

        var sut = BuildSut();

        // Act & Assert — should NOT throw; error is caught and logged
        var exception = await Record.ExceptionAsync(() => sut.Run(null!));
        Assert.Null(exception);
    }

    // ── Key Vault failure ─────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenKeyVaultGetSecretThrows_DoesNotThrow()
    {
        // Arrange — Key Vault is unavailable
        _keyVault.Setup(m => m.GetSecretAsync("jjg-net-linkedin-refresh-token"))
            .ThrowsAsync(new Exception("Key Vault unreachable"));

        var sut = BuildSut();

        // Act & Assert — should NOT throw; error is caught and logged
        var exception = await Record.ExceptionAsync(() => sut.Run(null!));
        Assert.Null(exception);
    }
}
