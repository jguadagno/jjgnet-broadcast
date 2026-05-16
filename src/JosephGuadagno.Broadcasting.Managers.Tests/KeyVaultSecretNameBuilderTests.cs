using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Utilities;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class KeyVaultSecretNameBuilderTests
{
    [Theory]
    [InlineData(KeyVaultSecretOwnerType.Publisher, "owner-1", KeyVaultSecretNames.Platform.Bluesky, KeyVaultSecretNames.SettingName.AppPassword, "publisher-owner-1-bluesky-app-password")]
    [InlineData(KeyVaultSecretOwnerType.Publisher, "owner-1", KeyVaultSecretNames.Platform.Twitter, KeyVaultSecretNames.SettingName.ConsumerKey, "publisher-owner-1-twitter-consumer-key")]
    [InlineData(KeyVaultSecretOwnerType.Publisher, "owner-1", KeyVaultSecretNames.Platform.LinkedIn, KeyVaultSecretNames.SettingName.AccessToken, "publisher-owner-1-linkedin-access-token")]
    [InlineData(KeyVaultSecretOwnerType.Publisher, "owner-1", KeyVaultSecretNames.Platform.Facebook, KeyVaultSecretNames.SettingName.PageAccessToken, "publisher-owner-1-facebook-page-access-token")]
    [InlineData(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, "collector-owner-1-youtube-channel-api-key")]
    public void Build_WithCleanOwner_ReturnsExpectedFormat(
        KeyVaultSecretOwnerType ownerType, string ownerOid, string platform, string settingName, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build(ownerType, ownerOid, platform, settingName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("owner@with#special!", "publisher-owner-with-special--bluesky-app-password")]
    [InlineData("owner with spaces", "publisher-owner-with-spaces-bluesky-app-password")]
    [InlineData("owner_underscore", "publisher-owner-underscore-bluesky-app-password")]
    public void Build_WithSpecialCharsInOwner_SanitizesToHyphens(string ownerOid, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Publisher, ownerOid, KeyVaultSecretNames.Platform.Bluesky, KeyVaultSecretNames.SettingName.AppPassword);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("owner-1", "UCabc123", "collector-owner-1-youtube-channel-UCabc123-api-key")]
    [InlineData("owner@with#special!", "UCabc123", "collector-owner-with-special--youtube-channel-UCabc123-api-key")]
    public void Build_WithDiscriminator_InsertsDiscriminatorBetweenPlatformAndSettingName(
        string ownerOid, string discriminator, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, ownerOid, KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, discriminator);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("UC_my_channel", "collector-owner-1-youtube-channel-UC-my-channel-api-key")]
    [InlineData("channel_id_123", "collector-owner-1-youtube-channel-channel-id-123-api-key")]
    [InlineData("UC@special#chars!", "collector-owner-1-youtube-channel-UC-special-chars--api-key")]
    public void Build_WithSpecialCharsInDiscriminator_SanitizesToHyphens(string discriminator, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, discriminator);

        result.Should().Be(expected);
    }

    [Fact]
    public void Build_WithNullDiscriminator_OmitsDiscriminatorSegment()
    {
        var withDiscriminator = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, "channel-x");
        var withoutDiscriminator = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey);

        withDiscriminator.Should().Be("collector-owner-1-youtube-channel-channel-x-api-key");
        withoutDiscriminator.Should().Be("collector-owner-1-youtube-channel-api-key");
        withDiscriminator.Should().NotBe(withoutDiscriminator);
    }
}
