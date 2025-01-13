using System;
using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Tests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", false)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), false)
                .AddEnvironmentVariables();
        });
    }

    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
        var config = hostBuilderContext.Configuration;
        services.AddSingleton(config);

        ConfigureKeyVault(services, config);
        services.TryAddSingleton<IMicrosoftGraphManager, MicrosoftGraphManager>();
        services.AddHttpClient();
    }

    void ConfigureKeyVault(IServiceCollection services, IConfiguration config)
    {
        var keyVaultSettings = new KeyVaultSettings
        {
            KeyVaultUri = null,
            TenantId = null,
            ClientId = null,
            ClientSecret = null
        };
        config.Bind("Settings:KeyVault", keyVaultSettings);
            
        services.TryAddSingleton(s => new SecretClient(new Uri(keyVaultSettings.KeyVaultUri),
            new ChainedTokenCredential(new ManagedIdentityCredential(),
                new ClientSecretCredential(keyVaultSettings.TenantId, keyVaultSettings.ClientId,
                    keyVaultSettings.ClientSecret))));
    
        services.TryAddScoped<IKeyVault, KeyVault>();
    }
}
