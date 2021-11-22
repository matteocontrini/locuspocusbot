using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

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

            // Create a new token to be passed to .StartReceiving() below.
            // When the token is canceled, the tg client stops receiving
            this.tokenSource = new CancellationTokenSource();

            // Start getting messages
            this.bot.Client.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions(),
                cancellationToken: this.tokenSource.Token);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            // Create a scope for the update that is about to be processed
            using (var scope = this.serviceProvider.CreateScope())
            {
                // Get an IUpdateProcessor instance
                var updateService = scope.ServiceProvider.GetRequiredService<IUpdateProcessor>();

                try
                {
                    // Process the update
                    await updateService.ProcessUpdate(update);
                }
                catch (ApiRequestException exception) when (exception.Message.Contains("query ID is invalid")
                                                            || exception.Message.Contains("message is not modified"))
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Exception [{Message}] while handling update {Update}",
                        ex.Message.Replace('\n', '|'), // keep the message on one line
                        update.ToJson()
                    );
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            this.logger.LogError(exception, "Polling error");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.tokenSource.Cancel();

            return Task.CompletedTask;
        }
    }
}
