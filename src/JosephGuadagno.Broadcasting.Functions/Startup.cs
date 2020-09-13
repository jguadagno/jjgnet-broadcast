using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Functions;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using LinqToTwitter;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace JosephGuadagno.Broadcasting.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Setup the Configuration Source
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                .AddEnvironmentVariables()
                .Build();
            builder.Services.AddSingleton<IConfiguration>(config);
            
            // Bind the 'Settings' section to the ISettings class
            var settings = new Domain.Models.Settings();
            config.Bind("Settings", settings);
            builder.Services.TryAddSingleton<ISettings>(settings);
            
            ConfigureTwitter(builder);
            ConfigureCollectors(builder);
        }

        public void ConfigureTwitter(IFunctionsHostBuilder builder)
        {
            builder.Services.TryAddSingleton<IAuthorizer>(s =>
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
            builder.Services.TryAddSingleton(s =>
            {
                var authorizer = s.GetService<IAuthorizer>();
                return new TwitterContext(authorizer);
            });
            builder.Services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
                return new SourceDataRepository(settings.StorageAccount);
            });
        }

        public void ConfigureCollectors(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            
            builder.Services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
                return new ConfigurationRepository(settings.StorageAccount);
            });
            builder.Services.TryAddSingleton(s =>
            {
                var settings = s.GetService<ISettings>();
                return new SourceDataRepository(settings.StorageAccount);
            });
            builder.Services.TryAddSingleton(s =>
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
        }
    }
}