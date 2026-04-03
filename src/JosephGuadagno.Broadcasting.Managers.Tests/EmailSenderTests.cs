using System.Net.Mail;

using Azure.Storage.Queues.Models;
using FluentAssertions;
using JosephGuadagno.AzureHelpers.Storage.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models.Messages;

using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class EmailSenderTests
{
    private readonly Mock<IQueue> _mockEmailQueue;
    private readonly Mock<IEmailSettings> _mockEmailSettings;
    private readonly Mock<ILogger<EmailSender>> _mockLogger;
    private readonly EmailSender _sut;

    public EmailSenderTests()
    {
        _mockEmailQueue = new Mock<IQueue>();
        _mockEmailSettings = new Mock<IEmailSettings>();
        _mockLogger = new Mock<ILogger<EmailSender>>();

        _mockEmailSettings.Setup(x => x.FromAddress).Returns("noreply@example.com");
        _mockEmailSettings.Setup(x => x.FromDisplayName).Returns("Default Sender");
        _mockEmailSettings.Setup(x => x.ReplyToAddress).Returns("replyto@example.com");
        _mockEmailSettings.Setup(x => x.ReplyToDisplayName).Returns("Default ReplyTo");

        _mockEmailQueue
            .Setup(x => x.AddMessageAsync(It.IsAny<Email>()))
            .ReturnsAsync((SendReceipt?)null!);

        _sut = new EmailSender(_mockEmailQueue.Object, _mockEmailSettings.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task QueueEmail_WithMailAddress_QueuesEmailMessage()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");
        var subject = "Test Subject";
        var body = "<p>Test body</p>";
        Email? capturedEmail = null;

        _mockEmailQueue
            .Setup(x => x.AddMessageAsync(It.IsAny<Email>()))
            .Callback<Email>(email => capturedEmail = email)
            .ReturnsAsync((SendReceipt?)null!);

        // Act
        await _sut.QueueEmail(toAddress, subject, body);

        // Assert
        _mockEmailQueue.Verify(x => x.AddMessageAsync(It.IsAny<Email>()), Times.Once);
        capturedEmail.Should().NotBeNull();
        capturedEmail!.ToMailAddress.Should().Be("user@example.com");
        capturedEmail.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task QueueEmail_WithCustomFromAddress_UsesProvidedFromAddress()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");
        var fromAddress = new MailAddress("custom@sender.com", "Custom Sender");
        var replyToAddress = new MailAddress("custom-reply@sender.com", "Custom ReplyTo");
        Email? capturedEmail = null;

        _mockEmailQueue
            .Setup(x => x.AddMessageAsync(It.IsAny<Email>()))
            .Callback<Email>(email => capturedEmail = email)
            .ReturnsAsync((SendReceipt?)null!);

        // Act
        await _sut.QueueEmail(toAddress, "Custom From Test", "<p>Body</p>", fromAddress, replyToAddress);

        // Assert
        capturedEmail.Should().NotBeNull();
        capturedEmail!.FromMailAddress.Should().Be("custom@sender.com");
        capturedEmail.FromDisplayName.Should().Be("Custom Sender");
        capturedEmail.ReplyToMailAddress.Should().Be("custom-reply@sender.com");
        capturedEmail.ReplyToDisplayName.Should().Be("Custom ReplyTo");
    }

    [Fact]
    public async Task QueueEmail_UsesDefaultsFromSettings_WhenNoFromProvided()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");
        Email? capturedEmail = null;

        _mockEmailQueue
            .Setup(x => x.AddMessageAsync(It.IsAny<Email>()))
            .Callback<Email>(email => capturedEmail = email)
            .ReturnsAsync((SendReceipt?)null!);

        // Act
        await _sut.QueueEmail(toAddress, "Subject", "Body");

        // Assert
        capturedEmail.Should().NotBeNull();
        capturedEmail!.FromMailAddress.Should().Be("noreply@example.com");
        capturedEmail.FromDisplayName.Should().Be("Default Sender");
        capturedEmail.ReplyToMailAddress.Should().Be("replyto@example.com");
        capturedEmail.ReplyToDisplayName.Should().Be("Default ReplyTo");
    }

    [Fact]
    public async Task SendEmailAsync_QueuesEmailMessage()
    {
        // Arrange
        var emailAddress = "user@example.com";
        var subject = "Async Subject";
        var htmlMessage = "<p>HTML body</p>";

        // Act
        await _sut.SendEmailAsync(emailAddress, subject, htmlMessage);

        // Assert
        _mockEmailQueue.Verify(
            x => x.AddMessageAsync(It.Is<Email>(e => e.ToMailAddress == emailAddress && e.Subject == subject)),
            Times.Once);
    }

    [Fact]
    public async Task QueueEmail_WhenQueueFails_ThrowsException()
    {
        // Arrange
        var toAddress = new MailAddress("user@example.com", "Test User");

        _mockEmailQueue
            .Setup(x => x.AddMessageAsync(It.IsAny<Email>()))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));

        // Act
        Func<Task> act = async () => await _sut.QueueEmail(toAddress, "Subject", "Body");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Queue unavailable");
    }
}