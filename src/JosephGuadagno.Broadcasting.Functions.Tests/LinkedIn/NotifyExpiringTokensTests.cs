using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.LinkedIn;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.LinkedIn;

public class NotifyExpiringTokensTests
{
    private readonly Mock<IUserOAuthTokenManager> _tokenManager;
    private readonly Mock<IApplicationUserDataStore> _userDataStore;
    private readonly Mock<IEmailTemplateManager> _emailTemplateManager;
    private readonly Mock<IEmailSender> _emailSender;
    private readonly Mock<IConfiguration> _configuration;
    private readonly Mock<ILogger<NotifyExpiringTokens>> _logger;
    private readonly NotifyExpiringTokens _sut;

    private static readonly TimerInfo FakeTimer = new();

    public NotifyExpiringTokensTests()
    {
        _tokenManager = new Mock<IUserOAuthTokenManager>();
        _userDataStore = new Mock<IApplicationUserDataStore>();
        _emailTemplateManager = new Mock<IEmailTemplateManager>();
        _emailSender = new Mock<IEmailSender>();
        _configuration = new Mock<IConfiguration>();
        _logger = new Mock<ILogger<NotifyExpiringTokens>>();
        _configuration.Setup(c => c["Settings:WebBaseUrl"]).Returns("https://example.com");

        _sut = new NotifyExpiringTokens(
            _tokenManager.Object,
            _userDataStore.Object,
            _emailTemplateManager.Object,
            _emailSender.Object,
            _logger.Object,
            _configuration.Object);
    }

    private static UserOAuthToken BuildToken(
        string oid = "test-oid",
        int platformId = 3,
        DateTimeOffset? lastNotifiedAt = null,
        DateTimeOffset? expiresAt = null) =>
        new()
        {
            CreatedByEntraOid = oid,
            SocialMediaPlatformId = platformId,
            AccessToken = "secret-access-token",
            AccessTokenExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(6),
            LastNotifiedAt = lastNotifiedAt
        };

