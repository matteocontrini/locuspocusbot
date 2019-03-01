using NodaTime;
using System.Collections.Generic;
using System.Linq;

namespace LocusPocusBot.Rooms
{
    public class Department
    {
        /// <summary>
        /// EasyRoom ID of the department
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Name of the department
        /// </summary>
        public string Name { get; private set;  }

        /// <summary>
        /// List of rooms in the department
        /// </summary>
        public List<Room> Rooms { get; set; }

        public Department(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public static Department Povo { get; } = new Department("E0503", "Povo");

        public List<RoomAvailbility> FindFreeRoomsAt(Instant instant)
        {
            List<RoomAvailbility> freeRooms = new List<RoomAvailbility>();

            foreach (Room room in this.Rooms)
            {
                // At the end of a long journey, this interval will contain
                // the start/end instants when the room is free, based on the input time
                Interval interval = new Interval();

                // No lectures today in this room
                if (room.Lectures.Count == 0)
                {
                    // The room is free all-day.
                    // Create an interval with undefined from/to
                    interval = new Interval();
                }
                // The first lecture still hasn't started
                else if (instant < room.Lectures[0].StartInstant)
                {
                    // The room will be free until the start of the first lecture
                    // [ start of the day ] [ FREE ] [ first lecture ]
                    //                        ^^^^
                    interval = new Interval(
                        start: null,
                        end: room.Lectures[0].StartInstant
                    );
                }
                // The last lecture is already finished
                else if (instant > room.Lectures.Last().EndInstant)
                {
                    // The room is free until the end of the day
                    // [ last lecture ] [ FREE ] [ end of the day ]
                    //                   ^^^^
                    interval = new Interval(
                        start: room.Lectures.Last().EndInstant,
                        end: null
                    );
                }
                // Otherwise, we need to loop through the lectures for the day,
                // in order to find the lectures around the "current time"
                else
                {
                    for (int i = 0; i < room.Lectures.Count; i++)
                    {
                        Lecture lecture = room.Lectures[i];
                        Lecture previousLecture = room.Lectures.ElementAtOrDefault(i - 1);

                        // The lecture is currently being held.
                        // NOTE: don't chek equality at EndInstant, because the room
                        // could be free at EndInstant (e.g. at 12:30), if there are no lectures after it
                        if (instant >= lecture.StartInstant && instant < lecture.EndInstant)
                        {
                            // We're at CURRENT, we need to loop through the next lectures,
                            // until we find a FREE slot
                            // [ ... ] [ current lecture ] [ FREE ] [ lecture ]
                            // [ ... ] [ current lecture ] [ .... ] [ FREE ] [ lecture ]
                            for (int j = i + 1; j < room.Lectures.Count; j++)
                            {
                                // If this condition is true, we've found a gap between two lectures,
                                // which means we've a FREE slot
                                if (room.Lectures[j].StartInstant != room.Lectures[j - 1].EndInstant)
                                {
                                    // So the room is free during the gap
                                    interval = new Interval(
                                        start: room.Lectures[j - 1].EndInstant,
                                        end: room.Lectures[j].StartInstant
                                    );

                                    // Stop looking for gaps
                                    break;
                                }
                            }

                            // We've arrived at the end of the lectures for the day.
                            // The room will be free from that moment until the end of the day
                            if (!interval.HasStart)
                            {
                                // [ last lecture ] [ FREE ] [ end of the day ]
                                //                   ^^^^
                                interval = new Interval(
                                    start: room.Lectures.Last().EndInstant,
                                    end: null
                                );
                            }

                            // Stop looping through lectures
                            break;
                        }
                        // If we're in between two lectures...
                        else if (previousLecture != null &&
                                 instant >= previousLecture.EndInstant &&
                                 instant < lecture.StartInstant)
                        {
                            // The room will be free until the start of the next lecture
                            // [ previousLecture ] [ FREE ] [ lecture ]
                            //                       ^^^^
                            interval = new Interval(
                                start: previousLecture.EndInstant,
                                end: lecture.StartInstant
                            );

                            // Stop looping through lectures
                            break;
                        }
                    } // lectures loop
                } // room else

                bool isFreeNow = interval.Contains(instant);

                freeRooms.Add(new RoomAvailbility(room, interval, isFreeNow));
            } // rooms loop

            return freeRooms;
        }
    }
}
