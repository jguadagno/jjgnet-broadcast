using System.Reflection;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Facebook.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Tests;

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
        
        var facebookApplicationSettings = new FacebookApplicationSettings();
        config.Bind("Facebook", facebookApplicationSettings);
        services.TryAddSingleton<IFacebookApplicationSettings>(facebookApplicationSettings);
        
        services.TryAddSingleton<IFacebookManager, FacebookManager>();

        services.AddHttpClient();
    }
}