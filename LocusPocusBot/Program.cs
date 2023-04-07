using System;
using System.IO;
using LocusPocusBot;
using LocusPocusBot.Data;
using LocusPocusBot.Handlers;
using LocusPocusBot.Rooms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlainConsoleLoggerFormatter;

IHost host = new HostBuilder()
    .ConfigureAppConfiguration(ConfigureApp)
    .ConfigureServices(ConfigureServices)
    .ConfigureLogging(ConfigureLogging)
    .UseConsoleLifetime(opts => opts.SuppressStatusMessages = true)
    .Build();

ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    host.Run();
}
catch (Exception ex)
{
    logger.LogError(ex, "Unhandled exception");

    // Required to stop all the threads.
    // With "return 1", the process could actually stay online forever
    Environment.Exit(1);
}


void ConfigureApp(HostBuilderContext hostContext, IConfigurationBuilder configApp)
{
    // Load the application settings
    configApp.SetBasePath(Directory.GetCurrentDirectory());
    configApp.AddJsonFile("appsettings.json", optional: true);
    configApp.AddEnvironmentVariables();
}

void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
{
    logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
    logging.AddConsoleFormatter<PlainConsoleFormatter, PlainConsoleFormatterOptions>();
    logging.AddConsole(options => options.FormatterName = nameof(PlainConsoleFormatter));
}

void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
{
    services.Configure<BotConfiguration>(hostContext.Configuration.GetSection("Bot"));
    services.Configure<DatabaseConfiguration>(hostContext.Configuration.GetSection("Database"));

    // This also registers the service as a transient service
    services.AddHttpClient<IRoomsService, RoomsService>();

    services.AddSingleton<IDatabaseService, DatabaseService>();
    services.AddSingleton<IBotService, BotService>();
    services.AddScoped<IUpdateProcessor, UpdateProcessor>();
    services.AddHandlers();

    services.AddSingleton(new Department[]
    {
        new("E0503", "Povo", "povo"),
        new("E0301", "Mesiano", "mesiano"),
        new("E0601", "Sociologia", "sociologia"),
        new("E0801", "Lettere", "lettere"),
        new("E0101", "Economia", "economia"),
        new("E0705", "Psicologia", "psicologia"),
    });

    services.AddHostedService<SettingsValidationHostedService>();
    services.AddHostedService<BotHostedService>();
    services.AddHostedService<FetchSchedulerHostedService>();
}
