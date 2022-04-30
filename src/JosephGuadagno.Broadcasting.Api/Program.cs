using System.Reflection;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Exceptions;
using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;
using Settings = JosephGuadagno.Broadcasting.Api.Models.Settings;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);
builder.Services.TryAddSingleton<IDatabaseSettings>(new DatabaseSettings
    { JJGNetDatabaseSqlServer = settings.JJGNetDatabaseSqlServer });

builder.Services.AddApplicationInsightsTelemetry(settings.AppInsightsKey);

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureLogging(builder.Services, settings, fullyQualifiedLogFile, "Api");

// Register DI services
ConfigureApplication(builder.Services);

// ASP.NET Core API stuff
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "JosephGuadagno.NET Broadcasting API", 
            Version = "v1",
            Description = "The API for the JosephGuadagno.NET Broadcasting Application",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Joseph Guadagno",
                Email = "jguadagno@hotmail.com",
                Url = new Uri("https://www.josephguadagno.net"),
            }
        });
                
    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

void ConfigureLogging(IServiceCollection services, ISettings configSettings, string logPath, string applicationName)
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
        .WriteTo.AzureTableStorage(configSettings.StorageAccount, storageTableName:"Logging")
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddApplicationInsights(configSettings.AppInsightsKey);
        loggingBuilder.AddSerilog(logger);
    });
}

void ConfigureApplication(IServiceCollection services)
{
    ConfigureRepositories(services);
}

void ConfigureRepositories(IServiceCollection services)
{
    services.AddDbContext<BroadcastingContext>(ServiceLifetime.Scoped);
            
    // Engagements
    services.TryAddScoped<IEngagementDataStore>(s =>
    {
        var databaseSettings = s.GetService<IDatabaseSettings>();
        if (databaseSettings is null)
        {
            throw new ApplicationException("Failed to get a Settings object from ServiceCollection");
        }
        return new EngagementDataStore(databaseSettings);
    });
    services.TryAddScoped<IEngagementRepository>(s =>
    {
        var engagementDataStore = s.GetService<IEngagementDataStore>();
        if (engagementDataStore is null)
        {
            throw new ApplicationException("Failed to get an EngagementDataStore from ServiceCollection");
        }
        return new EngagementRepository(engagementDataStore);
    });
    services.TryAddScoped<IEngagementManager, EngagementManager>();

    // ScheduledItem
    services.TryAddScoped<IScheduledItemDataStore>(s =>
    {
        var databaseSettings = s.GetService<IDatabaseSettings>();
        if (databaseSettings is null)
        {
            throw new ApplicationException("Failed to get a settings object from ServiceCollection");
        }
        return new ScheduledItemDataStore(databaseSettings);
    });
    services.TryAddScoped<IScheduledItemRepository>(s =>
    {
        var scheduledItemDataStore = s.GetService<IScheduledItemDataStore>();
        if (scheduledItemDataStore is null)
        {
            throw new ApplicationException("Failed to get a ScheduledItemDataStore object from ServiceCollection");
        }
        return new ScheduledItemRepository(scheduledItemDataStore);
    });
    services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();
}