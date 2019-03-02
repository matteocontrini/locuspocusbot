using System;
using System.Net;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LocusPocusBot
{
    public class BotService : IBotService
    {
        public TelegramBotClient Client { get; }
        public User Me { get; set; }

        public BotService(BotConfiguration config)
        {
            HttpClient client = new HttpClient(new SocketsHttpHandler()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(60),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            this.Client = new TelegramBotClient(config.BotToken, client);
        }
    }
}
