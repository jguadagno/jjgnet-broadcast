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
