package main

import (
	"fmt"
	"gopkg.in/telegram-bot-api.v4"
	"log"
	"time"
)

type FreeRoomText struct {
	FreeRoom
	Text string
}

type GroupedFreeRoom struct {
	FreeNow    []FreeRoomText
	FreeFuture []FreeRoomText
}

func (g *GroupedFreeRoom) AddFreeNow(room FreeRoom, text string) {
	r := FreeRoomText{room, text}
	g.FreeNow = append(g.FreeNow, r)
}

func (g *GroupedFreeRoom) AddFreeFuture(room FreeRoom, text string) {
	r := FreeRoomText{room, text}
	g.FreeFuture = append(g.FreeFuture, r)
}

func main() {
	bot, err := tgbotapi.NewBotAPI("431023270:AAHiWdYW5Bamkw5fpwssrNbgCus1pmpJO84")

	if err != nil {
		log.Panic(err)
	}

	// bot.Debug = true

	log.Printf("Authorized on account %s", bot.Self.UserName)

	u := tgbotapi.NewUpdate(0)
	u.Timeout = 10

	updates, err := bot.GetUpdatesChan(u)

	for update := range updates {
		if update.Message == nil {
			continue
		}

		log.Printf("<%d> %s", update.Message.From.ID, update.Message.Text)

		grouped := getFreeRoms()

		out := ""
		for _, r := range grouped.FreeNow {
			out += fmt.Sprintf("✅ <strong>%s</strong>: %s\n", r.Name, r.Text)
		}

		msg := tgbotapi.NewMessage(update.Message.Chat.ID, out)
		msg.ParseMode = "HTML"

		bot.Send(msg)

		out = ""
		for _, r := range grouped.FreeFuture {
			out += fmt.Sprintf("❌ <strong>%s</strong>: %s\n", r.Name, r.Text)
		}

		msg = tgbotapi.NewMessage(update.Message.Chat.ID, out)
		msg.ParseMode = "HTML"

		bot.Send(msg)
	}
}

func formatHour(t time.Time) string {
	return fmt.Sprintf("%02d:%02d", t.Hour(), t.Minute())
}

func getFreeRoms() GroupedFreeRoom {
	location, _ := time.LoadLocation("Europe/Rome")
	t := time.Date(2017, 10, 13, 11, 0, 0, 0, location)

	rooms := Departments[0].FindFreeRooms(t)

	var grouped GroupedFreeRoom

	for _, room := range rooms {
		if room.IsFreeLimitedAt(t) {
			if room.FreeUntil.IsZero() {
				text := "Libera tutto il giorno"
				grouped.AddFreeNow(room, text)
			} else {
				text := fmt.Sprintf("Libera fino alle %s", formatHour(room.FreeUntil))
				grouped.AddFreeNow(room, text)
			}
		} else {
			if room.FreeSince.IsZero() {
				if room.FreeUntil.IsZero() {
					text := "Libera tutto il giorno"
					grouped.AddFreeNow(room, text)
				} else {
					text := fmt.Sprintf("Libera fino alle %s", formatHour(room.FreeUntil))
					grouped.AddFreeNow(room, text)
				}
			} else {
				text := fmt.Sprintf("Libera dalle %s", formatHour(room.FreeSince))

				if room.FreeUntil.IsZero() {
					text += fmt.Sprintf(" in poi")
				} else {
					text += fmt.Sprintf(" fino alle %s", formatHour(room.FreeUntil))
				}

				grouped.AddFreeFuture(room, text)
			}
		}
	}

	return grouped
}
