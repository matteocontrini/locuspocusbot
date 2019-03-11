using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LocusPocusBot.Rooms
{
    public class RoomsService : IRoomsService
    {
        private readonly string easyRoomUrl = "https://easyroom.unitn.it/Orario/rooms_call.php";
        private readonly HttpClient client;

        public RoomsService(HttpClient client)
        {
            this.client = client;
        }

        public async Task<List<Room>> LoadRooms(Department department)
        {
            // Take the current date for Italy's timezone
            DateTimeZone zone = DateTimeZoneProviders.Tzdb["Europe/Rome"];
            LocalDate now = SystemClock.Instance.InZone(zone).GetCurrentDate();

            // Format date like 13-10-2017
            LocalDatePattern pattern = LocalDatePattern.CreateWithInvariantCulture("dd-MM-yyyy");
            string dateString = pattern.Format(now);

            // Clear cached data of the previous day
            if (department.UpdatedAt != dateString)
            {
                department.Rooms = null;
                department.UpdatedAt = dateString;
            }

            // Prepare the request payload
            var form = new Dictionary<string, string>
            {
                { "form-type", "rooms" },
                { "sede", department.Id },
                { "date", dateString },
                { "_lang", "it" }
            };

            // Post x-www-form-urlencoded data
            HttpResponseMessage response =
                await this.client.PostAsync(this.easyRoomUrl, new FormUrlEncodedContent(form));

            // Read the response body
            string body = await response.Content.ReadAsStringAsync();

            // Parse the JSON object
            JObject payload = JObject.Parse(body);

            List<Room> rooms = ParseRooms(payload, department);
            ParseLectures(payload, rooms);

            return rooms;
        }

        private static List<Room> ParseRooms(JObject payload, Department department)
        {
            // Get the dictionary of rooms for the department.
            // This API is so weird. You ask information for a department,
            // and it returns all the departments anyway
            JObject roomsDic = (JObject)payload.SelectToken($"area_rooms.{department.Id}");

            // Create a list for rooms
            List<Room> rooms = new List<Room>();

            foreach (KeyValuePair<string, JToken> item in roomsDic)
            {
                string roomName = item.Value["room_name"].ToString();

                if (department == Department.Povo)
                {
                    // Keep only the name of the room, like "B107"
                    // Strips parentheses, bla...
                    Match match = Regex.Match(roomName, "[A-B]{1}[0-9]{3}");

                    if (match.Success)
                    {
                        roomName = match.Value;
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (department == Department.Mesiano)
                {
                    if (roomName.StartsWith("Aula "))
                    {
                        roomName = roomName.Substring(roomName.IndexOf(' ') + 1);
                    }
                    else if (roomName.StartsWith("Biblioteca"))
                    {
                        roomName = "Biblioteca";
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (department == Department.Psicologia)
                {
                    if (roomName.StartsWith("Laboratorio informatico"))
                    {
                        roomName = "LAB" + roomName.Substring(24);
                    }
                    else if (roomName == "Aula Magna")
                    {
                        roomName = "Magna";
                    }
                    else {
                        // Keep only the name of the room, like "1" or "11"
                        // Strips floor, etc...
                        Match match = Regex.Match(roomName, "[0-9]{1,2}");

                        if (match.Success)
                        {
                            roomName = match.Value;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                Room room = new Room(item.Key, roomName);
                rooms.Add(room);
            }

            return rooms;
        }

        private void ParseLectures(JObject payload, List<Room> rooms)
        {
            JArray eventsArray = (JArray)payload.SelectToken("events");

            // Loop through all the lectures
            // (they could be in random order)
            foreach (JToken item in eventsArray)
            {
                Lecture lec = new Lecture(
                    name: item["name"].ToString(),
                    startTimestamp: item["timestamp_from"].ToObject<long>(),
                    endTimestamp: item["timestamp_to"].ToObject<long>()
                );

                string roomId = item["CodiceAula"].ToString();

                // Assign the parsed lecture to the room
                Room r = rooms.Find(x => x.Key == roomId);

                if (r != null)
                {
                    r.Lectures.Add(lec);
                }
            }
        }
    }
}
