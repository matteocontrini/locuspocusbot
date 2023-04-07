using LocusPocusBot.Rooms;
using System;
using MongoDB.Bson;

namespace LocusPocusBot.Data
{
    public class LogEntity
    {
        public ObjectId Id { get; set; }

        public long ChatId { get; set; }

        public DateTime At { get; set; }

        public RequestType RequestType { get; set; }

        public string Department { get; set; }

        public AvailabilityType AvailabilityType { get; set; }
    }
}
