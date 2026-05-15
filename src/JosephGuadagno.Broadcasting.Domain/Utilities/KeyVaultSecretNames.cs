namespace JosephGuadagno.Broadcasting.Domain.Utilities;

/// <summary>
/// Well-known Key Vault secret name segments for platforms and settings.
/// Use these constants in place of inline string literals when calling
/// <see cref="KeyVaultSecretNameBuilder.Build"/>.
/// </summary>
public static class KeyVaultSecretNames
{
    /// <summary>Platform segment constants.</summary>
    public static class Platform
    {
        /// <summary>Bluesky social platform.</summary>
        public const string Bluesky = "bluesky";

        /// <summary>Twitter / X social platform.</summary>
        public const string Twitter = "twitter";

        /// <summary>LinkedIn social platform.</summary>
        public const string LinkedIn = "linkedin";

        /// <summary>Facebook social platform.</summary>
        public const string Facebook = "facebook";

        /// <summary>YouTube Channel collector.</summary>
        public const string YouTubeChannel = "youtube-channel";
    }

    /// <summary>Setting-name segment constants.</summary>
    public static class SettingName
    {
        /// <summary>Bluesky app password.</summary>
        public const string AppPassword = "app-password";

        /// <summary>Twitter OAuth 1.0a consumer key.</summary>
        public const string ConsumerKey = "consumer-key";

        /// <summary>Twitter OAuth 1.0a consumer secret.</summary>
        public const string ConsumerSecret = "consumer-secret";

        /// <summary>OAuth access token.</summary>
        public const string AccessToken = "access-token";

        /// <summary>Twitter OAuth 1.0a access token secret.</summary>
        public const string AccessTokenSecret = "access-token-secret";

        /// <summary>OAuth client secret.</summary>
        public const string ClientSecret = "client-secret";

        /// <summary>Facebook page access token.</summary>
        public const string PageAccessToken = "page-access-token";

        /// <summary>Facebook app secret.</summary>
        public const string AppSecret = "app-secret";

        /// <summary>Facebook client token.</summary>
        public const string ClientToken = "client-token";

        /// <summary>Facebook short-lived user access token.</summary>
        public const string ShortLivedAccessToken = "short-lived-access-token";

        /// <summary>Facebook long-lived user access token.</summary>
        public const string LongLivedAccessToken = "long-lived-access-token";

        /// <summary>YouTube Data API key.</summary>
        public const string ApiKey = "api-key";
    }
}
