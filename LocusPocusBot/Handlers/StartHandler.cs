using LocusPocusBot.Rooms;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace LocusPocusBot.Handlers
{
    public class StartHandler : HandlerBase
    {
        private readonly IBotService bot;
        private readonly Department[] departments;

        public StartHandler(IBotService botService,
                            Department[] departments)
        {
            this.bot = botService;
            this.departments = departments;
        }

        public override async Task Run()
        {
            StringBuilder msg = new StringBuilder();
            
            msg.AppendLine("Ciao! 🤓");
            msg.AppendLine();
            msg.AppendLine("Sono *LocusPocus* e ti posso aiutare a trovare le aule libere presso i poli dell'Università di Trento 🎓");
            msg.AppendLine();
            msg.AppendLine("Usa uno di questi comandi per ottenere la lista delle aule libere:");
            msg.AppendLine();

            foreach (Department dep in this.departments)
            {
                msg.Append('/');
                msg.AppendLine(dep.Slug);
            }

            msg.AppendLine();
            msg.AppendLine("Altre info in /aiuto");

            await this.bot.Client.SendTextMessageAsync(
                chatId: this.Chat.Id,
                text: msg.ToString(),
                parseMode: ParseMode.Markdown
            );
        }
    }
}
