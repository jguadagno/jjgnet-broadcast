using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;
using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Constants = JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Functions.Maintenance;

public class AdClientSecretRefresh(
    IMicrosoftGraphManager microsoftGraphManager,
    TokenRefreshRepository tokenRefreshRepository,
    IKeyVault keyVault,
    ILogger<AdClientSecretRefresh> logger,
    TelemetryClient telemetryClient)
{

    [Function(Constants.ConfigurationFunctionNames.MaintenanceAdClientSecretRefresh)]
    public async Task Run([TimerTrigger("%maintenance_ad_secret_refresh_cron_settings%")] TimerInfo myTimer)
    {
        var startedAt = DateTime.UtcNow;
        logger.LogDebug("{FunctionName} started at: {StartedAt:f}",
            Constants.ConfigurationFunctionNames.MaintenanceAdClientSecretRefresh, startedAt);

        await RefreshToken(Managers.MicrosoftGraph.Constants.TokenTypes.WebClientSecret);
        await RefreshToken(Managers.MicrosoftGraph.Constants.TokenTypes.ApiClientSecret);
    }
    
    private async Task RefreshToken(Managers.MicrosoftGraph.Constants.TokenTypes tokenType)
    {
        const string azureKeyVaultSecretName = "jjg-net-{token-name}";
        
        // Check the Long Lived Token - Refresh it if needed
        var tokenInfo = await tokenRefreshRepository.GetAsync(Constants.Tables.TokenRefresh,
                                    tokenType.DisplayName()) ??
                                new TokenRefreshInfo(tokenType.DisplayName())
                                {
                                    LastRefreshed = DateTime.MinValue,
                                    LastChecked = DateTime.MinValue,
                                    Expires = DateTime.MinValue
                                };
        
        if (tokenInfo.Expires < DateTime.UtcNow || tokenInfo.Expires.AddDays(-5) < DateTime.UtcNow)
        {
            // Refresh the token
            logger.LogDebug("{DisplayName} is expired or will expire soon. Refreshing the token", tokenType.DisplayName());
            try
            {
                // TODO: Implement
                var newToken = new TokenInfo();
                //var newToken = await microsoftGraphManager.RefreshToken(tokenType.DisplayName());
                
                // Save the token to Key Vault
                var secretName = azureKeyVaultSecretName.Replace("{token-name}", tokenType.DisplayName());
                
                await keyVault.UpdateSecretValueAndPropertiesAsync(secretName, newToken.AccessToken, newToken.ExpiresOn);
                
                // Save the token refresh info to the database
                tokenInfo.LastRefreshed = tokenInfo.LastChecked = DateTime.UtcNow;
                tokenInfo.Expires = newToken.ExpiresOn;
                await tokenRefreshRepository.SaveAsync(tokenInfo);
                
                // Update logs and telemetry
                logger.LogInformation("{DisplayName} refreshed successfully. Expires on {Expires:f}",
                    tokenType.DisplayName(), tokenInfo.Expires);
                var eventName = "{DisplayName}TokenRefreshed".Replace("{DisplayName}", tokenType.DisplayName());
                telemetryClient.TrackEvent(eventName, new Dictionary<string, string>
                {
                    {"Expires", tokenInfo.Expires.ToString("O")},
                    {"TokenType", tokenType.DisplayName()}
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing the {DisplayName} Token", tokenType.DisplayName());
            }
        }
        else
        {
            logger.LogDebug("{DisplayName} is still valid until {Expires:f}", tokenType.DisplayName(), tokenInfo.Expires);
        }
    }
}