    private static ApplicationUser BuildUser(string oid = "test-oid", string email = "user@example.com") =>
        new()
        {
            Id = 1,
            EntraObjectId = oid,
            DisplayName = "Test User",
            Email = email,
            ApprovalStatus = "Approved",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static EmailTemplate BuildTemplate(string name = "LinkedInTokenExpiring7Day") =>
        new()
        {
            Id = 1,
            Name = name,
            Subject = "Your LinkedIn token is expiring",
            Body = "Hello {{ display_name }}, your token expires {{ expires_at }}. <a href='{{ reauth_url }}'>Re-auth</a>",
            CreatedDate = DateTimeOffset.UtcNow,
            UpdatedDate = DateTimeOffset.UtcNow
        };

    // -----------------------------------------------------------------------
    // Scenario: no expiring tokens → nothing queued
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenNoExpiringTokens_DoesNotQueueAnyEmail()
    {
        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.RunAsync(FakeTimer);

        _emailSender.Verify(
            s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Scenario: token expiring, user not yet notified today → email queued and LastNotifiedAt updated
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenTokenExpiringAndNotNotifiedToday_QueuesEmailAndUpdatesLastNotifiedAt()
    {
        var token = BuildToken(lastNotifiedAt: null);
        var user = BuildUser();
        var template = BuildTemplate();

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync(token.CreatedByEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailSender
            .Setup(s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenManager
            .Setup(m => m.UpdateLastNotifiedAtAsync(token.CreatedByEntraOid, token.SocialMediaPlatformId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(FakeTimer);

        _emailSender.Verify(
            s => s.QueueEmail(
                It.Is<MailAddress>(a => a.Address == user.Email),
                template.Subject,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _tokenManager.Verify(
            m => m.UpdateLastNotifiedAtAsync(token.CreatedByEntraOid, token.SocialMediaPlatformId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    // -----------------------------------------------------------------------
    // Scenario: LastNotifiedAt is already today → email skipped
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenAlreadyNotifiedToday_SkipsToken()
    {
        var notifiedToday = DateTimeOffset.UtcNow.Date;
        var token = BuildToken(lastNotifiedAt: new DateTimeOffset(notifiedToday, TimeSpan.Zero));
        var template = BuildTemplate();

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        await _sut.RunAsync(FakeTimer);

        _emailSender.Verify(
            s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _tokenManager.Verify(
            m => m.UpdateLastNotifiedAtAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Scenario: LastNotifiedAt is yesterday → email IS sent (a new day)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenLastNotifiedAtIsYesterday_QueuesEmail()
    {
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        var token = BuildToken(lastNotifiedAt: yesterday);
        var user = BuildUser();
        var template = BuildTemplate();

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync(token.CreatedByEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailSender
            .Setup(s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenManager
            .Setup(m => m.UpdateLastNotifiedAtAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(FakeTimer);

        _emailSender.Verify(
            s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    // -----------------------------------------------------------------------
    // Scenario: template not found → emails skipped gracefully
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenTemplateNotFound_DoesNotQueueEmail()
    {
        var token = BuildToken(lastNotifiedAt: null);

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        await _sut.RunAsync(FakeTimer);

        _emailSender.Verify(
            s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Scenario: user not found (no account) → skipped gracefully
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenUserNotFound_SkipsNotificationGracefully()
    {
        var token = BuildToken(lastNotifiedAt: null);
        var template = BuildTemplate();

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync(token.CreatedByEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        await _sut.RunAsync(FakeTimer);

        _emailSender.Verify(
            s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Scenario: both 7-day and 1-day passes run; each uses the correct template
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_RunsBothPasses_WithCorrectTemplateNames()
    {
        var token7 = BuildToken(oid: "oid-7", expiresAt: DateTimeOffset.UtcNow.AddDays(6));
        var token1 = BuildToken(oid: "oid-1", expiresAt: DateTimeOffset.UtcNow.AddHours(20));
        var user7 = BuildUser(oid: "oid-7", email: "user7@example.com");
        var user1 = BuildUser(oid: "oid-1", email: "user1@example.com");

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(
                It.IsAny<DateTimeOffset>(),
                It.Is<DateTimeOffset>(t => t > DateTimeOffset.UtcNow.AddDays(3)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([token7]);

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(
                It.IsAny<DateTimeOffset>(),
                It.Is<DateTimeOffset>(t => t <= DateTimeOffset.UtcNow.AddDays(2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([token1]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync("LinkedInTokenExpiring7Day", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTemplate("LinkedInTokenExpiring7Day"));

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync("LinkedInTokenExpiring1Day", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTemplate("LinkedInTokenExpiring1Day"));

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync("oid-7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user7);

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync("oid-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);

        _emailSender
            .Setup(s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenManager
            .Setup(m => m.UpdateLastNotifiedAtAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(FakeTimer);

        _emailTemplateManager.Verify(m => m.GetTemplateAsync("LinkedInTokenExpiring7Day", It.IsAny<CancellationToken>()), Times.Once);
        _emailTemplateManager.Verify(m => m.GetTemplateAsync("LinkedInTokenExpiring1Day", It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Scenario: reauth_url in rendered email body is an absolute URL
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_WhenTokenExpiring_ReauthUrlInEmailBodyIsAbsolute()
    {
        var token = BuildToken(lastNotifiedAt: null);
        var user = BuildUser();
        var template = BuildTemplate();
        string? capturedBody = null;

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync(token.CreatedByEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailSender
            .Setup(s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MailAddress, string, string, CancellationToken>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        _tokenManager
            .Setup(m => m.UpdateLastNotifiedAtAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(FakeTimer);

        Assert.NotNull(capturedBody);
        Assert.Contains("https://example.com/LinkedIn", capturedBody);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_WhenWebBaseUrlIsMissingOrEmpty_LogsWarningAndUsesRelativeLink(string? configuredWebBaseUrl)
    {
        var token = BuildToken(lastNotifiedAt: null);
        var user = BuildUser();
        var template = BuildTemplate();
        string? capturedBody = null;

        _configuration.Setup(c => c["Settings:WebBaseUrl"]).Returns(configuredWebBaseUrl);

        _tokenManager
            .Setup(m => m.GetExpiringWindowAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        _emailTemplateManager
            .Setup(m => m.GetTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _userDataStore
            .Setup(m => m.GetByEntraObjectIdAsync(token.CreatedByEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailSender
            .Setup(s => s.QueueEmail(It.IsAny<MailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MailAddress, string, string, CancellationToken>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        _tokenManager
            .Setup(m => m.UpdateLastNotifiedAtAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(FakeTimer);

        VerifyWarningLogged("Settings:WebBaseUrl is not configured");
        Assert.NotNull(capturedBody);
        Assert.Contains("href='/LinkedIn'", capturedBody);
    }

    private void VerifyWarningLogged(string expectedMessage)
    {
        _logger.Verify(
            target => target.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(expectedMessage, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
