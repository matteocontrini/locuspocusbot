package main

import (
	"fmt"
	"github.com/robfig/cron"
	"github.com/tidwall/gjson"
	"io/ioutil"
	"log"
	"net/http"
	neturl "net/url"
	"regexp"
	"strings"
	"time"
)

type Department struct {
	ID    string
	Name  string
	Slugs []string
	Rooms []Room
}

type Room struct {
	ID       string
	Name     string
	Lectures []Lecture
}

type FreeRoom struct {
	Room
	FreeSince time.Time
	FreeUntil time.Time
}

type Lecture struct {
	StartTime time.Time
	EndTime   time.Time
	Name      string
}

func (lec *Lecture) IsAt(t time.Time) bool {
	if (t.After(lec.StartTime) || t.Equal(lec.StartTime)) &&
		// don't chek equality at endtime because room could be free
		// at endtime if there are no lectures after that
		t.Before(lec.EndTime) {
		return true
	} else {
		return false
	}
}

func (room *FreeRoom) IsFreeLimitedAt(t time.Time) bool {
	if room.FreeSince.IsZero() {
		return false
	} else {
		if t.After(room.FreeSince) || t.Equal(room.FreeSince) {
			if room.FreeUntil.IsZero() {
				return true
			} else {
				if t.Before(room.FreeUntil) {
					return true
				} else {
					return false
				}
			}
		} else {
			return false
		}
	}
}

var Departments []Department = []Department{
	Department{
		ID:    "E0503",
		Name:  "Povo",
		Slugs: []string{"povo"},
	},
	// Department{
	// 	Id:    "E0101",
	// 	Name:  "Economia e Management",
	// 	Slugs: []string{"economia"},
	// },
	// Department{
	// 	Id:    "E0801",
	// 	Name:  "Lettere e Filosofia",
	// 	Slugs: []string{"lettere", "filosofia"},
	// },
	// Department{
	// 	Id:    "E0301",
	// 	Name:  "Ingegneria Civile Ambientale Meccanica (Mesiano)",
	// 	Slugs: []string{"mesiano", "ingegneria"},
	// },
	// Department{
	// 	Id:    "E0201",
	// 	Name:  "Giurisprudenza",
	// 	Slugs: []string{"giurisprudenza", "giuri"},
	// },
}

func init() {
	load := func() {
		// For each department, load the lectures
		for i := range Departments {
			dep := &Departments[i]

			log.Printf("Loading rooms for %s", dep.Name)
			dep.loadLectures()
		}

		log.Println("Loaded rooms")
	}

	load()

	c := cron.New()
	c.AddFunc("@hourly", load)
	c.Start()
}

func (dep *Department) loadLectures() {
	url := "https://easyroom.unitn.it/Orario/rooms_call.php"

	// Format date as e.g. 13-10-2017
	now := time.Now()
	date := fmt.Sprintf("%02d-%02d-%04d", now.Day(), now.Month(), now.Year())

	data := neturl.Values{
		"form-type": {"rooms"},
		"sede":      {dep.ID},
		"date":      {date},
		"_lang":     {"it"},
	}

	resp, err := http.PostForm(url, data)

	if err != nil {
		panic(err)
	}

	content, err := ioutil.ReadAll(resp.Body)
	resp.Body.Close()

	if err != nil {
		panic(err)
	}

	// Parse json
	json := gjson.ParseBytes(content)

	var rooms []Room

	// Loop through rooms for the department
	json.Get("area_rooms." + dep.ID).ForEach(func(key, value gjson.Result) bool {
		room := Room{
			ID:   key.String(),
			Name: value.Get("room_name").String(),
		}

		if strings.HasPrefix(room.Name, "LD") {
			return true
		}

		room.Name = strings.TrimPrefix(room.Name, "Aula")
		room.Name = strings.TrimSpace(room.Name)
		room.Name = strings.ToUpper(room.Name)

		re := regexp.MustCompile(`[A-B]{1}[0-9]{3}`)
		match := re.FindString(room.Name)

		if match != "" {
			room.Name = match
		} else {
			return true
		}

		rooms = append(rooms, room)

		return true
	})

	// Loop through lectures
	json.Get("events").ForEach(func(key, value gjson.Result) bool {
		lecture := Lecture{
			Name:      value.Get("name").String(),
			StartTime: time.Unix(value.Get("timestamp_from").Int(), 0),
			EndTime:   time.Unix(value.Get("timestamp_to").Int(), 0),
		}

		roomId := value.Get("CodiceAula").String()

		// Assign lecture to the correct room
		for i := range rooms {
			room := &rooms[i]

			if room.ID == roomId {
				room.Lectures = append(room.Lectures, lecture)
				break
			}
		}

		return true
	})

	dep.Rooms = rooms
}

func (dep *Department) FindFreeRooms(t time.Time) []FreeRoom {
	rooms := dep.Rooms
	var freeRooms []FreeRoom

	for _, room := range rooms {
		var freeUntil time.Time
		var freeSince time.Time

		if len(room.Lectures) > 0 {
			if t.Before(room.Lectures[0].StartTime) {
				// First lesson still to start...
				freeUntil = room.Lectures[0].StartTime
			} else if last := room.Lectures[len(room.Lectures)-1]; t.After(last.EndTime) {
				// Input time was after all the lectures, take the last one

				freeSince = last.EndTime
				// freeUntil the end of the day
			} else {
				// Loop through the lectures
				for i, lecture := range room.Lectures {
					var previousLecture Lecture

					if i > 0 {
						previousLecture = room.Lectures[i-1]
					}

					if lecture.IsAt(t) {
						// Loop through the next lectures
						for j := i + 1; j < len(room.Lectures); j++ {
							// If there's a gap between lessons,
							// the room is free since the end of the previous lesson
							if room.Lectures[j].StartTime != room.Lectures[j-1].EndTime {
								freeSince = room.Lectures[j-1].EndTime
								freeUntil = room.Lectures[j].StartTime
								break
							}
						}

						// If we've arrived at the end of the lectures with no gaps,
						// the room will be free from that moment until the end of the day
						if freeSince.IsZero() {
							freeSince = room.Lectures[len(room.Lectures)-1].EndTime
							// freeUntil the end of the day
						}

						break
					} else if !previousLecture.StartTime.IsZero() && // previous exists
						(previousLecture.EndTime.Before(t) || previousLecture.EndTime.Equal(t)) &&
						lecture.StartTime.After(t) {
						// Currently free, we're in a gap between two lectures,
						// the room will be free until the lecture starts
						freeSince = previousLecture.EndTime
						freeUntil = lecture.StartTime

						break
					}
				}
			}
		}

		freeRoom := FreeRoom{room, freeSince, freeUntil}
		freeRooms = append(freeRooms, freeRoom)
	}

	return freeRooms
}
