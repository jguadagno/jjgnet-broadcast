using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;
using JosephGuadagno.Broadcasting.JsonFeedReader.Models;
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
using NLog;
using NLog.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Functions
{
    public class Startup
    {
        static Task Main(string[] args)
        {

            var currentDirectory = AppContext.BaseDirectory;
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder
                        .SetBasePath(currentDirectory)
                        .AddJsonFile("local.settings.json", true)
                        .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                        .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) => 
                {
                    // Add Logging
                    services.AddLogging();
                    LogManager.Setup()
                        .SetupExtensions(e => e.AutoLoadAssemblies(false))
                        .LoadConfigurationFromFile(currentDirectory + Path.DirectorySeparatorChar + "nlog.config", optional: false)
                        .LoadConfiguration(configurationBuilder => configurationBuilder.LogFactory.AutoShutdown = false);
                    SetLoggingGlobalDiagnosticsContext();

                    // Add HttpClient
                    services.AddHttpClient();

                    // Add Custom Services
                    
                    // Bind the 'Settings' section to the ISettings class
                    var config = context.Configuration;
                    var settings = new Domain.Models.Settings();
                    config.Bind("Settings", settings);
                    services.TryAddSingleton<ISettings>(settings);

                    var randomPostSettings = new Domain.Models.RandomPostSettings();
                    config.Bind("Settings:RandomPost", randomPostSettings);
                    services.TryAddSingleton<IRandomPostSettings>(randomPostSettings);
            
                    // Configure the logger
                    services.AddLogging((loggingBuilder =>
                    {
                        //loggingBuilder.ClearProviders();
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        loggingBuilder.AddConfiguration(config);
                        loggingBuilder.AddNLog(new NLogProviderOptions {ShutdownOnDispose = true});
                    }));

                    // Configure all the services
                    ConfigureTwitter(services);
                    ConfigureJsonFeedReader(services);
                    ConfigureSyndicationFeedReader(services);
                    ConfigureYouTubeReader(services);
                    ConfigureFunction(services);
                    
                })
                .Build();

            return host.RunAsync();
        }

        private static void SetLoggingGlobalDiagnosticsContext()
        {
            
            var executingAssembly = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            var productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            GlobalDiagnosticsContext.Set("ExecutingAssembly-AssemblyVersion", executingAssembly);
            GlobalDiagnosticsContext.Set("ExecutingAssembly-FileVersion", fileVersion);
            GlobalDiagnosticsContext.Set("ExecutingAssembly-ProductVersion", productVersion);
        }

        private static void ConfigureTwitter(IServiceCollection services)
        {
            services.TryAddSingleton<IAuthorizer>(s =>
            {
                var settings = s.GetService<ISettings>();
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
                return new TwitterContext(authorizer);
            });
            services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
                return new SourceDataRepository(settings.StorageAccount);
            });
        }

        private static void ConfigureJsonFeedReader(IServiceCollection services)
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

        private static void ConfigureSyndicationFeedReader(IServiceCollection services)
        {
            services.TryAddSingleton<ISyndicationFeedReaderSettings>(s =>
            {
                var settings = new SyndicationFeedReaderSettings();
                var configuration = s.GetService<IConfiguration>();
                configuration.Bind("Settings:SyndicationFeedReader", settings);
                return settings;
            });
            services.TryAddSingleton<ISyndicationFeedReader, SyndicationFeedReader.SyndicationFeedReader>();

        }

        private static void ConfigureYouTubeReader(IServiceCollection services)
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

        private static void ConfigureFunction(IServiceCollection services)
        {
            services.AddHttpClient();
            
            services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
                return new ConfigurationRepository(settings.StorageAccount);
            });
            services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
                return new SourceDataRepository(settings.StorageAccount);
            });
            services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
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
        }
    }
}