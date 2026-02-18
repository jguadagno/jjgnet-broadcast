using System.Reflection;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
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
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Models;
using JosephGuadagno.Broadcasting.YouTubeReader;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Models;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
builder.Services.AddSingleton<IAutoMapperSettings>(settings.AutoMapper);

var randomPostSettings = new RandomPostSettings
{
    ExcludedCategories = []
};
builder.Configuration.Bind("Settings:RandomPost", randomPostSettings);
builder.Services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);

var speakerEngagementsSettings = new SpeakingEngagementsReaderSettings
{
    SpeakingEngagementsFile = null
};
builder.Configuration.Bind("Settings:SpeakingEngagementsReader", speakerEngagementsSettings);
builder.Services.TryAddSingleton<ISpeakingEngagementsReaderSettings>(speakerEngagementsSettings);

var eventPublisherSettings = new EventPublisherSettings { TopicEndpointSettings = [] };
var endpoints = builder.Configuration.GetSection("Settings:EventGridTopics:TopicEndpointSettings").Get<List<TopicEndpointSettings>>();
if (endpoints != null)
{
    foreach (var endpoint in endpoints)
    {
        eventPublisherSettings.TopicEndpointSettings.Add(endpoint);
    }
}
builder.Services.TryAddSingleton<IEventPublisherSettings>(eventPublisherSettings);

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
ConfigureFunction(builder.Services, settings);
ConfigureTwitter(builder.Services, settings);
ConfigureSyndicationFeedReader(builder.Services, builder.Configuration);
ConfigureYouTubeReader(builder.Services, builder.Configuration);
ConfigureLinkedInManager(builder.Services, builder.Configuration);
ConfigureFacebookManager(builder.Services, builder.Configuration);
ConfigureBlueskyManager(builder.Services, builder.Configuration);

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

void ConfigureKeyVault(IServiceCollection services)
{
    services.AddAzureClients(clientBuilder =>
    {
        clientBuilder.AddSecretClient(builder.Configuration.GetSection("KeyVault"));
    });
    services.TryAddScoped<IKeyVault, KeyVault>();
}

void ConfigureFunction(IServiceCollection services, ISettings appSettings)
{
    services.AddHttpClient();

    services.TryAddSingleton(s =>
    {
        var httpClient = s.GetService(typeof(HttpClient)) as HttpClient;

        return new JosephGuadagno.Utilities.Web.Shortener.Bitly(httpClient,
            new BitlyConfiguration
            {
                ApiRootUri = appSettings.BitlyAPIRootUri,
                Token = appSettings.BitlyToken
            });
    });
    services.TryAddSingleton<IUrlShortener, UrlShortener>();
    services.TryAddSingleton<IEventPublisher, EventPublisher>();

    builder.AddSqlServerDbContext<BroadcastingContext>("JJGNetDatabaseSqlServer");
    builder.EnrichSqlServerDbContext<BroadcastingContext>(
        configureSettings: sqlServerSettings =>
        {
            sqlServerSettings.DisableRetry = false;
            sqlServerSettings.CommandTimeout = 30; // seconds
        });

    services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
    services.TryAddScoped<IEngagementRepository, EngagementRepository>();
    services.TryAddScoped<IEngagementManager, EngagementManager>();

    services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
    services.TryAddScoped<IScheduledItemRepository, ScheduledItemRepository>();
    services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();

    services.TryAddScoped<IYouTubeSourceDataStore, YouTubeSourceDataStore>();
    services.TryAddScoped<IYouTubeSourceRepository, YouTubeSourceRepository>();
    services.TryAddScoped<IYouTubeSourceManager, YouTubeSourceManager>();

    services.TryAddScoped<ISyndicationFeedSourceDataStore, SyndicationFeedSourceDataStore>();
    services.TryAddScoped<ISyndicationFeedSourceRepository, SyndicationFeedSourceRepository>();
    services.TryAddScoped<ISyndicationFeedSourceManager, SyndicationFeedSourceManager>();

    services.TryAddScoped<IFeedCheckDataStore, FeedCheckDataStore>();
    services.TryAddScoped<IFeedCheckRepository, FeedCheckRepository>();
    services.TryAddScoped<IFeedCheckManager, FeedCheckManager>();

    services.TryAddScoped<ITokenRefreshDataStore, TokenRefreshDataStore>();
    services.TryAddScoped<ITokenRefreshRepository, TokenRefreshRepository>();
    services.TryAddScoped<ITokenRefreshManager, TokenRefreshManager>();
}

void ConfigureTwitter(IServiceCollection services, ISettings appSettings)
{
    services.TryAddSingleton<IAuthorizer>(_ => new SingleUserAuthorizer
    {
        CredentialStore = new InMemoryCredentialStore
        {
            ConsumerKey = appSettings.TwitterApiKey,
            ConsumerSecret = appSettings.TwitterApiSecret,
            OAuthToken = appSettings.TwitterAccessToken,
            OAuthTokenSecret = appSettings.TwitterAccessTokenSecret
        }
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

void ConfigureSyndicationFeedReader(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<ISyndicationFeedReaderSettings>(_ =>
    {
        var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings();
        config.Bind("Settings:SyndicationFeedReader", syndicationFeedReaderSettings);
        return syndicationFeedReaderSettings;
    });
    services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader>();
}

void ConfigureYouTubeReader(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<IYouTubeSettings>(_ =>
    {
        var youTubeSettings = new YouTubeSettings();
        config.Bind("Settings:YouTube", youTubeSettings);
        return youTubeSettings;
    });
    services.TryAddSingleton<IYouTubeReader, YouTubeReader>();
}

void ConfigureLinkedInManager(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<ILinkedInApplicationSettings>(_ =>
    {
        var linkedInApplicationSettings = new LinkedInApplicationSettings
        {
            ClientId = null!,
            ClientSecret = null!,
            AccessToken = null!,
            AuthorId = null!
        };
        config.Bind("Settings:LinkedIn", linkedInApplicationSettings);
        return linkedInApplicationSettings;
    });
    services.TryAddSingleton<ILinkedInManager, LinkedInManager>();
}

void ConfigureFacebookManager(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<IFacebookApplicationSettings>(_ =>
    {
        var facebookApplicationSettings = new FacebookApplicationSettings();
        config.Bind("Settings:Facebook", facebookApplicationSettings);
        return facebookApplicationSettings;
    });
    services.TryAddSingleton<IFacebookManager, FacebookManager>();
}

void ConfigureBlueskyManager(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<IBlueskySettings>(_ =>
    {
        var blueskySettings = new BlueskySettings();
        config.Bind("Settings:Bluesky", blueskySettings);
        return blueskySettings;
    });
    services.TryAddSingleton<IBlueskyManager, BlueskyManager>();
}