using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Managers.Bluesky;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using JosephGuadagno.Broadcasting.Managers.Facebook;
using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Facebook.Models;
using JosephGuadagno.Broadcasting.Managers.LinkedIn;
using JosephGuadagno.Broadcasting.Managers.LinkedIn.Models;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

using Serilog;
using Serilog.Exceptions;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class Startup
{
    private readonly string _currentDirectory = Directory.GetCurrentDirectory();
    
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder
                //.SetBasePath(_currentDirectory)
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

        var settings =
            new JosephGuadagno.Broadcasting.Functions.Models.Settings
            {
                LoggingStorageAccount = null!, ShortenedDomainToUse = null!
            };
        config.Bind("Settings", settings);
        services.TryAddSingleton<ISettings>(settings);

        var randomPostSettings = new RandomPostSettings { ExcludedCategories = [] };
        config.Bind("RandomPost", randomPostSettings);
        services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);

        var speakerEngagementsSettings = new SpeakingEngagementsReaderSettings { SpeakingEngagementsFile = null };
        config.Bind("SpeakingEngagementsReader", speakerEngagementsSettings);
        services.TryAddSingleton<ISpeakingEngagementsReaderSettings>(speakerEngagementsSettings);

        var eventPublisherSettings = new EventPublisherSettings { TopicEndpointSettings = [] };
        var endpoints = config.GetSection("EventGridTopics:TopicEndpointSettings").Get<List<TopicEndpointSettings>>();
        if (endpoints != null)
        {
            foreach (var endpoint in endpoints)
            {
                eventPublisherSettings.TopicEndpointSettings.Add(endpoint);
            }
        }
        services.TryAddSingleton<IEventPublisherSettings>(eventPublisherSettings);

        // Configure the logger
        string loggerFile = Path.Combine(_currentDirectory, $"logs{Path.DirectorySeparatorChar}logs.txt");
        ConfigureLogging(config, services, settings, loggerFile, "Functions");

        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

        // Add in AutoMapper
        var autoMapperSettings = new AutoMapperSettings();
        config.Bind("AutoMapper", autoMapperSettings);
        services.AddAutoMapper(mapperConfig =>
        {
            mapperConfig.LicenseKey = autoMapperSettings.LicenseKey;
            mapperConfig.AddProfile<Data.Sql.MappingProfiles.BroadcastingProfile>();
        }, typeof(Program));

        // Configure all the services
        ConfigureKeyVault(services, config);
        ConfigureFunction(services);
        ConfigureBitly(services, config);
        ConfigureTwitter(services, config);
        ConfigureSyndicationFeedReader(services, config);
        ConfigureYouTubeReader(services, config);
        ConfigureLinkedInManager(services, config);
        ConfigureFacebookManager(services, config);
        ConfigureBlueskyManager(services, config);

    }

    private void ConfigureLogging(IConfiguration configurationRoot, IServiceCollection services, ISettings appSettings,
        string logPath, string applicationName)
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
            .WriteTo.AzureTableStorage(appSettings.LoggingStorageAccount, storageTableName: "Logging",
                keyGenerator: new SerilogKeyGenerator())
            .CreateLogger();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                    config.ConnectionString =
                        configurationRoot["APPLICATIONINSIGHTS_CONNECTION_STRING"],
                configureApplicationInsightsLoggerOptions: (_) => { });
            loggingBuilder.AddSerilog(logger);
        });
    }

    private void ConfigureKeyVault(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddSecretClient(configuration.GetSection("KeyVault"));
        });
        services.TryAddScoped<IKeyVault, KeyVault>();
    }

    private void ConfigureFunction(IServiceCollection services)
    {
        services.AddHttpClient();

        services.TryAddSingleton<IEventPublisher, EventPublisher>();

        services.AddDbContext<BroadcastingContext>(options => options.UseSqlServer("name=ConnectionStrings:JJGNetDatabaseSqlServer"));

        services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
        services.TryAddScoped<IEngagementRepository, EngagementRepository>();
        services.TryAddScoped<IEngagementManager, EngagementManager>();

        services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
        services.TryAddScoped<IScheduledItemRepository, ScheduledItemRepository>();
        services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();

        services.AddSingleton<IYouTubeSourceDataStore, YouTubeSourceDataStore>();
        services.AddSingleton<IYouTubeSourceRepository, YouTubeSourceRepository>();
        services.AddSingleton<IYouTubeSourceManager, YouTubeSourceManager>();

        services.AddSingleton<ISyndicationFeedSourceDataStore, SyndicationFeedSourceDataStore>();
        services.AddSingleton<ISyndicationFeedSourceRepository, SyndicationFeedSourceRepository>();
        services.AddSingleton<ISyndicationFeedSourceManager, SyndicationFeedSourceManager>();

        services.AddSingleton<IFeedCheckDataStore, FeedCheckDataStore>();
        services.AddSingleton<IFeedCheckRepository, FeedCheckRepository>();
        services.AddSingleton<IFeedCheckManager, FeedCheckManager>();

        services.AddSingleton<ITokenRefreshDataStore, TokenRefreshDataStore>();
        services.AddSingleton<ITokenRefreshRepository, TokenRefreshRepository>();
        services.AddSingleton<ITokenRefreshManager, TokenRefreshManager>();

        services.AddScoped<ISpeakingEngagementsReader, SpeakingEngagementsReader.SpeakingEngagementsReader>();
    }

    void ConfigureBitly(IServiceCollection services, IConfiguration config)
    {
        var bitlySettings = new BitlyConfiguration();
        config.Bind("Bitly", bitlySettings);
        services.TryAddSingleton<IBitlyConfiguration>(bitlySettings);
        services.TryAddSingleton(s =>
        {
            var httpClient = s.GetService(typeof(HttpClient)) as HttpClient;
            return new Utilities.Web.Shortener.Bitly(httpClient, bitlySettings);
        });
        services.TryAddSingleton<IUrlShortener, UrlShortener>();
    }

    void ConfigureTwitter(IServiceCollection services, IConfiguration config)
    {
        var twitterSettings = new InMemoryCredentialStore();
        config.Bind("Twitter", twitterSettings);
        services.TryAddSingleton<InMemoryCredentialStore>(twitterSettings);

        services.TryAddSingleton<IAuthorizer>(s =>
        {
            var credentialStore = s.GetService<InMemoryCredentialStore>();
            if (credentialStore is null)
            {
                throw new ApplicationException("Failed to get credential store from ServiceCollection");
            }

            return new SingleUserAuthorizer { CredentialStore = credentialStore };
        });
        services.TryAddSingleton(s =>
        {
            var authorizer = s.GetService<IAuthorizer>();
            if (authorizer is null)
            {
                throw new ApplicationException("Failed to get authorizer from ServiceCollection");
            }
            return new TwitterContext(authorizer);
        });
    }

    private void ConfigureSyndicationFeedReader(IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton<ISyndicationFeedReaderSettings>(_ =>
        {
            var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings();
            config.Bind("SyndicationFeedReader", syndicationFeedReaderSettings);
            return syndicationFeedReaderSettings;
        });
        services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader.SyndicationFeedReader>();
    }

    private void ConfigureYouTubeReader(IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton<IYouTubeSettings>(_ =>
        {
            var youTubeSettings = new YouTubeSettings();
            config.Bind("YouTube", youTubeSettings);
            return youTubeSettings;
        });
        services.TryAddSingleton<IYouTubeReader, YouTubeReader.YouTubeReader>();
    }

    private void ConfigureLinkedInManager(IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton<ILinkedInApplicationSettings>(_ =>
        {
            var linkedInApplicationSettings = new LinkedInApplicationSettings
            {
                ClientId = null!, ClientSecret = null!, AccessToken = null!, AuthorId = null!
            };
            config.Bind("LinkedIn", linkedInApplicationSettings);
            return linkedInApplicationSettings;
        });
        services.TryAddSingleton<ILinkedInManager, LinkedInManager>();
    }

    private void ConfigureFacebookManager(IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton<IFacebookApplicationSettings>(_ =>
        {
            var facebookApplicationSettings = new FacebookApplicationSettings();
            config.Bind("Facebook", facebookApplicationSettings);
            return facebookApplicationSettings;
        });
        services.TryAddSingleton<IFacebookManager, FacebookManager>();
    }

    private void ConfigureBlueskyManager(IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton<IBlueskySettings>(_ =>
        {
            var blueskySettings = new BlueskySettings();
            config.Bind("Bluesky", blueskySettings);
            return blueskySettings;
        });
        services.TryAddSingleton<IBlueskyManager, BlueskyManager>();
    }
}