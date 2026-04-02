using System;
using System.Net.Mail;
using System.Text;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class EmailSenderTests
{
    private readonly Mock<QueueServiceClient> _mockQueueServiceClient;
    private readonly Mock<QueueClient> _mockQueueClient;
    private readonly Mock<IEmailSettings> _mockEmailSettings;
    private readonly Mock<ILogger<EmailSender>> _mockLogger;
    private readonly EmailSender _sut;

    public EmailSenderTests()
    {
        _mockQueueServiceClient = new Mock<QueueServiceClient>();
        _mockQueueClient = new Mock<QueueClient>();
        _mockEmailSettings = new Mock<IEmailSettings>();
        _mockLogger = new Mock<ILogger<EmailSender>>();

        _mockQueueServiceClient
            .Setup(x => x.GetQueueClient(Queues.SendEmail))
            .Returns(_mockQueueClient.Object);

        _mockEmailSettings.Setup(x => x.FromAddress).Returns("noreply@example.com");
        _mockEmailSettings.Setup(x => x.FromDisplayName).Returns("Default Sender");
        _mockEmailSettings.Setup(x => x.ReplyToAddress).Returns("replyto@example.com");
        _mockEmailSettings.Setup(x => x.ReplyToDisplayName).Returns("Default ReplyTo");

        _sut = new EmailSender(_mockQueueServiceClient.Object, _mockEmailSettings.Object, _mockLogger.Object);
    }

    private void SetupQueueToSucceed()
    {
        _mockQueueClient
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<Response<SendReceipt>>().Object);
    }

    private static string DecodeBase64Message(string base64) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(base64));

    [Fact]
    public async Task QueueEmail_WithMailAddress_QueuesEmailMessage()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");
        var subject = "Test Subject";
        var body = "<p>Test body</p>";
        string? capturedBase64 = null;

        _mockQueueClient
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((msg, _) => capturedBase64 = msg)
            .ReturnsAsync(new Mock<Response<SendReceipt>>().Object);

        // Act
        await _sut.QueueEmail(toAddress, subject, body);

        // Assert
        _mockQueueClient.Verify(
            x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        capturedBase64.Should().NotBeNull();
        var json = DecodeBase64Message(capturedBase64!);
        json.Should().Contain("user@example.com");
        json.Should().Contain("Test Subject");
    }

    [Fact]
    public async Task QueueEmail_WithCustomFromAddress_UsesProvidedFromAddress()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");
        var fromAddress = new MailAddress("custom@sender.com", "Custom Sender");
        var replyToAddress = new MailAddress("custom-reply@sender.com", "Custom ReplyTo");
        string? capturedBase64 = null;

        _mockQueueClient
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((msg, _) => capturedBase64 = msg)
            .ReturnsAsync(new Mock<Response<SendReceipt>>().Object);

        // Act
        await _sut.QueueEmail(toAddress, "Custom From Test", "<p>Body</p>", fromAddress, replyToAddress);

        // Assert
        capturedBase64.Should().NotBeNull();
        var json = DecodeBase64Message(capturedBase64!);
        json.Should().Contain("custom@sender.com");
        json.Should().Contain("Custom Sender");
        json.Should().Contain("custom-reply@sender.com");
        json.Should().Contain("Custom ReplyTo");
    }

    [Fact]
    public async Task QueueEmail_UsesDefaultsFromSettings_WhenNoFromProvided()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");
        string? capturedBase64 = null;

        _mockQueueClient
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((msg, _) => capturedBase64 = msg)
            .ReturnsAsync(new Mock<Response<SendReceipt>>().Object);

        // Act
        await _sut.QueueEmail(toAddress, "Subject", "Body");

        // Assert
        capturedBase64.Should().NotBeNull();
        var json = DecodeBase64Message(capturedBase64!);
        json.Should().Contain("noreply@example.com");
        json.Should().Contain("Default Sender");
        json.Should().Contain("replyto@example.com");
        json.Should().Contain("Default ReplyTo");
    }

    [Fact]
    public async Task SendEmailAsync_QueuesEmailMessage()
    {
        // Arrange
        var emailAddress = "user@example.com";
        var subject = "Async Subject";
        var htmlMessage = "<p>HTML body</p>";
        SetupQueueToSucceed();

        // Act
        await _sut.SendEmailAsync(emailAddress, subject, htmlMessage);

        // Assert
        _mockQueueClient.Verify(
            x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task QueueEmail_WhenQueueFails_ThrowsException()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");

        _mockQueueClient
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));

        // Act
        Func<Task> act = async () => await _sut.QueueEmail(toAddress, "Subject", "Body");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Queue unavailable");
    }
}
