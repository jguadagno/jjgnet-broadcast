using System.Reflection;

using Azure.Communication.Email;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.HealthChecks;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Managers.Bluesky;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Interfaces;
using JosephGuadagno.Broadcasting.Managers.Bluesky.Models;
using JosephGuadagno.Broadcasting.Managers.Twitter;
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
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Logs;

using Serilog;

var currentDirectory = Directory.GetCurrentDirectory();

var builder = FunctionsApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.ConfigureFunctionsWebApplication();

// Configure Settings
builder.Configuration.SetBasePath(currentDirectory);
#if DEBUG
    builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true, true);
#endif
builder.Configuration.AddEnvironmentVariables();

// Register Settings via IOptions
builder.Services.Configure<JosephGuadagno.Broadcasting.Functions.Models.Settings>(builder.Configuration.GetSection("Settings"));
builder.Services.AddOptions<JosephGuadagno.Broadcasting.Functions.Models.Settings>().ValidateDataAnnotations();

var emailSettings = new EmailSettings
{
    FromAddress = string.Empty, FromDisplayName = string.Empty,
    ReplyToAddress = string.Empty, ReplyToDisplayName = string.Empty,
    AzureCommunicationsConnectionString = string.Empty
};
builder.Configuration.Bind("Email", emailSettings);
builder.Services.TryAddSingleton<IEmailSettings>(emailSettings);
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddOptions<EmailSettings>().ValidateDataAnnotations();

var randomPostSettings = new RandomPostSettings
{
    ExcludedCategories = []
};
builder.Configuration.Bind("RandomPost", randomPostSettings);
builder.Services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);
builder.Services.Configure<RandomPostSettings>(builder.Configuration.GetSection("RandomPost"));
builder.Services.AddOptions<RandomPostSettings>().ValidateDataAnnotations();

var speakerEngagementsSettings = new SpeakingEngagementsReaderSettings
{
    SpeakingEngagementsFile = null
};
builder.Configuration.Bind("SpeakingEngagementsReader", speakerEngagementsSettings);
builder.Services.TryAddSingleton<ISpeakingEngagementsReaderSettings>(speakerEngagementsSettings);
builder.Services.Configure<SpeakingEngagementsReaderSettings>(builder.Configuration.GetSection("SpeakingEngagementsReader"));
builder.Services.AddOptions<SpeakingEngagementsReaderSettings>().ValidateDataAnnotations();

var eventPublisherSettings = new EventPublisherSettings { TopicEndpointSettings = [] };
var endpoints = builder.Configuration.GetSection("EventGridTopics:TopicEndpointSettings").Get<List<TopicEndpointSettings>>();
if (endpoints != null)
{
    foreach (var endpoint in endpoints)
    {
        eventPublisherSettings.TopicEndpointSettings.Add(endpoint);
    }
}
builder.Services.TryAddSingleton<IEventPublisherSettings>(eventPublisherSettings);
builder.Services.Configure<EventPublisherSettings>(builder.Configuration.GetSection("EventGridTopics"));
builder.Services.AddOptions<EventPublisherSettings>().ValidateDataAnnotations();

// Configure the telemetry and logging
string loggerFile = Path.Combine(currentDirectory, $"logs{Path.DirectorySeparatorChar}logs.txt");
ConfigureTelemetryAndLogging(builder.Services,loggerFile, "Functions");

// Add in AutoMapper
var autoMapperSettings = new AutoMapperSettings();
builder.Configuration.Bind("AutoMapper", autoMapperSettings);
builder.Services.AddAutoMapper(mapperConfig =>
{
    mapperConfig.LicenseKey = autoMapperSettings.LicenseKey;
    mapperConfig.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
}, typeof(Program));
    
// Configure all the services
builder.AddAzureQueueServiceClient("QueueAccount");
builder.AddAzureBlobServiceClient("BlobAccount");
builder.AddAzureTableServiceClient("TableAccount");

ConfigureKeyVault(builder.Services);
ConfigureFunction(builder.Services);
ConfigureBitly(builder.Services, builder.Configuration);
ConfigureTwitter(builder.Services, builder.Configuration);
ConfigureSyndicationFeedReader(builder.Services, builder.Configuration);
ConfigureYouTubeReader(builder.Services, builder.Configuration);
ConfigureLinkedInManager(builder.Services, builder.Configuration);
ConfigureFacebookManager(builder.Services, builder.Configuration);
ConfigureBlueskyManager(builder.Services, builder.Configuration);

builder.Services.AddScoped<ISpeakingEngagementsReader, SpeakingEngagementsReader>();

// Register external-dependency readiness health checks
builder.Services.AddHealthChecks()
    .AddCheck<BitlyHealthCheck>("bitly", tags: ["ready"])
    .AddCheck<TwitterHealthCheck>("twitter", tags: ["ready"])
    .AddCheck<FacebookHealthCheck>("facebook", tags: ["ready"])
    .AddCheck<LinkedInHealthCheck>("linkedin", tags: ["ready"])
    .AddCheck<BlueskyHealthCheck>("bluesky", tags: ["ready"])
    .AddCheck<EventGridHealthCheck>("event-grid", tags: ["ready"]);

