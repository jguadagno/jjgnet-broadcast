using System.Reflection;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using LinqToTwitter;
using LinqToTwitter.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Twitter.IntegrationTests;

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

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
        });

        var credentialStore = new InMemoryCredentialStore();
        config.Bind("Twitter", credentialStore);
        services.TryAddSingleton(credentialStore);

        services.TryAddSingleton<IAuthorizer>(s =>
        {
            var store = s.GetRequiredService<InMemoryCredentialStore>();
            return new SingleUserAuthorizer { CredentialStore = store };
        });

        services.TryAddSingleton(s =>
        {
            var authorizer = s.GetRequiredService<IAuthorizer>();
            return new TwitterContext(authorizer);
        });

        services.TryAddSingleton<ITwitterManager, TwitterManager>();
    }
}
