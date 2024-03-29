﻿using LocusPocusBot.Data;
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
        private readonly Department[] departments;
        private readonly IDatabaseService db;

        public UpdateProcessor(ILogger<UpdateProcessor> logger,
            IHandlersFactory handlersFactory,
            IBotService botService,
            IDatabaseService db,
            Department[] departments)
        {
            this.logger = logger;
            this.handlersFactory = handlersFactory;
            this.bot = botService;
            this.db = db;
            this.departments = departments;
        }

        public async Task ProcessUpdate(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                await this.db.LogChat(update.Message.Chat);

                await HandleMessage(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await this.db.LogUser(update.CallbackQuery.From);

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

            Department dep = this.departments.First(x => x.Slug == data[1]);

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
            else
            {
                Department dep = this.departments.FirstOrDefault(
                    x => t.Contains(x.Slug, StringComparison.OrdinalIgnoreCase)
                );

                if (dep != null)
                {
                    await HandleRoomRequest(message, dep, AvailabilityType.Free);
                }
                else
                {
                    HelpHandler handler = this.handlersFactory.GetHandler<HelpHandler>();
                    handler.Chat = message.Chat;
                    await handler.Run();
                }
            }
        }

        private async Task HandleRoomRequest(
            Message message,
            Department dep,
            AvailabilityType type,
            CallbackQuery query = null)
        {
            long chatId = message?.Chat.Id ?? query.Message.Chat.Id;
            RequestType requestType = query != null ? RequestType.CallbackQuery : RequestType.Message;

            await this.db.LogUsage(chatId, requestType, type, dep);

            RoomsHandler handler = this.handlersFactory.GetHandler<RoomsHandler>();
            handler.Chat = message.Chat;
            handler.CallbackQuery = query;
            handler.RequestedDepartment = dep;
            handler.RequestedGroup = type;
            await handler.Run();
        }
    }
}