builder.Build().Run();

void ConfigureTelemetryAndLogging(IServiceCollection services, string logPath, string applicationName)
{
    services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults();

    var logger = new LoggerConfiguration()
        .ConfigureSerilog(applicationName, logPath)
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddOpenTelemetry(options =>
        {
            options.AddConsoleExporter();
        });
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

void ConfigureFunction(IServiceCollection services)
{
    services.AddHttpClient();

    services.TryAddSingleton<IEventPublisher, EventPublisher>();

    builder.AddSqlServerDbContext<BroadcastingContext>("JJGNetDatabaseSqlServer");
    builder.EnrichSqlServerDbContext<BroadcastingContext>(
        configureSettings: sqlServerSettings =>
        {
            sqlServerSettings.DisableRetry = false;
            sqlServerSettings.CommandTimeout = 30; // seconds
        });

    services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
    services.TryAddScoped<IEngagementManager, EngagementManager>();

    services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
    services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();

    services.TryAddScoped<IYouTubeSourceDataStore, YouTubeSourceDataStore>();
    services.TryAddScoped<IYouTubeSourceManager, YouTubeSourceManager>();

    services.TryAddScoped<ISyndicationFeedSourceDataStore, SyndicationFeedSourceDataStore>();
    services.TryAddScoped<ISyndicationFeedSourceManager, SyndicationFeedSourceManager>();

    services.TryAddScoped<IFeedCheckDataStore, FeedCheckDataStore>();
    services.TryAddScoped<IFeedCheckManager, FeedCheckManager>();

    services.TryAddScoped<ITokenRefreshDataStore, TokenRefreshDataStore>();
    services.TryAddScoped<ITokenRefreshManager, TokenRefreshManager>();

    services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
    
    services.TryAddScoped<ISocialMediaPlatformDataStore, SocialMediaPlatformDataStore>();
    services.TryAddScoped<ISocialMediaPlatformManager, SocialMediaPlatformManager>();

    // RBAC Phase 1
    services.TryAddScoped<IApplicationUserDataStore, ApplicationUserDataStore>();
    services.TryAddScoped<IRoleDataStore, RoleDataStore>();
    services.TryAddScoped<IUserApprovalLogDataStore, UserApprovalLogDataStore>();
    services.TryAddScoped<IEmailTemplateDataStore, EmailTemplateDataStore>();
    services.TryAddScoped<IUserApprovalManager, UserApprovalManager>();

    // Email
    services.TryAddScoped<IEmailSender, EmailSender>();
    services.TryAddScoped<IEmailTemplateManager, EmailTemplateManager>();
    services.TryAddSingleton(sp =>
    {
        var emailSettingsService = sp.GetRequiredService<IEmailSettings>();
        return new EmailClient(emailSettingsService.AzureCommunicationsConnectionString);
    });
}

void ConfigureBitly(IServiceCollection services, IConfiguration config)
{
    var bitlySettings = new BitlyConfiguration();
    config.Bind("Bitly", bitlySettings);
    services.TryAddSingleton<IBitlyConfiguration>(bitlySettings);
    services.TryAddSingleton(s =>
    {
        var httpClient = s.GetService(typeof(HttpClient)) as HttpClient;
        return new JosephGuadagno.Utilities.Web.Shortener.Bitly(httpClient, bitlySettings);
    });
    services.TryAddSingleton<IUrlShortener, UrlShortener>();
}

void ConfigureTwitter(IServiceCollection services, IConfiguration config)
{
    var twitterSettings = new InMemoryCredentialStore();
    config.Bind("Twitter", twitterSettings);
    services.TryAddSingleton(twitterSettings);

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
    services.TryAddSingleton<ITwitterManager, TwitterManager>();
}

void ConfigureSyndicationFeedReader(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<ISyndicationFeedReaderSettings>(_ =>
    {
        var syndicationFeedReaderSettings = new SyndicationFeedReaderSettings();
        config.Bind("SyndicationFeedReader", syndicationFeedReaderSettings);
        return syndicationFeedReaderSettings;
    });
    services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader>();
}

void ConfigureYouTubeReader(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<IYouTubeSettings>(_ =>
    {
        var youTubeSettings = new YouTubeSettings();
        config.Bind("YouTube", youTubeSettings);
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
        config.Bind("LinkedIn", linkedInApplicationSettings);
        return linkedInApplicationSettings;
    });
    services.TryAddSingleton<ILinkedInManager, LinkedInManager>();
}

void ConfigureFacebookManager(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<IFacebookApplicationSettings>(_ =>
    {
        var facebookApplicationSettings = new FacebookApplicationSettings();
        config.Bind("Facebook", facebookApplicationSettings);
        return facebookApplicationSettings;
    });
    services.TryAddSingleton<IFacebookManager, FacebookManager>();
}

void ConfigureBlueskyManager(IServiceCollection services, IConfiguration config)
{
    services.TryAddSingleton<IBlueskySettings>(_ =>
    {
        var blueskySettings = new BlueskySettings();
        config.Bind("Bluesky", blueskySettings);
        return blueskySettings;
    });
    services.TryAddSingleton<IBlueskyManager, BlueskyManager>();
}
