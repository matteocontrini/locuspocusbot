using LocusPocusBot.Rooms;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LocusPocusBot.Handlers
{
    public class HelpHandler : HandlerBase
    {
        private readonly IBotService bot;
        private readonly Department[] departments;

        public HelpHandler(IBotService botService,
                           Department[] departments)
        {
            this.bot = botService;
            this.departments = departments;
        }

        public override async Task Run()
        {
            StringBuilder msg = new StringBuilder();

            msg.AppendLine("*LocusPocus* è il bot per controllare la disponibilità delle aule presso i poli dell'Università di Trento 🎓");
            msg.AppendLine();
            msg.AppendLine("👉 *Usa uno di questi comandi per ottenere la lista delle aule libere*");
            msg.AppendLine();

            foreach (Department dep in this.departments)
            {
                msg.Append('/');
                msg.AppendLine(dep.Slug);
            }

            msg.AppendLine();
            msg.AppendLine("🤫 Il bot è sviluppato da Matteo Contrini (@matteosonoio) con la collaborazione di Emilio Molinari");
            msg.AppendLine();
            msg.AppendLine("👏 Un grazie speciale a Alessandro Conti per il nome del bot e a [Dario Crisafulli](https://botfactory.it/#chisiamo) per il logo!");
            msg.AppendLine();
            msg.AppendLine("🤓 Il bot è [open source](https://github.com/matteocontrini/locuspocusbot)");

            await this.bot.Client.SendTextMessageAsync(
                chatId: this.Chat.Id,
                text: msg.ToString(),
                parseMode: ParseMode.Markdown,
                disableWebPagePreview: true
            );
        }
    }
}
