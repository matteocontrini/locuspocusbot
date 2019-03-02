using LocusPocusBot.Handlers;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LocusPocusBot
{
    public class UpdateProcessor : IUpdateProcessor
    {
        private readonly ILogger logger;
        private readonly IHandlersFactory handlersFactory;
        private readonly IBotService bot;

        public UpdateProcessor(ILogger<UpdateProcessor> logger,
                               IHandlersFactory handlersFactory,
                               IBotService botService)
        {
            this.logger = logger;
            this.handlersFactory = handlersFactory;
            this.bot = botService;
        }

        public async Task ProcessUpdate(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                await LogChat(update.Message.Chat);

                await HandleMessage(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandlerCallbackQuery(update.CallbackQuery);
            }
        }

        private async Task HandleMessage(Message message)
        {
            if (message.Type == MessageType.Text)
            {
                this.logger.LogInformation("TXT <{0}> {1}", message.Chat.Id, message.Text);

                await HandleTextMessage(message);
            }
            else if (message.Type == MessageType.GroupCreated)
            {
                StartHandler handler = this.handlersFactory.GetHandler<StartHandler>();
                handler.Chat = message.Chat;
                await handler.Run();
            }
            else if (message.Type == MessageType.ChatMembersAdded)
            {
                if (message.NewChatMembers.FirstOrDefault(x => x.Username == this.bot.Me.Username) != null)
                {
                    StartHandler handler = this.handlersFactory.GetHandler<StartHandler>();
                    handler.Chat = message.Chat;
                    await handler.Run();
                }
            }
            else if (message.Type == MessageType.MigratedToSupergroup)
            {
                // TODO: migrate chat ID
            }
            else if (message.Type == MessageType.Text)
            {
                // Remove the bot mention in groups
                if (message.Chat.Type == ChatType.Group ||
                    message.Chat.Type == ChatType.Supergroup)
                {
                    message.Text = message.Text.Replace($"@{this.bot.Me.Username}", "");
                }

                await HandleTextMessage(message);
            }
        }

        private Task HandlerCallbackQuery(CallbackQuery callbackQuery)
        {
            this.logger.LogInformation("CB <{0}> {1}", callbackQuery.Message.Chat.Id, callbackQuery.Data);

            return Task.CompletedTask;
            //RoomsHandler handler = this.handlersFactory.GetHandler<RoomsHandler>();
            //handler.Chat = callbackQuery.Message.Chat;
            //handler.CallbackQuery = callbackQuery;
            //return handler.Run();
        }

        private async Task HandleTextMessage(Message message)
        {
            string t = message.Text;

            if (t == "/start" || t.StartsWith("/start "))
            {
                StartHandler handler = this.handlersFactory.GetHandler<StartHandler>();
                handler.Chat = message.Chat;
                await handler.Run();
            }
            else if (t == "/aiuto")
            {
                HelpHandler handler = this.handlersFactory.GetHandler<HelpHandler>();
                handler.Chat = message.Chat;
                await handler.Run();
            }
            else
            {
                //RoomsHandler handler = this.handlersFactory.GetHandler<RoomsHandler>();
                //handler.Chat = callbackQuery.Message.Chat;
                //handler.CallbackQuery = callbackQuery;
                //return handler.Run();
            }
        }

        private async Task LogChat(Chat chat)
        {
            //ChatEntity chatEntity = new ChatEntity()
            //{
            //    ChatId = chat.Id,
            //    Type = chat.Type.ToString(),
            //    Title = chat.Title,
            //    Username = chat.Username,
            //    FirstName = chat.FirstName,
            //    LastName = chat.LastName,
            //    UpdatedAt = DateTime.UtcNow
            //};

            //await this.chatRepository.InsertOrReplaceChat(chatEntity);
        }
    }
}
