using System.Net.Mail;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace JosephGuadagno.Broadcasting.Functions.LinkedIn;

/// <summary>
/// Daily timer function that notifies users whose LinkedIn OAuth tokens are expiring soon.
/// Sends a 7-day warning and a 1-day warning, skipping users already notified today.
/// </summary>
public class NotifyExpiringTokens(
    IUserOAuthTokenManager userOAuthTokenManager,
    IApplicationUserDataStore applicationUserDataStore,
    IEmailTemplateManager emailTemplateManager,
    IEmailSender emailSender,
    ILogger<NotifyExpiringTokens> logger,
    IConfiguration configuration)
{
    private const string SevenDayTemplateName = "LinkedInTokenExpiring7Day";
    private const string OneDayTemplateName = "LinkedInTokenExpiring1Day";

    [Function(ConfigurationFunctionNames.LinkedInNotifyExpiringTokens)]
    public async Task RunAsync(
        [TimerTrigger("%linkedin_notify_expiring_tokens_cron_settings%")] TimerInfo myTimer,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            ConfigurationFunctionNames.LinkedInNotifyExpiringTokens, now);
        var webBaseUrl = GetWebBaseUrl();

        await NotifyWindowAsync(now, now.AddDays(7), SevenDayTemplateName, webBaseUrl, cancellationToken);
        await NotifyWindowAsync(now, now.AddDays(1), OneDayTemplateName, webBaseUrl, cancellationToken);

        logger.LogDebug("{FunctionName} completed at: {CompletedAt:f}",
            ConfigurationFunctionNames.LinkedInNotifyExpiringTokens, DateTimeOffset.UtcNow);
    }

    private async Task NotifyWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string templateName,
        string webBaseUrl,
        CancellationToken cancellationToken)
    {
        var expiringTokens = await userOAuthTokenManager.GetExpiringWindowAsync(from, to, cancellationToken);

        if (expiringTokens.Count == 0)
        {
            logger.LogDebug("{FunctionName}: No expiring tokens found for window [{From:O}, {To:O}] using template '{TemplateName}'.",
                ConfigurationFunctionNames.LinkedInNotifyExpiringTokens, from, to, templateName);
            return;
        }

        var template = await emailTemplateManager.GetTemplateAsync(templateName, cancellationToken);
        if (template is null)
        {
            logger.LogWarning("{FunctionName}: Email template '{TemplateName}' not found. Skipping notification pass.",
                ConfigurationFunctionNames.LinkedInNotifyExpiringTokens, templateName);
            return;
        }

        var todayUtc = from.UtcDateTime.Date;

        foreach (var token in expiringTokens)
        {
            if (token.LastNotifiedAt.HasValue &&
                token.LastNotifiedAt.Value.UtcDateTime.Date >= todayUtc)
            {
                logger.LogDebug("{FunctionName}: Skipping already-notified token for OID {OwnerOid}, platform {PlatformId} (last notified: {LastNotifiedAt:O}).",
                    ConfigurationFunctionNames.LinkedInNotifyExpiringTokens,
                    LogSanitizer.Sanitize(token.CreatedByEntraOid),
                    token.SocialMediaPlatformId,
                    token.LastNotifiedAt.Value);
                continue;
            }

            await TrySendNotificationAsync(token, template, webBaseUrl, cancellationToken);
        }
    }

    private async Task TrySendNotificationAsync(
        UserOAuthToken token,
        Domain.Models.EmailTemplate template,
        string webBaseUrl,
        CancellationToken cancellationToken)
    {
        var user = await applicationUserDataStore.GetByEntraObjectIdAsync(
            token.CreatedByEntraOid, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("{FunctionName}: Cannot send notification for OID {OwnerOid}, platform {PlatformId}: user not found or has no email address.",
                ConfigurationFunctionNames.LinkedInNotifyExpiringTokens,
                LogSanitizer.Sanitize(token.CreatedByEntraOid),
                token.SocialMediaPlatformId);
            return;
        }

        try
        {
            var reauthUrl = $"{webBaseUrl}/LinkedIn";

            var body = RenderTemplate(template.Body, user.DisplayName ?? user.Email,
                token.AccessTokenExpiresAt, reauthUrl);

            var toAddress = new MailAddress(user.Email, user.DisplayName ?? user.Email);
            await emailSender.QueueEmail(toAddress, template.Subject, body, cancellationToken);

            var notifiedAt = DateTimeOffset.UtcNow;
            await userOAuthTokenManager.UpdateLastNotifiedAtAsync(
                token.CreatedByEntraOid, token.SocialMediaPlatformId, notifiedAt, cancellationToken);

            logger.LogInformation("{FunctionName}: Queued expiry notification for OID {OwnerOid}, platform {PlatformId}. Token expires {ExpiresAt:O}.",
                ConfigurationFunctionNames.LinkedInNotifyExpiringTokens,
                LogSanitizer.Sanitize(token.CreatedByEntraOid),
                token.SocialMediaPlatformId,
                token.AccessTokenExpiresAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{FunctionName}: Failed to send notification for OID {OwnerOid}, platform {PlatformId}.",
                ConfigurationFunctionNames.LinkedInNotifyExpiringTokens,
                LogSanitizer.Sanitize(token.CreatedByEntraOid),
                token.SocialMediaPlatformId);
        }
    }

    private string GetWebBaseUrl()
    {
        var webBaseUrl = configuration["Settings:WebBaseUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(webBaseUrl))
        {
            logger.LogWarning(
                "{FunctionName}: Settings:WebBaseUrl is not configured. Re-auth links in LinkedIn expiry emails will be relative paths.",
                ConfigurationFunctionNames.LinkedInNotifyExpiringTokens);
            return string.Empty;
        }

        return webBaseUrl.TrimEnd('/');
    }

    private static string RenderTemplate(string templateBody, string displayName, DateTimeOffset expiresAt, string reauthUrl)
    {
        try
        {
            var scribanTemplate = Template.Parse(templateBody);
            var scriptObject = new ScriptObject();
            scriptObject.Import(new
            {
                display_name = displayName,
                expires_at = expiresAt.ToString("f"),
                reauth_url = reauthUrl
            });
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            return scribanTemplate.Render(context);
        }
        catch
        {
            return templateBody;
        }
    }
}
