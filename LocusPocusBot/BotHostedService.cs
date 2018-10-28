using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LocusPocusBot
{
    public class BotHostedService : IHostedService
    {
        /// <summary>
        /// Cancellation token source used for shutting down long polling
        /// </summary>
        /// 
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// The Telegram client for managing the bot
        /// </summary>
        private TelegramBotClient bot;

        /// <summary>
        /// Information about the bot
        /// </summary>
        private User me;

        /// <summary>
        /// Configuration for the bot
        /// </summary>
        private readonly BotConfiguration config;

        /// <summary>
        /// Logger instance for this context
        /// </summary>
        private readonly ILogger<BotHostedService> logger;

        public BotHostedService(IOptions<BotConfiguration> options,
                                ILogger<BotHostedService> logger)
        {
            this.config = options.Value;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.bot = new TelegramBotClient(this.config.BotToken);

            // Get information about the bot associated with the token
            this.me = await this.bot.GetMeAsync();

            this.logger.LogInformation($"running as @{this.me.Username}");

            // Register event handlers
            this.bot.OnMessage += OnMessage;

            // Create a new token to be passed to .StartReceiving() below.
            // When the token is canceled, the tg client stops receiving
            this.tokenSource = new CancellationTokenSource();

            // Start getting messages
            this.bot.StartReceiving(cancellationToken: this.tokenSource.Token);
        }

        private void OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            // Echo
            this.bot.SendTextMessageAsync(e.Message.Chat.Id, e.Message.Text);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.tokenSource.Cancel();

            return Task.CompletedTask;
        }
    }
}
