using CustomConsoleLogger;
using LocusPocusBot.Data;
using LocusPocusBot.Handlers;
using LocusPocusBot.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LocusPocusBot
{
    class Program
    {
        /// <summary>
        /// Holds the <see cref="CancellationToken"/> used
        /// for shutting down the WebHost
        /// </summary>
        private static CancellationTokenSource shutdownTokenSource =
            new CancellationTokenSource();

        public static IHost Host { get; }

        static Program()
        {
            Host = new HostBuilder()
                    .ConfigureAppConfiguration(ConfigureApp)
                    .ConfigureServices(ConfigureServices)
                    .ConfigureLogging(ConfigureLogging)
                    .UseConsoleLifetime()
                    .Build();
        }

        static async Task Main(string[] args)
        {
            // Get the logger
            ILogger logger = Host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Running database migrations...");

                using (BotContext db = Host.Services.GetRequiredService<BotContext>())
                {
                    db.Database.Migrate();
                }

                logger.LogInformation("Done");

                await Host.RunAsync(shutdownTokenSource.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");

                // Stop the WebHost, otherwise it might hang here
                shutdownTokenSource.Cancel();

                // Required to stop all the threads.
                // With "return 1", the process could actually stay online forever
                Environment.Exit(1);
            }
        }

        private static void ConfigureApp(HostBuilderContext hostContext, IConfigurationBuilder configApp)
        {
            // Load the application settings
            configApp.SetBasePath(Directory.GetCurrentDirectory());
            configApp.AddJsonFile("appsettings.json");
        }

        private static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
            logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
            logging.AddCustomConsole();
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            // TODO CORE 3.0: Change after https://github.com/aspnet/Hosting/issues/1346 is released
            services.Configure<ConsoleLifetimeOptions>(console => console.SuppressStatusMessages = true);

            services.UseConfigurationValidation();
            services.ConfigureValidatableSetting<BotConfiguration>(hostContext.Configuration.GetSection("Bot"));
            services.ConfigureValidatableSetting<DatabaseConfiguration>(hostContext.Configuration.GetSection("Database"));
            
            services.AddDbContext<BotContext>();

            // This also registers the service as a transient service
            services.AddHttpClient<IRoomsService, RoomsService>();

            services.AddSingleton<IBotService, BotService>();
            services.AddScoped<IUpdateProcessor, UpdateProcessor>();
            services.AddHandlers();

            services.AddSingleton(new Department[]
            {
                new Department("E0503", "Povo", "povo"),
                new Department("E0301", "Mesiano", "mesiano"),
                new Department("E0705", "Psicologia", "psicologia")
            });

            services.AddHostedService<BotHostedService>();
            services.AddHostedService<FetchSchedulerHostedService>();
        }
    }
}
