using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);
builder.Services.AddApplicationInsightsTelemetry(settings.AppInsightsKey);

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureLogging(builder.Services, settings, fullyQualifiedLogFile, "Web");

// Register DI services
ConfigureApplication(builder.Services);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
    services.AddHttpClient();
    services.TryAddSingleton<IEngagementService, EngagementService>();
    services.TryAddSingleton<IScheduledItemService, ScheduledItemService>();
}