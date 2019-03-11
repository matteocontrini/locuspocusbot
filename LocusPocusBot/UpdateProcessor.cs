using LocusPocusBot.Data;
using LocusPocusBot.Handlers;
using LocusPocusBot.Rooms;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly BotContext db;

        public UpdateProcessor(ILogger<UpdateProcessor> logger,
                               IHandlersFactory handlersFactory,
                               IBotService botService,
                               BotContext context)
        {
            this.logger = logger;
            this.handlersFactory = handlersFactory;
            this.bot = botService;
            this.db = context;
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
                await LogUser(update.CallbackQuery.From);

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

            string[] data = callbackQuery.Data.Split(';');

            // It doesn't make much sense but it must be kept for backwards compatiblity
            if (data[0] != "free")
            {
                return Task.CompletedTask;
            }

            Department dep;
            if (data[1] == "povo")
            {
                dep = Department.Povo;
            }
            else if (data[1] == "mesiano")
            {
                dep = Department.Mesiano;
            }
            else if (data[1] == "psicologia")
            {
                dep = Department.Psicologia;
            }
            else
            {
                return Task.CompletedTask;
            }

            AvailabilityType type;
            if (data[2] == "now")
            {
                type = AvailabilityType.Free;
            }
            else if (data[2] == "future")
            {
                type = AvailabilityType.Occupied;
            }
            else if (data[2] == "all")
            {
                type = AvailabilityType.Any;
            }
            else
            {
                return Task.CompletedTask;
            }

            return HandleRoomRequest(callbackQuery.Message, dep, type, callbackQuery);
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
            else if (t.Contains("povo", StringComparison.OrdinalIgnoreCase))
            {
                await HandleRoomRequest(message, Department.Povo, AvailabilityType.Free);
            }
            else if (t.Contains("mesiano", StringComparison.OrdinalIgnoreCase))
            {
                await HandleRoomRequest(message, Department.Mesiano, AvailabilityType.Free);
            }else if (t.Contains("psicologia", StringComparison.OrdinalIgnoreCase))
            {
                await HandleRoomRequest(message, Department.Psicologia, AvailabilityType.Free);
            }
            else
            {
                HelpHandler handler = this.handlersFactory.GetHandler<HelpHandler>();
                handler.Chat = message.Chat;
                await handler.Run();
            }
        }

        private async Task HandleRoomRequest(
            Message message,
            Department dep,
            AvailabilityType type,
            CallbackQuery query = null)
        {
            long chatId = message?.Chat?.Id ?? query.Message.Chat.Id;

            await IncrementDepartmentUsage(chatId, dep);

            RoomsHandler handler = this.handlersFactory.GetHandler<RoomsHandler>();
            handler.Chat = message.Chat;
            handler.CallbackQuery = query;
            handler.RequestedDepartment = dep;
            handler.RequestedGroup = type;
            await handler.Run();
        }

        private async Task LogUser(User user)
        {
            ChatEntity existing = this.db.Chats.Find((long)user.Id);

            if (existing == null)
            {
                ChatEntity entity = new ChatEntity()
                {
                    Id = user.Id,
                    Type = ChatType.Private.ToString(),
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UpdatedAt = DateTime.UtcNow
                };

                this.db.Add(entity);
            }
            else
            {
                existing.Username = user.Username;
                existing.FirstName = user.FirstName;
                existing.LastName = user.LastName;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await this.db.SaveChangesAsync();
        }

        private async Task LogChat(Chat chat)
        {
            ChatEntity existing = this.db.Chats.Find(chat.Id);

            if (existing == null)
            {
                ChatEntity entity = new ChatEntity()
                {
                    Id = chat.Id,
                    Type = chat.Type.ToString(),
                    Title = chat.Title,
                    Username = chat.Username,
                    FirstName = chat.FirstName,
                    LastName = chat.LastName,
                    UpdatedAt = DateTime.UtcNow
                };

                this.db.Add(entity);
            }
            else
            {
                existing.Type = chat.Type.ToString();
                existing.Title = chat.Title;
                existing.Username = chat.Username;
                existing.FirstName = chat.FirstName;
                existing.LastName = chat.LastName;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await this.db.SaveChangesAsync();
        }

        private Task IncrementDepartmentUsage(long chatId, Department dep)
        {
            ChatEntity chat = this.db.Chats.Find(chatId);

            if (dep == Department.Povo)
            {
                chat.PovoCount++;
            }
            else if (dep == Department.Mesiano)
            {
                chat.MesianoCount++;
            }
            else
            {
                return Task.CompletedTask;
            }

            return this.db.SaveChangesAsync();
        }
    }
}
