using Telegram.Bot;
using Telegram.Bot.Types;

namespace LocusPocusBot
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }

        User Me { get; set; }
    }
}
