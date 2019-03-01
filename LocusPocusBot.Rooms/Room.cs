using System.Collections.Generic;

namespace LocusPocusBot.Rooms
{
    public class Room
    {
        public string Key { get; set; }

        public string Name { get; set; }

        public List<Lecture> Lectures { get; set; }

        public Room(string key, string name)
        {
            this.Key = key;
            this.Name = name;
            this.Lectures = new List<Lecture>();
        }
    }
}
