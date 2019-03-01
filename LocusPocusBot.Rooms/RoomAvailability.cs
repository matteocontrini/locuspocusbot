using NodaTime;

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
    }
}
