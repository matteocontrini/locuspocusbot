using NodaTime;

namespace LocusPocusBot.Rooms
{
    /// <summary>
    /// Holds a room with the associated
    /// </summary>
    public class RoomAvailbility
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

        /// <summary>
        /// Whether the room is currently free
        /// </summary>
        public bool IsFreeNow { get; set; }

        public RoomAvailbility(Room room, Interval freeInterval, bool isFreeNow)
        {
            this.room = room;
            this.FreeInterval = freeInterval;
        }
    }
}
