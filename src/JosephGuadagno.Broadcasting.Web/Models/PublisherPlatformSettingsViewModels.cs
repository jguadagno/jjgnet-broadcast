using System.ComponentModel.DataAnnotations;

namespace JosephGuadagno.Broadcasting.Web.Models;

public abstract class PublisherPlatformSettingsViewModel : IValidatableObject
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public int SocialMediaPlatformId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string? PlatformIcon { get; set; }
    public bool IsEnabled { get; set; }
    public bool ChangeCredentials { get; set; }
    public bool IsManagedBySiteAdmin { get; set; }
    public string? CredentialSetupDocumentationUrl { get; set; }

    public string MaskedValue => "••••••••";

    public virtual string DisplayName => PlatformName;

    public virtual bool IsConfigured => true;

    public abstract string PartialViewName { get; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return [];
    }
}

public sealed class BlueskyPublisherSettingsViewModel : PublisherPlatformSettingsViewModel
{
    [Display(Name = "Bluesky handle")]
    public string? UserName { get; set; }

    public bool HasAppPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "App password")]
    public string? AppPassword { get; set; }

    public override string PartialViewName => "_BlueskySettings";

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEnabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(UserName))
        {
            yield return new ValidationResult("Bluesky handle is required when Bluesky publishing is enabled.", [nameof(UserName)]);
        }

        if ((ChangeCredentials || !HasAppPassword) && string.IsNullOrWhiteSpace(AppPassword))
        {
            yield return new ValidationResult("App password is required when enabling Bluesky publishing or changing credentials.", [nameof(AppPassword)]);
        }
    }
}

public sealed class TwitterPublisherSettingsViewModel : PublisherPlatformSettingsViewModel
{
    public bool HasConsumerKey { get; set; }
    public bool HasConsumerSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public bool HasAccessTokenSecret { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Consumer key")]
    public string? ConsumerKey { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Consumer secret")]
    public string? ConsumerSecret { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Access token")]
    public string? AccessToken { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Access token secret")]
    public string? AccessTokenSecret { get; set; }

    public override string PartialViewName => "_TwitterSettings";
    public override string DisplayName => $"{PlatformName} / X";

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEnabled)
        {
            yield break;
        }

        if ((ChangeCredentials || !HasConsumerKey) && string.IsNullOrWhiteSpace(ConsumerKey))
        {
            yield return new ValidationResult("Consumer key is required when enabling Twitter/X publishing or changing credentials.", [nameof(ConsumerKey)]);
        }

        if ((ChangeCredentials || !HasConsumerSecret) && string.IsNullOrWhiteSpace(ConsumerSecret))
        {
            yield return new ValidationResult("Consumer secret is required when enabling Twitter/X publishing or changing credentials.", [nameof(ConsumerSecret)]);
        }

        if ((ChangeCredentials || !HasAccessToken) && string.IsNullOrWhiteSpace(AccessToken))
        {
            yield return new ValidationResult("Access token is required when enabling Twitter/X publishing or changing credentials.", [nameof(AccessToken)]);
        }

        if ((ChangeCredentials || !HasAccessTokenSecret) && string.IsNullOrWhiteSpace(AccessTokenSecret))
        {
            yield return new ValidationResult("Access token secret is required when enabling Twitter/X publishing or changing credentials.", [nameof(AccessTokenSecret)]);
        }
    }
}

public sealed class FacebookPublisherSettingsViewModel : PublisherPlatformSettingsViewModel
{
    [Display(Name = "Page ID")]
    public string? PageId { get; set; }

    [Display(Name = "App ID")]
    public string? AppId { get; set; }

    public string? GraphApiVersion { get; set; }

    public string? GraphApiRootUrl { get; set; }

    public bool HasPageAccessToken { get; set; }
    public bool HasAppSecret { get; set; }
    public bool HasClientToken { get; set; }
    public bool HasShortLivedAccessToken { get; set; }
    public bool HasLongLivedAccessToken { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Page access token")]
    public string? PageAccessToken { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "App secret")]
    public string? AppSecret { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Client token")]
    public string? ClientToken { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Short-lived access token")]
    public string? ShortLivedAccessToken { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Long-lived access token")]
    public string? LongLivedAccessToken { get; set; }

    public override string PartialViewName => "_FacebookSettings";

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEnabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(PageId))
        {
            yield return new ValidationResult("Page ID is required when Facebook publishing is enabled.", [nameof(PageId)]);
        }

        if (string.IsNullOrWhiteSpace(AppId))
        {
            yield return new ValidationResult("App ID is required when Facebook publishing is enabled.", [nameof(AppId)]);
        }

        if ((ChangeCredentials || !HasPageAccessToken) && string.IsNullOrWhiteSpace(PageAccessToken))
        {
            yield return new ValidationResult("Page access token is required when enabling Facebook publishing or changing credentials.", [nameof(PageAccessToken)]);
        }

        if ((ChangeCredentials || !HasAppSecret) && string.IsNullOrWhiteSpace(AppSecret))
        {
            yield return new ValidationResult("App secret is required when enabling Facebook publishing or changing credentials.", [nameof(AppSecret)]);
        }

        if ((ChangeCredentials || !HasClientToken) && string.IsNullOrWhiteSpace(ClientToken))
        {
            yield return new ValidationResult("Client token is required when enabling Facebook publishing or changing credentials.", [nameof(ClientToken)]);
        }

        if ((ChangeCredentials || !HasShortLivedAccessToken) && string.IsNullOrWhiteSpace(ShortLivedAccessToken))
        {
            yield return new ValidationResult("Short-lived access token is required when enabling Facebook publishing or changing credentials.", [nameof(ShortLivedAccessToken)]);
        }

        if ((ChangeCredentials || !HasLongLivedAccessToken) && string.IsNullOrWhiteSpace(LongLivedAccessToken))
        {
            yield return new ValidationResult("Long-lived access token is required when enabling Facebook publishing or changing credentials.", [nameof(LongLivedAccessToken)]);
        }
    }
}

public sealed class LinkedInPublisherSettingsViewModel : PublisherPlatformSettingsViewModel
{
    [Display(Name = "Author ID")]
    public string? AuthorId { get; set; }

    [Display(Name = "Client ID")]
    public string? ClientId { get; set; }

    public string? AccessTokenUrl { get; set; }

    public bool HasClientSecret { get; set; }
    public bool HasAccessToken { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Client secret")]
    public string? ClientSecret { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Access token")]
    public string? AccessToken { get; set; }

    public override string PartialViewName => "_LinkedInSettings";

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEnabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(AuthorId))
        {
            yield return new ValidationResult("Author ID is required when LinkedIn publishing is enabled.", [nameof(AuthorId)]);
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            yield return new ValidationResult("Client ID is required when LinkedIn publishing is enabled.", [nameof(ClientId)]);
        }

        if ((ChangeCredentials || !HasClientSecret) && string.IsNullOrWhiteSpace(ClientSecret))
        {
            yield return new ValidationResult("Client secret is required when enabling LinkedIn publishing or changing credentials.", [nameof(ClientSecret)]);
        }

        if ((ChangeCredentials || !HasAccessToken) && string.IsNullOrWhiteSpace(AccessToken))
        {
            yield return new ValidationResult("Access token is required when enabling LinkedIn publishing or changing credentials.", [nameof(AccessToken)]);
        }
    }
}

public sealed class UnsupportedPublisherSettingsViewModel : PublisherPlatformSettingsViewModel
{
    public override string PartialViewName => "_UnsupportedPublisherSettings";
    public override bool IsConfigured => false;
}
