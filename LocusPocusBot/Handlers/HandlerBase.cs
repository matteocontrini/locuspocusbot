using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace LocusPocusBot.Handlers
{
    public abstract class HandlerBase
    {
        /// <summary>
        /// Telegram <see cref="Chat"/> invoking the handler
        /// </summary>
        public Chat Chat { get; set; }

        /// <summary>
        /// <see cref="CallbackQuery"/> if the handler was invoked through an inline button
        /// </summary>
        public CallbackQuery CallbackQuery { get; set; }

        public abstract Task Run();
    }
}
