using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Extensions.Azure;

namespace JosephGuadagno.Broadcasting.Functions.IntegrationTests;

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

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
        });

        services.AddHttpClient();

        ConfigureKeyVault(services, config);
        ConfigureTwitter(services, config);
    }

    private void ConfigureKeyVault(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddSecretClient(configuration.GetSection("KeyVault"));
        });
    }

    private void ConfigureTwitter(IServiceCollection services, IConfiguration config)
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
    }
}
