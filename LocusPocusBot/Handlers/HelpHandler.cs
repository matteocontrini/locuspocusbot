using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace LocusPocusBot.Handlers
{
    public class HelpHandler : HandlerBase
    {
        private readonly IBotService bot;

        public HelpHandler(IBotService botService)
        {
            this.bot = botService;
        }

        public override async Task Run()
        {
            StringBuilder msg = new StringBuilder();
            
            msg.AppendLine("*LocusPocus* è il bot per controllare la disponibilità delle aule presso i poli di Mesiano e Povo (Polo Ferrari) dell'Università di Trento 🎓");
            msg.AppendLine();
            msg.AppendLine("*Scrivi* /povo *oppure* /mesiano *per ottenere la lista delle aule libere.*");
            msg.AppendLine();
            msg.AppendLine("Sviluppato da Matteo Contrini (@matteocontrini). Si ringraziano Alessandro Conti per il nome del bot e Dario Crisafulli per il logo.");
            msg.AppendLine();
            msg.AppendLine("Il bot è [open source](https://github.com/matteocontrini/locuspocusbot) 🤓");
            
            await this.bot.Client.SendTextMessageAsync(
                chatId: this.Chat.Id,
                text: msg.ToString(),
                parseMode: ParseMode.Markdown
            );
        }
    }
}
