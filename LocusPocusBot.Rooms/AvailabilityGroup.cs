using System.Collections.Generic;

namespace LocusPocusBot.Rooms
{
    public class AvailabilityGroup
    {
        public AvailabilityType Availability { get; set; }

        public List<RoomAvailability> Rooms { get; set; }

        public AvailabilityGroup(AvailabilityType availabilityType)
        {
            this.Availability = availabilityType;
            this.Rooms = new List<RoomAvailability>();
        }
    }
}
