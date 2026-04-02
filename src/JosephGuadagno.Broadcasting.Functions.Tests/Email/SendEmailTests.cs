using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using EmailModel = JosephGuadagno.Broadcasting.Domain.Models.Messages.Email;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Email;

/// <summary>
/// Unit tests for the SendEmail Azure Function (Issue #618).
///
/// Design notes:
///   - Messages arrive Base64-encoded JSON on the send-email queue.
///   - Invalid/undeserializable messages are logged and silently dropped (no retry).
///   - ACS client failures propagate so the Functions host can retry / route to poison queue.
///   - The From address must come from the queued Email model, never from a hardcoded setting.
/// </summary>
public class SendEmailTests
{
    private readonly Mock<EmailClient> _mockEmailClient;
    private readonly Mock<FunctionContext> _mockFunctionContext;

    public SendEmailTests()
    {
        _mockEmailClient = new Mock<EmailClient>();
        _mockFunctionContext = new Mock<FunctionContext>();

        var mockOperation = new Mock<EmailSendOperation>();
        mockOperation.Setup(o => o.Id).Returns("test-operation-id");

        _mockEmailClient
            .Setup(c => c.SendAsync(
                WaitUntil.Started,
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOperation.Object);
    }

    private Functions.Email.SendEmail BuildSut() => new(
        _mockEmailClient.Object,
        NullLogger<Functions.Email.SendEmail>.Instance);

    private static string BuildBase64JsonMessage(EmailModel email)
    {
        var json = JsonSerializer.Serialize(email);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static EmailModel BuildTestEmail(
        string from = "sender@example.com",
        string fromDisplay = "Sender",
        string to = "recipient@example.com",
        string toDisplay = "Recipient",
        string subject = "Test Subject",
        string body = "<p>Test Body</p>") => new()
    {
        FromMailAddress = from,
        FromDisplayName = fromDisplay,
        ToMailAddress = to,
        ToDisplayName = toDisplay,
        ReplyToMailAddress = from,
        ReplyToDisplayName = fromDisplay,
        Subject = subject,
        Body = body
    };

    [Fact]
    public async Task Run_ValidBase64JsonMessage_CallsEmailClientSendAsync()
    {
        // Arrange
        var email = BuildTestEmail();
        var message = BuildBase64JsonMessage(email);
        var sut = BuildSut();

        // Act
        await sut.Run(message, _mockFunctionContext.Object);

        // Assert
        _mockEmailClient.Verify(
            c => c.SendAsync(
                WaitUntil.Started,
                It.Is<EmailMessage>(m =>
                    m.Recipients.To.Any(r => r.Address == email.ToMailAddress) &&
                    m.Content.Subject == email.Subject),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_ValidMessage_ExtractsFromAddressFromEmail()
    {
        // Arrange: the From address must come from the Email model, never from a hardcoded default
        var email = BuildTestEmail(from: "custom-from@domain.com", fromDisplay: "Custom Sender");
        var message = BuildBase64JsonMessage(email);

        EmailMessage? capturedMessage = null;
        var mockOperation = new Mock<EmailSendOperation>();
        mockOperation.Setup(o => o.Id).Returns("op-id");

        _mockEmailClient
            .Setup(c => c.SendAsync(
                WaitUntil.Started,
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<WaitUntil, EmailMessage, CancellationToken>(
                (_, m, _) => capturedMessage = m)
            .ReturnsAsync(mockOperation.Object);

        var sut = BuildSut();

        // Act
        await sut.Run(message, _mockFunctionContext.Object);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.SenderAddress.Should().Be("custom-from@domain.com");
    }

    [Fact]
    public async Task Run_InvalidBase64_DoesNotCallEmailClient()
    {
        // Arrange: a string that is neither valid Base64 nor valid JSON.
        // By design the implementation catches all deserialization failures, logs, and returns early.
        const string malformedMessage = "this-is-not-base64-or-json!!!!!";
        var sut = BuildSut();

        // Act
        var exception = await Record.ExceptionAsync(
            () => sut.Run(malformedMessage, _mockFunctionContext.Object));

        // Assert
        exception.Should().BeNull();
        _mockEmailClient.Verify(
            c => c.SendAsync(
                It.IsAny<WaitUntil>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Run_EmailClientThrows_ExceptionPropagates()
    {
        // Arrange: ACS failures must propagate so the Functions host retries / routes to poison queue
        var email = BuildTestEmail();
        var message = BuildBase64JsonMessage(email);

        _mockEmailClient
            .Setup(c => c.SendAsync(
                WaitUntil.Started,
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ACS service unavailable"));

        var sut = BuildSut();

        // Act
        Func<Task> act = async () => await sut.Run(message, _mockFunctionContext.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("ACS service unavailable");
    }
}
