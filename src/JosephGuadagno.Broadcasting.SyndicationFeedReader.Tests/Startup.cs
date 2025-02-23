using System.Reflection;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests;

public class Startup
{

    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder //.SetBasePath(hostBuilder.HostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.development.json", false)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddEnvironmentVariables();
        });
    }
        
    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
        var config = hostBuilderContext.Configuration;
            
        services.AddSingleton(config);

        // Bind the 'Settings' section to the ISettings class
        var settings = new Domain.Models.Settings();
        config.Bind("Settings", settings);
        services.TryAddSingleton<ISettings>(settings);

        var randomPostSettings = new RandomPostSettings();
        config.Bind("Settings:RandomPost", randomPostSettings);
        services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);
            
        // Configure the logger
        services.AddLogging((loggingBuilder =>
        {
            //loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddConfiguration(config);
        }));

        // Configure all the services
        ConfigureSyndicationFeedReader(services);
        ConfigureFunction(services);
    }
    
    private void ConfigureSyndicationFeedReader(IServiceCollection services)
    {
        services.TryAddSingleton<ISyndicationFeedReaderSettings>(s =>
        {
            var settings = new SyndicationFeedReaderSettings();
            var configuration = s.GetService<IConfiguration>();
            configuration.Bind("Settings:SyndicationFeedReader", settings);
            return settings;
        });
        services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader>();
    }

    private void ConfigureFunction(IServiceCollection services)
    {
        services.AddHttpClient();
    }
}