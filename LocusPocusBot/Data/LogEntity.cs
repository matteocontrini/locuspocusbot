using LocusPocusBot.Rooms;
using System;
using System.ComponentModel.DataAnnotations;

namespace LocusPocusBot.Data
{
    public class LogEntity
    {
        public uint Id { get; set; }

        [Required]
        public ChatEntity Chat { get; set; }
        
        public DateTime At { get; set; }

        public RequestType RequestType { get; set; }

        [Required]
        public string Department { get; set; }

        public AvailabilityType AvailabilityType { get; set; }
    }
}
