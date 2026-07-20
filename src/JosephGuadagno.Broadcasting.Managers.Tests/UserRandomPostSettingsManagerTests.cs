using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserRandomPostSettingsManagerTests
{
    private readonly Mock<IUserRandomPostSettingsDataStore> _dataStore = new();
    private readonly UserRandomPostSettingsManager _sut;

    public UserRandomPostSettingsManagerTests()
    {
        _sut = new UserRandomPostSettingsManager(_dataStore.Object);
    }

    [Fact]
    public async Task GetByUserAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var expected = new List<UserRandomPostSettings>
        {
            new() { CreatedByEntraOid = ownerOid, SocialMediaPlatformId = 2, CronExpression = "0 * * * *" }
        };

        _dataStore.Setup(d => d.GetByUserAsync(ownerOid, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByUserAsync(ownerOid, activeOnly: true);

        result.Should().BeEquivalentTo(expected);
        _dataStore.Verify(d => d.GetByUserAsync(ownerOid, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllActiveAsync_DelegatesToDataStore()
    {
        var expected = new List<UserRandomPostSettings>
        {
            new() { CreatedByEntraOid = "owner-1", SocialMediaPlatformId = 1, CronExpression = "0 * * * *" }
        };

        _dataStore.Setup(d => d.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAllDueAsync_DelegatesToDataStore()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var expected = new List<UserRandomPostSettings>
        {
            new() { Id = 1, CreatedByEntraOid = "owner-1", SocialMediaPlatformId = 1, CronExpression = "0 * * * *", IsActive = true }
        };

        _dataStore.Setup(d => d.GetAllDueAsync(utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllDueAsync(utcNow);

        result.Should().BeEquivalentTo(expected);
        _dataStore.Verify(d => d.GetAllDueAsync(utcNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllDueAsync_WhenNoneAreDue_ReturnsEmptyList()
    {
        var utcNow = DateTimeOffset.UtcNow;

        _dataStore.Setup(d => d.GetAllDueAsync(utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRandomPostSettings>());

        var result = await _sut.GetAllDueAsync(utcNow);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateNextRunAsync_WhenSuccessful_ReturnsTrue()
    {
        var nextRun = DateTimeOffset.UtcNow.AddHours(1);

        _dataStore.Setup(d => d.UpdateNextRunAsync(42, nextRun, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.UpdateNextRunAsync(42, nextRun);

        result.Should().BeTrue();
        _dataStore.Verify(d => d.UpdateNextRunAsync(42, nextRun, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateNextRunAsync_WhenRecordNotFound_ReturnsFalse()
    {
        _dataStore.Setup(d => d.UpdateNextRunAsync(99, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.UpdateNextRunAsync(99, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNextRunAsync_WithNullNextRunUtc_DelegatesToDataStore()
    {
        _dataStore.Setup(d => d.UpdateNextRunAsync(5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.UpdateNextRunAsync(5, null);

        result.Should().BeTrue();
        _dataStore.Verify(d => d.UpdateNextRunAsync(5, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateNextRunAsync_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int invalidId)
    {
        var act = async () => await _sut.UpdateNextRunAsync(invalidId, null);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        _dataStore.Verify(
            d => d.UpdateNextRunAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_DelegatesToDataStore()
    {
        var settings = new UserRandomPostSettings
        {
            CreatedByEntraOid = "owner-1",
            SocialMediaPlatformId = 2,
            CronExpression = "0 * * * *"
        };

        _dataStore.Setup(d => d.SaveAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await _sut.SaveAsync(settings);

        result.Should().Be(settings);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToDataStore()
    {
        _dataStore.Setup(d => d.DeleteAsync(42, "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(42, "owner-1");

        result.Should().BeTrue();
    }
}
