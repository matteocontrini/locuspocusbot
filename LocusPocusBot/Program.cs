using LocusPocusBot.Rooms;
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

        static async Task<int> Main(string[] args)
        {
            // Build the host
            IHost host = new HostBuilder()
                .ConfigureAppConfiguration(ConfigureApp)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(ConfigureLogging)
                .UseConsoleLifetime()
                .Build();

            // Get the logger
            ILogger logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                await host.RunAsync(shutdownTokenSource.Token);

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");

                // Stop the Host
                shutdownTokenSource.Cancel();

                return 1;
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
            logging.AddConsole();
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.Configure<BotConfiguration>(hostContext.Configuration.GetSection("Bot"));

            services.AddTransient<IRoomsService, RoomsService>();

            services.AddHostedService<BotHostedService>();
            services.AddHostedService<FetchSchedulerHostedService>();
        }
    }
}
