using System;
using System.Net.Http;
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
using EngagementRepository = JosephGuadagno.Broadcasting.Data.Sql.EngagementDataStore;

namespace JosephGuadagno.Broadcasting.SyndicationFeedReader.Tests;

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
        
        var randomPostSettings = new RandomPostSettings();
        config.Bind("Settings:RandomPost", randomPostSettings);
        services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);
        
        ConfigureSyndicationFeedReader(services);
    
        // Configure the logger
        services.AddLogging((loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddConfiguration(config);
        }));
    }
    
    private void ConfigureSyndicationFeedReader(IServiceCollection services)
    {
        services.TryAddSingleton<ISyndicationFeedReaderSettings>(s =>
        {
            var settings = new SyndicationFeedReaderSettings();
            var configuration = s.GetService<IConfiguration>();
            if (configuration is null)
            {
                throw new ApplicationException("Failed to get the configuration from the ServiceCollection");
            }
            configuration.Bind("Settings:SyndicationFeedReader", settings);
            return settings;
        });
        services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader>();
    }
}