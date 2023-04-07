using System;
using System.Threading.Tasks;
using LocusPocusBot.Rooms;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LocusPocusBot.Data
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IMongoCollection<ChatEntity> chats;
        private readonly IMongoCollection<LogEntity> logs;

        public DatabaseService(IOptions<DatabaseConfiguration> options)
        {
            MongoClient mongoClient = new MongoClient(options.Value.ConnectionString);
            IMongoDatabase database = mongoClient.GetDatabase("locuspocusbot");
            this.chats = database.GetCollection<ChatEntity>("chats");
            this.logs = database.GetCollection<LogEntity>("logs");
        }

        public async Task LogUser(User user)
        {
            bool exists = await this.chats.Find(
                Builders<ChatEntity>.Filter.Eq(c => c.ChatId, user.Id)
            ).AnyAsync();

            if (!exists)
            {
                ChatEntity entity = new ChatEntity()
                {
                    ChatId = user.Id,
                    Type = ChatType.Private.ToString(),
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UpdatedAt = DateTime.UtcNow
                };

                await this.chats.InsertOneAsync(entity);
            }
            else
            {
                await this.chats.UpdateOneAsync(
                    Builders<ChatEntity>.Filter.Eq(c => c.ChatId, user.Id),
                    Builders<ChatEntity>.Update
                        .Set(c => c.Username, user.Username)
                        .Set(c => c.FirstName, user.FirstName)
                        .Set(c => c.LastName, user.LastName)
                        .Set(c => c.UpdatedAt, DateTime.UtcNow)
                );
            }
        }

        public async Task LogChat(Chat chat)
        {
            bool exists = await this.chats.Find(
                Builders<ChatEntity>.Filter.Eq(c => c.ChatId, chat.Id)
            ).AnyAsync();

            if (!exists)
            {
                ChatEntity entity = new ChatEntity()
                {
                    ChatId = chat.Id,
                    Type = chat.Type.ToString(),
                    Title = chat.Title,
                    Username = chat.Username,
                    FirstName = chat.FirstName,
                    LastName = chat.LastName,
                    UpdatedAt = DateTime.UtcNow
                };

                await this.chats.InsertOneAsync(entity);
            }
            else
            {
                await this.chats.UpdateOneAsync(
                    Builders<ChatEntity>.Filter.Eq(c => c.ChatId, chat.Id),
                    Builders<ChatEntity>.Update
                        .Set(c => c.Type, chat.Type.ToString())
                        .Set(c => c.Title, chat.Title)
                        .Set(c => c.Username, chat.Username)
                        .Set(c => c.FirstName, chat.FirstName)
                        .Set(c => c.LastName, chat.LastName)
                        .Set(c => c.UpdatedAt, DateTime.UtcNow)
                );
            }
        }

        public Task LogUsage(
            long chatId,
            RequestType requestType,
            AvailabilityType availabilityType,
            Department dep)
        {
            return this.logs.InsertOneAsync(new LogEntity()
            {
                At = DateTime.UtcNow,
                ChatId = chatId,
                RequestType = requestType,
                AvailabilityType = availabilityType,
                Department = dep.Slug
            });
        }
    }
}
