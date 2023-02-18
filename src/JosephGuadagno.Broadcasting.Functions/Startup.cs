using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.Functions;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Exceptions;
using EngagementRepository = JosephGuadagno.Broadcasting.Data.Sql.EngagementDataStore;

[assembly: FunctionsStartup(typeof(Startup))]

namespace JosephGuadagno.Broadcasting.Functions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
            
        var executionContextOptions = builder.Services.BuildServiceProvider()
            .GetService<IOptions<ExecutionContextOptions>>();
        if (executionContextOptions is null)
        {
            throw new ApplicationException("Could not get the ExecutionContextOptions from the Builder");
        }
        var currentDirectory = executionContextOptions.Value.AppDirectory;
            
        // Setup the Configuration Source
        var config = new ConfigurationBuilder()
            .SetBasePath(currentDirectory)
            .AddJsonFile("local.settings.json", true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .AddEnvironmentVariables()
            .Build();
        builder.Services.AddSingleton<IConfiguration>(config);

        // Bind the 'Settings' section to the ISettings class
        var settings = new Domain.Models.Settings();
        config.Bind("Settings", settings);
        builder.Services.TryAddSingleton<ISettings>(settings);
        
        builder.Services.TryAddSingleton<IDatabaseSettings>(new DatabaseSettings
            { JJGNetDatabaseSqlServer = settings.JJGNetDatabaseSqlServer });

        var randomPostSettings = new Domain.Models.RandomPostSettings();
        config.Bind("Settings:RandomPost", randomPostSettings);
        builder.Services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);
        
        // Configure the logger
        string logPath = Path.Combine(currentDirectory, "logs\\logs.txt");
        ConfigureLogging(builder.Services, settings, logPath, "Functions");
        
        // Configure all the services
        ConfigureTwitter(builder);
        ConfigureJsonFeedReader(builder);
        ConfigureSyndicationFeedReader(builder);
        ConfigureYouTubeReader(builder);
        ConfigureFunction(builder);
    }

    private void ConfigureLogging(IServiceCollection services, ISettings settings, string logPath, string applicationName)
    {
        var logger = new LoggerConfiguration()
            #if DEBUG
            .MinimumLevel.Debug()
            #else
            .MinimumLevel.Warning()
            #endif
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithAssemblyName()
            .Enrich.WithAssemblyVersion(true)
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("Application", applicationName)
            .Destructure.ToMaximumDepth(4)
            .Destructure.ToMaximumStringLength(100)
            .Destructure.ToMaximumCollectionCount(10)
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .WriteTo.AzureTableStorage(settings.StorageAccount, storageTableName: "Logging",
                keyGenerator: new SerilogKeyGenerator())
            .CreateLogger();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddApplicationInsights(settings.AppInsightsKey);
            loggingBuilder.AddSerilog(logger);
        });
    }

    private void ConfigureTwitter(IFunctionsHostBuilder builder)
    {
        builder.Services.TryAddSingleton<IAuthorizer>(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get settings from ServiceCollection");
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
        builder.Services.TryAddSingleton(s =>
        {
            var authorizer = s.GetService<IAuthorizer>();
            if (authorizer is null)
            {
                throw new ApplicationException("Failed to get authorizer from ServiceCollection");
            }
            return new TwitterContext(authorizer);
        });
        builder.Services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get settings from ServiceCollection");
            }
            return new SourceDataRepository(settings.StorageAccount);
        });
    }

    private void ConfigureJsonFeedReader(IFunctionsHostBuilder builder)
    {
        builder.Services.TryAddSingleton<IJsonFeedReaderSettings>(s =>
        {
            var settings = new JsonFeedReaderSettings();
            var configuration = s.GetService<IConfiguration>();
            configuration.Bind("Settings:JsonFeedReader", settings);
            return settings;
        });
        builder.Services.TryAddSingleton<IJsonFeedReader, JsonFeedReader.JsonFeedReader>();
    }

    private void ConfigureSyndicationFeedReader(IFunctionsHostBuilder builder)
    {
        builder.Services.TryAddSingleton<ISyndicationFeedReaderSettings>(s =>
        {
            var settings = new SyndicationFeedReaderSettings();
            var configuration = s.GetService<IConfiguration>();
            configuration.Bind("Settings:SyndicationFeedReader", settings);
            return settings;
        });
        builder.Services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader.SyndicationFeedReader>();

    }

    private void ConfigureYouTubeReader(IFunctionsHostBuilder builder)
    {
        builder.Services.TryAddSingleton<IYouTubeSettings>(s =>
        {
            var settings = new YouTubeSettings();
            var configuration = s.GetService<IConfiguration>();
            configuration.Bind("Settings:YouTube", settings);
            return settings;
        });
        builder.Services.TryAddSingleton<IYouTubeReader, YouTubeReader.YouTubeReader>();
    }
        
    private void ConfigureRepositories(IServiceCollection services)
    {
        services.AddDbContext<BroadcastingContext>(ServiceLifetime.Scoped);
            
        // Engagements
        services.TryAddSingleton<IEngagementDataStore>(s =>
        {
            var databaseSettings = s.GetService<IDatabaseSettings>();
            if (databaseSettings is null)
            {
                throw new ApplicationException("Failed to get a IDatabaseSettings object from ServiceCollection when registering IEngagementDataStore");
            }
            return new EngagementDataStore(databaseSettings);
        });
        services.TryAddSingleton<IEngagementRepository>(s =>
        {
            var engagementDataStore = s.GetService<IEngagementDataStore>();
            if (engagementDataStore is null)
            {
                throw new ApplicationException("Failed to get an EngagementDataStore from ServiceCollection");
            }
            return new Data.Repositories.EngagementRepository(engagementDataStore);
        });
        services.TryAddSingleton<IEngagementManager, EngagementManager>();

        // ScheduledItem
        services.TryAddSingleton<IScheduledItemDataStore>(s =>
        {
            var databaseSettings = s.GetService<IDatabaseSettings>();
            if (databaseSettings is null)
            {
                throw new ApplicationException("Failed to get a IDatabaseSettings object from ServiceCollection when registering IScheduledItemDataStore");
            }
            return new ScheduledItemDataStore(databaseSettings);
        });
        services.TryAddSingleton<IScheduledItemRepository>(s =>
        {
            var scheduledItemDataStore = s.GetService<IScheduledItemDataStore>();
            if (scheduledItemDataStore is null)
            {
                throw new ApplicationException("Failed to get a ScheduledItemDataStore object from ServiceCollection");
            }
            return new ScheduledItemRepository(scheduledItemDataStore);
        });
        services.TryAddSingleton<IScheduledItemManager, ScheduledItemManager>();
    }

    private void ConfigureFunction(IFunctionsHostBuilder builder)
    {
        builder.Services.AddHttpClient();
            
        builder.Services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get settings from ServiceCollection");
            }
            return new ConfigurationRepository(settings.StorageAccount);
        });
        builder.Services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get settings from ServiceCollection");
            }
            return new SourceDataRepository(settings.StorageAccount);
        });
        builder.Services.TryAddSingleton(s =>
        {
            var settings = s.GetService<ISettings>();
            if (settings is null)
            {
                throw new ApplicationException("Failed to get settings from ServiceCollection");
            }
            var httpClient = s.GetService(typeof(HttpClient)) as HttpClient;
                
            return new Utilities.Web.Shortener.Bitly(httpClient,
                new BitlyConfiguration
                {
                    ApiRootUri = settings.BitlyAPIRootUri,
                    Token = settings.BitlyToken
                });
        });
        builder.Services.TryAddSingleton<IUrlShortener, UrlShortener>();
        builder.Services.TryAddSingleton<IEventPublisher, EventPublisher>();
            
        ConfigureRepositories(builder.Services);
    }
}