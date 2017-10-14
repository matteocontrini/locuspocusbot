package main

import (
	"fmt"
	// "github.com/davecgh/go-spew/spew"
	"time"
)

func main() {
	location, _ := time.LoadLocation("Europe/Rome")
	t := time.Date(2017, 10, 13, 13, 50, 0, 0, location)

	rooms := Departments[0].FindFreeRooms(t)

	// spew.Dump(rooms)

	for _, room := range rooms {
		fmt.Println(room.Name)
		fmt.Println(room.FreeSince)
		fmt.Println(room.FreeUntil)

		if room.IsFreeLimitedAt(t) {
			if room.FreeUntil.IsZero() {
				fmt.Println("Libera tutto il giorno")
			} else {
				fmt.Println("Libera fino alle", formatHour(room.FreeUntil))
			}
		} else {
			if room.FreeSince.IsZero() {
				if room.FreeUntil.IsZero() {
					fmt.Println("Libera tutto il giorno")
				} else {
					fmt.Println("fino alle", formatHour(room.FreeUntil))
				}
			} else {
				fmt.Println("Libera dalle", formatHour(room.FreeSince))

				if room.FreeUntil.IsZero() {
					fmt.Println("in poi")
				} else {
					fmt.Println("fino alle", formatHour(room.FreeUntil))
				}
			}
		}

		fmt.Println("")
	}
}

func formatHour(t time.Time) string {
	return fmt.Sprintf("%02d:%02d", t.Hour(), t.Minute())
}
