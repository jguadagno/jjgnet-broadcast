using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.FixSourceDataShortUrl;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.FixSourceDataShortUrl.Models;
using JosephGuadagno.Utilities.Web.Shortener;
using JosephGuadagno.Utilities.Web.Shortener.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Console.WriteLine("Starting Application");

var hostBuilder = Host.CreateApplicationBuilder(args);

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true)
    .AddUserSecrets<Program>()
    .Build();

var settings = new Settings
{
    AutoMapper = null!
};
config.Bind("Settings", settings);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

var services = hostBuilder.Services;

// Add IConfiguration to DI
services.AddSingleton(config);

// Register Serilog as the logging provider
services.AddLogging(builder =>
{
    builder.AddSerilog(Log.Logger);
});

services.AddSingleton(settings);
services.AddSingleton<IAutoMapperSettings>(settings.AutoMapper);
services.AddSingleton<IBitlyConfiguration>(new BitlyConfiguration
{
    ApiRootUri = settings.BitlyApiRootUri, Token = settings.BitlyToken
});
services.AddSingleton<Bitly>();
services.AddDbContext<BroadcastingContext>(options => options.UseSqlServer("name=ConnectionStrings:JJGNetDatabaseSqlServer"));
services.AddSingleton<IYouTubeSourceDataStore, YouTubeSourceDataStore>();
services.AddSingleton<IYouTubeSourceRepository, YouTubeSourceRepository>();
services.AddSingleton<IYouTubeSourceManager, YouTubeSourceManager>();
services.AddSingleton<ISyndicationFeedSourceDataStore, SyndicationFeedSourceDataStore>();
services.AddSingleton<ISyndicationFeedSourceRepository, SyndicationFeedSourceRepository>();
services.AddSingleton<ISyndicationFeedSourceManager, SyndicationFeedSourceManager>();

// Add in AutoMapper
services.AddAutoMapper(mapperConfig =>
{
    mapperConfig.LicenseKey = settings.AutoMapper.LicenseKey;
    mapperConfig.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
}, typeof(Program));

services.AddHttpClient();

// Register your main application class
services.AddSingleton<App>();

// Build the provider
//var provider = services.BuildServiceProvider();

// Run the app
//var app = provider.GetRequiredService<App>();
//await app.Run();

using IHost host = hostBuilder.Build();
var app = host.Services.GetRequiredService<App>();
await app.Run();

Log.Logger.Information("Done");
Log.Logger.Information("Ending console app...");
Log.CloseAndFlush();
Console.WriteLine("Done");