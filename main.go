package main

import (
	"fmt"
	"github.com/pelletier/go-toml"
	tg "gopkg.in/telegram-bot-api.v4"
	"log"
	"strings"
	"time"
)

var conf Config

type Config struct {
	BotToken string
}

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

func loadConfig() error {
	t, err := toml.LoadFile("config/config.toml")

	if err != nil {
		return err
	}

	return t.Unmarshal(&conf)
}

func init() {
	log.Println("Loading config")

	if err := loadConfig(); err != nil {
		panic(err)
	}

	log.Println("Loaded config")
}

var bot *tg.BotAPI

func main() {
	var err error
	bot, err = tg.NewBotAPI(conf.BotToken)

	if err != nil {
		log.Panic(err)
	}

	// bot.Debug = true

	log.Printf("Authorized on account %s", bot.Self.UserName)

	u := tg.NewUpdate(0)
	u.Timeout = 10

	updates, err := bot.GetUpdatesChan(u)

	for update := range updates {
		if update.Message != nil {
			handleMessage(bot, &update)
		} else if update.CallbackQuery != nil {
			handleCallbackQuery(bot, &update)
		}
	}
}

func handleMessage(bot *tg.BotAPI, update *tg.Update) {
	log.Printf("<%d> %s", update.Message.Chat.ID, update.Message.Text)

	sendRooms(update.Message.Chat.ID)
}

func handleCallbackQuery(bot *tg.BotAPI, update *tg.Update) {
	data := update.CallbackQuery.Data
	log.Printf("<%d> %s", update.CallbackQuery.From.ID, data)

	parts := strings.Split(data, ";")

	if parts[0] == "free" && parts[1] == "povo" {
		group := parts[2]

		if group == "now" {
			editRoomsMessage(update.CallbackQuery.Message.Chat.ID, update.CallbackQuery.Message.MessageID, "now")
		} else if group == "future" {
			editRoomsMessage(update.CallbackQuery.Message.Chat.ID, update.CallbackQuery.Message.MessageID, "future")
		}
	}
}

func sendRooms(chatId int64) {
	editRoomsMessage(chatId, -1, "now")
}

func editRoomsMessage(chatId int64, mid int, group string) {
	now := time.Now()
	// location, _ := time.LoadLocation("Europe/Rome")
	// now = time.Date(2017, 10, 13, 11, 0, 0, 0, location)

	grouped := getFreeRoms(now)
	var out string

	if group == "now" {
		out += fmt.Sprintf("<strong>Aule libere alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		for _, r := range grouped.FreeNow {
			out += fmt.Sprintf("✳️ <strong>%s</strong>: %s\n", r.Name, r.Text)
		}
	} else {
		out += fmt.Sprintf("<strong>Aule occupate alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		for _, r := range grouped.FreeFuture {
			out += fmt.Sprintf("❌ <strong>%s</strong>: %s\n", r.Name, r.Text)
		}
	}

	if mid != -1 {
		msg := tg.NewEditMessageText(chatId, mid, out)

		msg.ParseMode = "HTML"
		markup := tg.NewInlineKeyboardMarkup(
			tg.NewInlineKeyboardRow(
				tg.NewInlineKeyboardButtonData("✅ Libere", "free;povo;now"),
				tg.NewInlineKeyboardButtonData("Occupate", "free;povo;future"),
			),
		)

		msg.ReplyMarkup = &markup

		bot.Send(msg)
	} else {
		msg := tg.NewMessage(chatId, out)

		msg.ParseMode = "HTML"
		msg.ReplyMarkup = tg.NewInlineKeyboardMarkup(
			tg.NewInlineKeyboardRow(
				tg.NewInlineKeyboardButtonData("✅ Libere", "free;povo;now"),
				tg.NewInlineKeyboardButtonData("Occupate", "free;povo;future"),
			),
		)

		bot.Send(msg)
	}
}

func formatHour(t time.Time) string {
	return fmt.Sprintf("%02d:%02d", t.Hour(), t.Minute())
}

func getFreeRoms(t time.Time) GroupedFreeRoom {
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
					text += fmt.Sprintf(" alle %s", formatHour(room.FreeUntil))
				}

				grouped.AddFreeFuture(room, text)
			}
		}
	}

	return grouped
}
