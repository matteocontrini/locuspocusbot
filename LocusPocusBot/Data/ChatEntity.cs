using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocusPocusBot.Data
{
    public class ChatEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // disables autoinc
        public long Id { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime UpdatedAt { get; set; }

        public uint PovoCount { get; set; }

        public uint MesianoCount { get; set; }
    }
}
