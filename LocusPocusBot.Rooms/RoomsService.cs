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
        private readonly string easyRoomUrl = "https://easyacademy.unitn.it/AgendaStudentiUnitn/rooms_call.php";
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

                if (department.Slug == "povo")
                {
                    // Keep only the name of the room, like "B107"
                    // Strips parentheses, bla...
                    Match match = Regex.Match(roomName, "[AB]{1}[0-9]{3}");

                    if (match.Success)
                    {
                        roomName = match.Value;
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (department.Slug == "mesiano")
                {
                    if (roomName.StartsWith("Aula "))
                    {
                        roomName = roomName.Substring(5);

                        if (roomName == "1R" || roomName == "2R")
                        {
                            continue;
                        }
                    }
                    else if (roomName.StartsWith("Biblioteca"))
                    {
                        roomName = "Biblioteca";
                    }
                    // Keep EALAB, skip everything else
                    else if (roomName != "EALAB")
                    {
                        continue;
                    }
                }
                else if (department.Slug == "psicologia")
                {
                    Match match = Regex.Match(roomName, "^Aula ([0-9]{1,2}|Magna)");

                    if (match.Success)
                    {
                        // Keep only the name of the room, like "1", "11" or "Magna"
                        // Strips floor, etc...
                        roomName = match.Groups[1].Value;
                    }
                    else if (roomName.StartsWith("Laboratorio informatico "))
                    {
                        roomName = "Lab " + roomName.Substring(24);
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (department.Slug == "sociologia")
                {
                    string[] roomNameParts = roomName.Split(' ');

                    if (roomNameParts[0] == "Aula")
                    {
                        if (roomNameParts[1] == "Kessler")
                        {
                            continue;
                        }
                        else
                        {
                            roomName = roomNameParts[1];
                        }
                    }
                    else if (roomNameParts[0] == "Laboratorio")
                    {
                        roomName = "Lab " + roomNameParts[1];
                    }
                    else if (roomNameParts[0] == "Sala")
                    {
                        if (roomNameParts[1] == "Studio" || roomNameParts[1] == "Gruppi")
                        {
                            roomName = roomName.Substring(5);
                        }
                        else if (roomNameParts[1] == "archeologica")
                        {
                            roomName = "Archeologica";
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (department.Slug == "lettere")
                {
                    Match match = Regex.Match(roomName, "^Aula ([0-9]{1,3})");

                    if (match.Success)
                    {
                        // Keep only the name of the room, like "001"
                        // Strips everything else
                        roomName = match.Groups[1].Value;
                    }
                    else if (roomName.StartsWith("Laboratorio m"))
                    {
                        roomName = "Lab " + roomName[25];
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (department.Slug == "economia")
                {
                    if (roomName.StartsWith("Aula informatica "))
                    {
                        roomName = "Inf " + roomName.Substring(17).Replace(" - ", " ").ToUpper();
                    }
                    else if (roomName.StartsWith("Aula "))
                    {
                        roomName = roomName.Substring(5);
                    }
                    else if (roomName.StartsWith("Sala "))
                    {
                        if (roomName == "Sala corso Nettuno" ||
                            roomName == "Sala seminari" ||
                            roomName == "Sala Conferenze" ||
                            roomName.StartsWith("Sala studio") ||
                            roomName.StartsWith("Sala DEM"))
                        {
                            continue;
                        }

                        roomName = char.ToUpper(roomName[5]) + roomName.Substring(6).Replace(" - ", " ");
                    }
                    else
                    {
                        continue;
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
