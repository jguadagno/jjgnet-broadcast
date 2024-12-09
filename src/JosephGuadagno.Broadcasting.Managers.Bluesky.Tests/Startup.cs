namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Tests;

using System.Reflection;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder //.SetBasePath(hostBuilder.HostingEnvironment.ContentRootPath)
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
        
        var blueskySettings = new BlueskySettings();
        config.Bind("Settings:Bluesky", blueskySettings);
        services.TryAddSingleton<IBlueskySettings>(blueskySettings);
        services.TryAddSingleton<IBlueskyManager, BlueskyManager>();

        services.AddHttpClient();
    }
}