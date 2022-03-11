using NodaTime;

namespace LocusPocusBot.Rooms
{
    public class Lecture
    {
        public string Name { get; set; }

        public Instant StartInstant { get; set; }

        public Instant EndInstant { get; set; }

        public Lecture(string name, Instant startInstant, Instant endInstant)
        {
            this.Name = name;
            this.StartInstant = startInstant;
            this.EndInstant = endInstant;
        }
    }
}
