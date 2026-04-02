using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class EmailTemplateManagerTests
{
    private readonly Mock<IEmailTemplateDataStore> _mockDataStore;
    private readonly Mock<ILogger<EmailTemplateManager>> _mockLogger;
    private readonly EmailTemplateManager _sut;

    public EmailTemplateManagerTests()
    {
        _mockDataStore = new Mock<IEmailTemplateDataStore>();
        _mockLogger = new Mock<ILogger<EmailTemplateManager>>();
        _sut = new EmailTemplateManager(_mockDataStore.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetTemplateAsync_ReturnsTemplate_WhenExists()
    {
        // Arrange
        var expected = new EmailTemplate
        {
            Id = 1,
            Name = "UserApproved",
            Subject = "You are approved",
            Body = "<p>Welcome!</p>",
            CreatedDate = DateTimeOffset.UtcNow,
            UpdatedDate = DateTimeOffset.UtcNow
        };
        _mockDataStore.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(expected);

        // Act
        var result = await _sut.GetTemplateAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("UserApproved");
        _mockDataStore.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetTemplateAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _mockDataStore.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((EmailTemplate?)null);

        // Act
        var result = await _sut.GetTemplateAsync(999);

        // Assert
        result.Should().BeNull();
        _mockDataStore.Verify(x => x.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_ReturnsAllTemplates()
    {
        // Arrange
        var templates = new List<EmailTemplate>
        {
            new() { Id = 1, Name = "UserApproved", Subject = "Approved", Body = "<p>Approved</p>" },
            new() { Id = 2, Name = "UserRejected", Subject = "Rejected", Body = "<p>Rejected</p>" }
        };
        _mockDataStore.Setup(x => x.GetAllAsync()).ReturnsAsync(templates);

        // Act
        var result = await _sut.GetAllTemplatesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("UserApproved");
        result[1].Name.Should().Be("UserRejected");
        _mockDataStore.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
