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
        public string Id { get; }

        /// <summary>
        /// Name of the department
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Short name of the department, without spaces, capital letters, etc.
        /// </summary>
        public string Slug { get; }

        /// <summary>
        /// List of rooms in the department
        /// </summary>
        public List<Room> Rooms { get; set; }

        /// <summary>
        /// Date of the cached data
        /// </summary>
        public string UpdatedAt { get; set; }

        public Department(string id, string name, string slug)
        {
            this.Id = id;
            this.Name = name;
            this.Slug = slug;
        }

        public static Department Povo { get; } = new Department("E0503", "Povo", "povo");

        public static Department Mesiano { get; } = new Department("E0301", "Mesiano", "mesiano");

        public AvailabilityGroup[] FindFreeRoomsAt(Instant instant)
        {
            AvailabilityGroup[] groups = new AvailabilityGroup[]
            {
                new AvailabilityGroup(AvailabilityType.Free),
                new AvailabilityGroup(AvailabilityType.Occupied),
                new AvailabilityGroup(AvailabilityType.Any)
            };

            foreach (Room room in this.Rooms)
            {
                // At the end of a long journey, this interval will contain
                // the start/end instants when the room is free, based on the input time.
                // By default, a room is free all day
                Interval interval = new Interval(start: null, end: null);

                // No lectures today in this room
                if (room.Lectures.Count == 0)
                {
                    // The room is free all-day
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
                            // We're at "current lecture", we need to loop through the next lectures,
                            // until we find a FREE slot
                            // [ ... ] [ current lecture ] [ FREE ] [ optional lecture ]
                            // [ ... ] [ current lecture ] [ .... ] [ FREE ] [ optional lecture ]
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
                RoomAvailability r = new RoomAvailability(room, interval, isFreeNow);

                if (isFreeNow)
                {
                    groups[0].Rooms.Add(r);
                }
                else
                {
                    groups[1].Rooms.Add(r);
                }

                groups[2].Rooms.Add(r);
            } // rooms loop

            // Sort free rooms
            groups[0].Rooms.Sort((x, y) =>
            {
                // Sort rooms by name if free until the same time
                if ((!x.FreeInterval.HasEnd && !y.FreeInterval.HasEnd) ||
                    (x.FreeInterval.HasEnd && y.FreeInterval.HasEnd &&
                     x.FreeInterval.End == y.FreeInterval.End))
                {
                    return string.Compare(x.Name, y.Name);
                }

                // The room x is free until the end of the day,
                // return that x less than y
                if (!x.FreeInterval.HasEnd)
                {
                    return -1;
                }

                // The room y is free until the end of the day,
                // return that x is more than y
                if (!y.FreeInterval.HasEnd)
                {
                    return 1;
                }

                // Put before rooms that will be available for more time
                return y.FreeInterval.End.CompareTo(x.FreeInterval.End);
            });

            // Sort occupied rooms
            groups[1].Rooms.Sort((x, y) =>
            {
                // Occupied rooms will be free at some time T in the future.
                // Sort them by T
                return x.FreeInterval.Start.CompareTo(y.FreeInterval.Start);
            });

            // Sort all rooms by name
            groups[2].Rooms.Sort((x, y) =>
            {
                return x.Name.CompareTo(y.Name);
            });

            return groups;
        }
    }
}
