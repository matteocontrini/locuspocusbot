using NodaTime;
using NodaTime.Text;
using System.Text;

namespace LocusPocusBot.Rooms
{
    /// <summary>
    /// Holds a room with the associated
    /// </summary>
    public class RoomAvailability
    {
        private Room room;

        public string Name
        {
            get
            {
                return room.Name;
            }
        }

        /// <summary>
        /// Interval during which the room is free
        /// </summary>
        public Interval FreeInterval { get; set; }

        public RoomAvailability(Room room, Interval freeInterval)
        {
            this.room = room;
            this.FreeInterval = freeInterval;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append("[");
            s.Append(this.Name);
            s.Append("] ");

            LocalTimePattern pattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss");

            if (this.FreeInterval.HasStart)
            {
                s.Append(pattern.Format(this.FreeInterval.Start.InUtc().TimeOfDay));
                s.Append(" - ");
            }
            else
            {
                s.Append("SoD - ");
            }

            if (this.FreeInterval.HasEnd)
            {
                s.Append(pattern.Format(this.FreeInterval.End.InUtc().TimeOfDay));
            }
            else
            {
                s.Append("EoD");
            }

            return s.ToString();
        }
    }
}
