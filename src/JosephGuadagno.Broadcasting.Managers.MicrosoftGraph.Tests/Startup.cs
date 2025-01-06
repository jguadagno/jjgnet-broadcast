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

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph.Tests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddEnvironmentVariables();
        });
    }

    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
        var config = hostBuilderContext.Configuration;
        services.AddSingleton(config);
        
        var azureKeyVaultUrl = config["AzureKeyVaultUrl"];
        if (string.IsNullOrWhiteSpace(azureKeyVaultUrl))
        {
            throw new Exception("Azure Key Vault URL is missing");
        }
        
        // Register the KeyVault client
        services.TryAddSingleton(s => new SecretClient(new Uri(azureKeyVaultUrl), new DefaultAzureCredential()));
        services.TryAddScoped<IKeyVault, KeyVault>();
        
        services.TryAddSingleton<IMicrosoftGraphManager, MicrosoftGraphManager>();
        services.AddHttpClient();
    }
}
