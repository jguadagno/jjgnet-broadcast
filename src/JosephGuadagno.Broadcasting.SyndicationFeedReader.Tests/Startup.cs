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
            configurationBuilder //.SetBasePath(hostBuilder.HostingEnvironment.ContentRootPath)
                .AddJsonFile("local.settings.json", true)
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
        services.TryAddSingleton<IDatabaseSettings>(new DatabaseSettings
            { JJGNetDatabaseSqlServer = settings.JJGNetDatabaseSqlServer });

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
        ConfigureTwitter(services);
        ConfigureJsonFeedReader(services);
        ConfigureSyndicationFeedReader(services);
        ConfigureYouTubeReader(services);
        ConfigureFunction(services);
    }

    private void ConfigureTwitter(IServiceCollection services)
    {
        services.TryAddSingleton<IAuthorizer>(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get the settings from the ServiceCollection");
            }
            return new SingleUserAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore
                {
                    ConsumerKey = settings.TwitterApiKey,
                    ConsumerSecret = settings.TwitterApiSecret,
                    OAuthToken = settings.TwitterAccessToken,
                    OAuthTokenSecret = settings.TwitterAccessTokenSecret
                }
            };
        });
        services.TryAddSingleton(s =>
        {
            var authorizer = s.GetService<IAuthorizer>();
            if (authorizer is null)
            {
                throw new ApplicationException("Failed to get the authorizer from the ServiceCollection");
            }
            return new TwitterContext(authorizer);
        });
        services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get the settings from the ServiceCollection");
            }
            return new SourceDataRepository(settings.StorageAccount);
        });
    }

    private void ConfigureJsonFeedReader(IServiceCollection services)
    {
        services.TryAddSingleton<IJsonFeedReaderSettings>(s =>
        {
            var settings = new JsonFeedReaderSettings();
            var configuration = s.GetService<IConfiguration>();
            configuration.Bind("Settings:JsonFeedReader", settings);
            return settings;
        });
        services.TryAddSingleton<IJsonFeedReader, JsonFeedReader.JsonFeedReader>();
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

    private void ConfigureYouTubeReader(IServiceCollection services)
    {
        services.TryAddSingleton<IYouTubeSettings>(s =>
        {
            var settings = new YouTubeSettings();
            var configuration = s.GetService<IConfiguration>();
            configuration.Bind("Settings:YouTube", settings);
            return settings;
        });
        services.TryAddSingleton<IYouTubeReader, YouTubeReader.YouTubeReader>();
    }

    private void ConfigureRepositories(IServiceCollection services)
    {
        services.TryAddSingleton(s =>
        {
            var databaseSettings = s.GetService<IDatabaseSettings>();
            if (databaseSettings is null)
            {
                throw new ApplicationException("Failed to get a settings object from ServiceCollection");
            }
            return new BroadcastingContext(databaseSettings);
        });
            
        // Engagements
        services.TryAddSingleton(s =>
        {
            var databaseSettings = s.GetService<IDatabaseSettings>();
            if (databaseSettings is null)
            {
                throw new ApplicationException("Failed to get a settings object from ServiceCollection");
            }
            return new EngagementDataStore(databaseSettings);
        });
        services.TryAddSingleton(s =>
        {
            var engagementDataStore = s.GetService<EngagementRepository>();
            if (engagementDataStore is null)
            {
                throw new ApplicationException("Failed to get a Engagement Repository object from ServiceCollection");
            }
            return new Data.Repositories.EngagementRepository(engagementDataStore);
        });
        services.TryAddSingleton<IEngagementManager, EngagementManager>();

        // ScheduledItem
        services.TryAddSingleton(s =>
        {
            var databaseSettings = s.GetService<IDatabaseSettings>();
            if (databaseSettings is null)
            {
                throw new ApplicationException("Failed to get a settings object from ServiceCollection");
            }
            return new ScheduledItemDataStore(databaseSettings);
        });
        services.TryAddSingleton(s =>
        {
            var scheduledItemDataStore = s.GetService<ScheduledItemDataStore>();
            if (scheduledItemDataStore is null)
            {
                throw new ApplicationException("Failed to get a Scheduled Item Data Store object from ServiceCollection");
            }
            return new ScheduledItemRepository(scheduledItemDataStore);
        });
        services.TryAddSingleton<IScheduledItemManager, ScheduledItemManager>();
    }

    private void ConfigureFunction(IServiceCollection services)
    {
        services.AddHttpClient();
            
        services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get the settings from the ServiceCollection");
            }
            return new ConfigurationRepository(settings.StorageAccount);
        });
        services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get the settings from the ServiceCollection");
            }
            return new TokenRefreshRepository(settings.StorageAccount);
        });
        services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get the settings from the ServiceCollection");
            }
            return new SourceDataRepository(settings.StorageAccount);
        });
            
        services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get the settings from the ServiceCollection");
            }
            var httpClient = s.GetService(typeof(HttpClient)) as HttpClient;
                
            return new Utilities.Web.Shortener.Bitly(httpClient,
                new BitlyConfiguration
                {
                    ApiRootUri = settings.BitlyAPIRootUri,
                    Token = settings.BitlyToken
                });
        });
        services.TryAddSingleton<IUrlShortener, UrlShortener>();
        services.TryAddSingleton<IEventPublisher, EventPublisher>();
            
        ConfigureRepositories(services);
    }
}