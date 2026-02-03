using System.Reflection;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
using JosephGuadagno.Broadcasting.JsonFeedReader;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader.Models;
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
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using JosephGuadagno.Broadcasting.YouTubeReader;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using LinqToTwitter;
using LinqToTwitter.OAuth;

using Serilog;
using Serilog.Exceptions;

var currentDirectory = Directory.GetCurrentDirectory();

var builder = FunctionsApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.ConfigureFunctionsWebApplication();

// Configure Settings
builder.Configuration.SetBasePath(currentDirectory);
builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true, true);
builder.Configuration.AddEnvironmentVariables();

var settings = new JosephGuadagno.Broadcasting.Functions.Models.Settings
{
    AutoMapper = null!
};
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);
builder.Services.TryAddSingleton<IDatabaseSettings>(new DatabaseSettings
    { JJGNetDatabaseSqlServer = settings.JJGNetDatabaseSqlServer });
builder.Services.AddSingleton<IAutoMapperSettings>(settings.AutoMapper);

var randomPostSettings = new RandomPostSettings();
builder.Configuration.Bind("Settings:RandomPost", randomPostSettings);
builder.Services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);

var speakerEngagementsSettings = new SpeakingEngagementsReaderSettings
{
    SpeakingEngagementsFile = null
};
builder.Configuration.Bind("Settings:SpeakingEngagementsReader", speakerEngagementsSettings);
builder.Services.TryAddSingleton<ISpeakingEngagementsReaderSettings>(speakerEngagementsSettings);

// Configure the logger
string loggerFile = Path.Combine(currentDirectory, $"logs{Path.DirectorySeparatorChar}logs.txt");
ConfigureLogging(builder.Configuration, builder.Services, settings, loggerFile, "Functions");

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Add in AutoMapper
builder.Services.AddAutoMapper(mapperConfig =>
{
    mapperConfig.LicenseKey = settings.AutoMapper.LicenseKey;
    mapperConfig.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
}, typeof(Program));
    
// Configure all the services
// TODO: Refactor configuration setup to use dependency injection for settings
ConfigureKeyVault(builder.Services);
ConfigureTwitter(builder.Services);
ConfigureJsonFeedReader(builder.Services);
ConfigureSyndicationFeedReader(builder.Services);
ConfigureYouTubeReader(builder.Services);
ConfigureLinkedInManager(builder.Services);
ConfigureFacebookManager(builder.Services);
ConfigureBlueskyManager(builder.Services);
ConfigureFunction(builder.Services);

builder.Services.AddScoped<ISpeakingEngagementsReader, SpeakingEngagementsReader>();

builder.Build().Run();

void ConfigureLogging(IConfiguration configurationRoot, IServiceCollection services, ISettings appSettings, string logPath, string applicationName)
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
        .WriteTo.AzureTableStorage(appSettings.StorageAccount, storageTableName: "Logging",
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



void ConfigureTwitter(IServiceCollection services)
{
    services.TryAddSingleton<IAuthorizer>(s =>
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

void ConfigureJsonFeedReader(IServiceCollection services)
{
    services.TryAddSingleton<IJsonFeedReaderSettings>(s =>
    {
        var settings = new JsonFeedReaderSettings();
        var configuration = s.GetService<IConfiguration>();
        configuration.Bind("Settings:JsonFeedReader", settings);
        return settings;
    });
    services.TryAddSingleton<IJsonFeedReader, JsonFeedReader>();
}

void ConfigureSyndicationFeedReader(IServiceCollection services)
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

void ConfigureYouTubeReader(IServiceCollection services)
{
    services.TryAddSingleton<IYouTubeSettings>(s =>
    {
        var settings = new YouTubeSettings();
        var configuration = s.GetService<IConfiguration>();
        configuration.Bind("Settings:YouTube", settings);
        return settings;
    });
    services.TryAddSingleton<IYouTubeReader, YouTubeReader>();
}

void ConfigureLinkedInManager(IServiceCollection services)
{
    services.TryAddSingleton<ILinkedInApplicationSettings>(s =>
    {
        var settings = new LinkedInApplicationSettings();
        var configuration = s.GetService<IConfiguration>();
        configuration.Bind("Settings:LinkedIn", settings);
        return settings;
    });
    services.TryAddSingleton<ILinkedInManager, LinkedInManager>();
}

void ConfigureFacebookManager(IServiceCollection services)
{
    services.TryAddSingleton<IFacebookApplicationSettings>(s =>
    {
        var settings = new FacebookApplicationSettings();
        var configuration = s.GetService<IConfiguration>();
        configuration.Bind("Settings:Facebook", settings);
        return settings;
    });
    services.TryAddSingleton<IFacebookManager, FacebookManager>();
}

void ConfigureBlueskyManager(IServiceCollection services)
{
    services.TryAddSingleton<IBlueskySettings>(s =>
    {
        var settings = new BlueskySettings();
        var configuration = s.GetService<IConfiguration>();
        configuration.Bind("Settings:Bluesky", settings);
        return settings;
    });
    services.TryAddSingleton<IBlueskyManager, BlueskyManager>();
}
    
void ConfigureRepositories(IServiceCollection services)
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

void ConfigureFunction(IServiceCollection services)
{
    services.AddHttpClient();
        
    services.TryAddSingleton(s =>
    {
        var settings = s.GetService<ISettings>();
        if (settings is null)
        {
            throw new ApplicationException("Failed to get settings from ServiceCollection");
        }
        return new ConfigurationRepository(settings.StorageAccount);
    });
    
    services.TryAddSingleton(s =>
    {
        var settings = s.GetService<ISettings>();
        if (settings is null)
        {
            throw new ApplicationException("Failed to get settings from ServiceCollection");
        }
        return new TokenRefreshRepository(settings.StorageAccount);
    });
    services.TryAddSingleton(s =>
    {
        var settings = s.GetService<ISettings>();
        if (settings is null)
        {
            throw new ApplicationException("Failed to get settings from ServiceCollection");
        }
        return new SourceDataRepository(settings.StorageAccount);
    });
    services.TryAddSingleton(s =>
    {
        var settings = s.GetService<ISettings>();
        if (settings is null)
        {
            throw new ApplicationException("Failed to get settings from ServiceCollection");
        }
        var httpClient = s.GetService(typeof(HttpClient)) as HttpClient;
            
        return new JosephGuadagno.Utilities.Web.Shortener.Bitly(httpClient,
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