using System;
using MongoDB.Bson;

namespace LocusPocusBot.Data
{
    public class ChatEntity
    {
        public ObjectId Id { get; set; }

        public long ChatId { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
