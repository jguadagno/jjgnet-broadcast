using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
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
using Serilog;
using Serilog.Exceptions;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder //.SetBasePath(hostBuilder.HostingEnvironment.ContentRootPath)
                .AddJsonFile("local.settings.json", true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        });
    }
    
    public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
    {
        var config = hostBuilderContext.Configuration;
            
        services.AddSingleton(config);

        // Bind the 'Settings' section to the ISettings class
        var settings = new Models.Settings
        {
            AutoMapper = null!
        };
        config.Bind("Settings", settings);
        services.TryAddSingleton<ISettings>(settings);
        services.TryAddSingleton<IDatabaseSettings>(new DatabaseSettings
            { JJGNetDatabaseSqlServer = settings.JJGNetDatabaseSqlServer });
        services.AddSingleton<IAutoMapperSettings>(settings.AutoMapper);

        var randomPostSettings = new RandomPostSettings();
        config.Bind("Settings:RandomPost", randomPostSettings);
        services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);
        
        // Configure the logger
        string logPath = Path.Combine(hostBuilderContext.HostingEnvironment.ContentRootPath, "logs\\logs.txt");
        ConfigureLogging(services, config, logPath, "Functions_Test");
        
        // Configure all the services
        // Add in AutoMapper
        services.AddAutoMapper(mapperConfig =>
        {
            mapperConfig.LicenseKey = settings.AutoMapper.LicenseKey;
            mapperConfig.AddProfile<Data.Sql.MappingProfiles.BroadcastingProfile>();
        }, typeof(Program));

        ConfigureKeyVault(services);
        ConfigureTwitter(services);
        ConfigureJsonFeedReader(services);
        ConfigureSyndicationFeedReader(services);
        ConfigureYouTubeReader(services);
        ConfigureFunction(services);
    }

    private void ConfigureLogging(IServiceCollection services, IConfiguration config, string logPath, string applicationName)
    {
        var logger = new LoggerConfiguration()
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
            .WriteTo.AzureTableStorage(config["Values:AzureWebJobsStorage"], storageTableName:"Logging")
            .CreateLogger();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddApplicationInsights();
            loggingBuilder.AddSerilog(logger);
        });
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

    }

    private void ConfigureJsonFeedReader(IServiceCollection services)
    {
        services.TryAddSingleton<IJsonFeedReaderSettings>(s =>
        {
            var settings = new JsonFeedReaderSettings();
            var configuration = s.GetService<IConfiguration>();
            if (configuration is null)
            {
                throw new ApplicationException("Failed to get the configuration from the ServiceCollection");
            }
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
            if (configuration is null)
            {
                throw new ApplicationException("Failed to get the configuration from the ServiceCollection");
            }
            configuration.Bind("Settings:SyndicationFeedReader", settings);
            return settings;
        });
        services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader.SyndicationFeedReader>();
    }

    private void ConfigureYouTubeReader(IServiceCollection services)
    {
        services.TryAddSingleton<IYouTubeSettings>(s =>
        {
            var settings = new YouTubeSettings();
            var configuration = s.GetService<IConfiguration>();
            if (configuration is null)
            {
                throw new ApplicationException("Failed to get the configuration from the ServiceCollection");
            }
            configuration.Bind("Settings:YouTube", settings);
            return settings;
        });
        services.TryAddSingleton<IYouTubeReader, YouTubeReader.YouTubeReader>();
    }

    private void ConfigureRepositories(IServiceCollection services)
    {
        services.AddDbContext<BroadcastingContext>();

        // Engagements
        services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
        services.TryAddScoped<IEngagementRepository, EngagementRepository>();
        services.TryAddScoped<IEngagementManager, EngagementManager>();

        // ScheduledItem
        services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
        services.TryAddScoped<IScheduledItemRepository, ScheduledItemRepository>();
        services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();
    }

    void ConfigureKeyVault(IServiceCollection services)
    {
        services.TryAddSingleton(s =>
        {
            var applicationSettings = s.GetService<ISettings>();
            if (applicationSettings is null)
            {
                throw new ApplicationException("Failed to get application settings from ServiceCollection");
            }

            return new SecretClient(new Uri(applicationSettings.KeyVault.KeyVaultUri),
                new ChainedTokenCredential(new ManagedIdentityCredential(),
                    new ClientSecretCredential(applicationSettings.KeyVault.TenantId, applicationSettings.KeyVault.ClientId,
                        applicationSettings.KeyVault.ClientSecret)));
        });
    
        services.TryAddScoped<IKeyVault, KeyVault>();
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