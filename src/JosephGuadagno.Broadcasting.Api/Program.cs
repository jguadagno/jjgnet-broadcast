using System.Reflection;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Exceptions;
using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;
using Settings = JosephGuadagno.Broadcasting.Api.Models.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpLogging(
    options => { options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All; });

var settings = new Settings
{
    AutoMapper = null!
};
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);
builder.Services.TryAddSingleton<IDatabaseSettings>(new DatabaseSettings
    { JJGNetDatabaseSqlServer = settings.JJGNetDatabaseSqlServer });
builder.Services.AddSingleton<IAutoMapperSettings>(settings.AutoMapper);

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

builder.Services.AddSingleton<ITelemetryInitializer, AzureWebAppRoleEnvironmentTelemetryInitializer>();
builder.Services.AddApplicationInsightsTelemetry();

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureLogging(builder.Configuration, builder.Services, settings, fullyQualifiedLogFile, "Api");

// Register DI services
ConfigureApplication(builder.Services);

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = settings.AutoMapper.LicenseKey;
    config.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
}, typeof(Program));

// ASP.NET Core API stuff
builder.Services.AddControllers();

// Configure OpenAPI
// Learn more about configuring OpenAPI at https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Set document metadata
        document.Info = new OpenApiInfo
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
        };
        
        // Configure OAuth2 security
        var scopes = JosephGuadagno.Broadcasting.Domain.Scopes.AllAccessToDictionary(settings.ApiScopeUrl);
        scopes.Add($"{settings.ApiScopeUrl}user_impersonation", "Access application on user behalf");
        
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["oauth2"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Implicit = new OpenApiOAuthFlow()
                {
                    AuthorizationUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri("https://login.microsoftonline.com/common/common/v2.0/token"),
                    Scopes = scopes
                }
            }
        };
        
        // Add security requirement to all operations
        if (document.Paths != null && document.Paths.Count > 0)
        {
            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        },
                        Scheme = "oauth2",
                        Name = "oauth2",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            };
            
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    operation.Security ??= new List<OpenApiSecurityRequirement>();
                    operation.Security.Add(securityRequirement);
                }
            }
        }
        
        return Task.CompletedTask;
    });
    
    // Add XML documentation
    options.AddDocumentTransformer<JosephGuadagno.Broadcasting.Api.XmlDocumentTransformer>();
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("JosephGuadagno.NET Broadcasting API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        
        // Configure OAuth2 for Scalar with all scopes
        var scopes = JosephGuadagno.Broadcasting.Domain.Scopes.AllAccessToDictionary(settings.ApiScopeUrl);
        scopes.Add($"{settings.ApiScopeUrl}user_impersonation", "Access application on user behalf");
        
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecurityScheme = "oauth2",
            OAuth2 = new()
            {
                ClientId = settings.SwaggerClientId,
                Scopes = scopes.Keys.ToArray()
            }
        };
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

void ConfigureLogging(IConfigurationRoot configurationRoot, IServiceCollection services, ISettings configSettings, string logPath, string applicationName)
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
        .WriteTo.AzureTableStorage(configSettings.StorageAccount, storageTableName:"Logging", keyGenerator:new SerilogKeyGenerator())
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

void ConfigureApplication(IServiceCollection services)
{
    ConfigureRepositories(services);
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