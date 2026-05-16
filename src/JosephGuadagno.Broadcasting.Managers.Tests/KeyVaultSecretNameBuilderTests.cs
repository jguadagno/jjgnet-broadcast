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

    // UCabc123 -> SHA-256 first 8 bytes -> c52b99988c271f2d
    // owner@with#special! sanitized -> owner-with-special--
    [Theory]
    [InlineData("owner-1", "UCabc123", "collector-owner-1-youtube-channel-c52b99988c271f2d-api-key")]
    [InlineData("owner@with#special!", "UCabc123", "collector-owner-with-special--youtube-channel-c52b99988c271f2d-api-key")]
    public void Build_WithDiscriminator_InsertsHashedDiscriminatorBetweenPlatformAndSettingName(
        string ownerOid, string discriminator, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, ownerOid, KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, discriminator);

        result.Should().Be(expected);
    }

    // UCabc_def -> d5ca878c9efddfbd
    // UCabc-def -> ab82aaa6dd869199
    // These two IDs differ only by _ vs - but must produce DIFFERENT secret names.
    [Fact]
    public void Build_WithUnderscoreDiscriminator_ProducesConsistentHash()
    {
        var withUnderscore = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, "UCabc_def");
        var withHyphen = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, "UCabc-def");

        // Determinism: same input always yields same output
        var withUnderscoreAgain = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, "UCabc_def");
        withUnderscore.Should().Be(withUnderscoreAgain);

        // No collision: _ and - must produce different names
        withUnderscore.Should().NotBe(withHyphen);

        // Exact expected values (SHA-256 first 8 bytes, lowercase hex)
        withUnderscore.Should().Be("collector-owner-1-youtube-channel-d5ca878c9efddfbd-api-key");
        withHyphen.Should().Be("collector-owner-1-youtube-channel-ab82aaa6dd869199-api-key");
    }

    [Fact]
    public void Build_WithDiscriminator_OutputContainsOnlyLowercaseHexInDiscriminatorSegment()
    {
        // UC_my_channel -> ac1bfb8e9bcdb6dc
        // channel_id_123 -> 2522164f7a1b526c
        // UC@special#chars! -> d5aae42bc3d8abeb
        var inputs = new[] { "UC_my_channel", "channel_id_123", "UC@special#chars!" };
        var expectedHashes = new[] { "ac1bfb8e9bcdb6dc", "2522164f7a1b526c", "d5aae42bc3d8abeb" };

        for (var i = 0; i < inputs.Length; i++)
        {
            var result = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, inputs[i]);
            result.Should().Contain(expectedHashes[i], because: $"'{inputs[i]}' must hash to '{expectedHashes[i]}'");
            result.Should().MatchRegex(@"^[a-z0-9-]+$", because: "all characters in the secret name must be lowercase letters, digits, or hyphens");
        }
    }

    [Fact]
    public void Build_WithNullDiscriminator_OmitsDiscriminatorSegment()
    {
        // channel-x -> SHA-256 first 8 bytes -> cf4e23e81ae3ddea
        var withDiscriminator = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey, "channel-x");
        var withoutDiscriminator = KeyVaultSecretNameBuilder.Build(KeyVaultSecretOwnerType.Collector, "owner-1", KeyVaultSecretNames.Platform.YouTubeChannel, KeyVaultSecretNames.SettingName.ApiKey);

        withDiscriminator.Should().Be("collector-owner-1-youtube-channel-cf4e23e81ae3ddea-api-key");
        withoutDiscriminator.Should().Be("collector-owner-1-youtube-channel-api-key");
        withDiscriminator.Should().NotBe(withoutDiscriminator);
    }
}
