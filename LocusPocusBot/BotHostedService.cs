using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;

namespace LocusPocusBot
{
    public class BotHostedService : IHostedService
    {
        // Cancellation token source used for shutting down long polling
        private CancellationTokenSource tokenSource;

        private readonly ILogger<BotHostedService> logger;
        private readonly IBotService bot;
        private readonly IServiceProvider serviceProvider;

        public BotHostedService(ILogger<BotHostedService> logger,
                                IServiceProvider serviceProvider,
                                IBotService botService)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.bot = botService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Get information about the bot associated with the token
            this.bot.Me = await this.bot.Client.GetMeAsync();

            this.logger.LogInformation($"Running as @{this.bot.Me.Username}");

            // Register event handlers
            this.bot.Client.OnUpdate += OnUpdate;
            this.bot.Client.OnReceiveError += OnReceiveError;
            this.bot.Client.OnReceiveGeneralError += OnReceiveGeneralError;

            // Create a new token to be passed to .StartReceiving() below.
            // When the token is canceled, the tg client stops receiving
            this.tokenSource = new CancellationTokenSource();

            // Start getting messages
            this.bot.Client.StartReceiving(cancellationToken: this.tokenSource.Token);
        }

        private async void OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            // Create a scope for the update that is about to be processed
            using (var scope = this.serviceProvider.CreateScope())
            {
                // Get an IUpdateProcessor instance
                var updateService = scope.ServiceProvider.GetRequiredService<IUpdateProcessor>();

                try
                {
                    // Process the update
                    await updateService.ProcessUpdate(e.Update);
                }
                catch (InvalidQueryIdException)
                {
                    // ignore
                }
                catch (MessageIsNotModifiedException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Exception [{Message}] while handling update {Update}",
                        ex.Message.Replace('\n', '|'), // keep the message on one line
                        e.Update.ToJson()
                    );
                }
            }
        }

        private void OnReceiveGeneralError(object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
        {
            this.logger.LogError(e.Exception, "OnReceiveGeneralError");
        }

        private void OnReceiveError(object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
            this.logger.LogError(e.ApiRequestException, "OnReceiveError");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.tokenSource.Cancel();

            return Task.CompletedTask;
        }
    }
}
