using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Utilities;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class KeyVaultSecretNameBuilderTests
{
    [Theory]
    [InlineData("owner-1", "publisher", "bluesky", "app-password", "publisher-owner-1-bluesky-app-password")]
    [InlineData("owner-1", "publisher", "twitter", "consumer-key", "publisher-owner-1-twitter-consumer-key")]
    [InlineData("owner-1", "publisher", "linkedin", "access-token", "publisher-owner-1-linkedin-access-token")]
    [InlineData("owner-1", "publisher", "facebook", "page-access-token", "publisher-owner-1-facebook-page-access-token")]
    [InlineData("owner-1", "collector", "youtube-channel", "api-key", "collector-owner-1-youtube-channel-api-key")]
    public void Build_WithCleanOwner_ReturnsExpectedFormat(
        string ownerOid, string type, string platform, string settingName, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build(type, ownerOid, platform, settingName);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("owner@with#special!", "publisher-owner-with-special--bluesky-app-password")]
    [InlineData("owner with spaces", "publisher-owner-with-spaces-bluesky-app-password")]
    [InlineData("owner_underscore", "publisher-owner-underscore-bluesky-app-password")]
    public void Build_WithSpecialCharsInOwner_SanitizesToHyphens(string ownerOid, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build("publisher", ownerOid, "bluesky", "app-password");

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("owner-1", "UCabc123", "collector-owner-1-youtube-channel-UCabc123-api-key")]
    [InlineData("owner@with#special!", "UCabc123", "collector-owner-with-special--youtube-channel-UCabc123-api-key")]
    public void Build_WithDiscriminator_InsertsDiscriminatorBetweenPlatformAndSettingName(
        string ownerOid, string discriminator, string expected)
    {
        var result = KeyVaultSecretNameBuilder.Build("collector", ownerOid, "youtube-channel", "api-key", discriminator);

        result.Should().Be(expected);
    }

    [Fact]
    public void Build_WithNullDiscriminator_OmitsDiscriminatorSegment()
    {
        var withDiscriminator = KeyVaultSecretNameBuilder.Build("collector", "owner-1", "youtube-channel", "api-key", "channel-x");
        var withoutDiscriminator = KeyVaultSecretNameBuilder.Build("collector", "owner-1", "youtube-channel", "api-key");

        withDiscriminator.Should().Be("collector-owner-1-youtube-channel-channel-x-api-key");
        withoutDiscriminator.Should().Be("collector-owner-1-youtube-channel-api-key");
        withDiscriminator.Should().NotBe(withoutDiscriminator);
    }
}
