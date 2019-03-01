using NodaTime;

namespace LocusPocusBot.Rooms
{
    public class Lecture
    {
        public string Name { get; set; }

        public Instant StartInstant { get; set; }

        public Instant EndInstant { get; set; }

        public Lecture(string name, long startTimestamp, long endTimestamp)
        {
            this.Name = name;
            this.StartInstant = Instant.FromUnixTimeSeconds(startTimestamp);
            this.EndInstant = Instant.FromUnixTimeSeconds(endTimestamp);
        }
    }
}
