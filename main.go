package main

import (
	"fmt"
	"log"
	"strings"
	"time"

	"github.com/matteocontrini/locuspocusbot/tg"
	"github.com/pelletier/go-toml"
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

var bot *tg.Bot

func main() {
	var err error
	bot, err = tg.NewBot(conf.BotToken)

	if err != nil {
		log.Panic(err)
	}

	log.Printf("Authorized on account @%s", bot.Me.Username)

	updates := make(chan tg.Update, 100)
	bot.GetUpdates(updates, 10*time.Second)

	for update := range updates {
		if update.Message != nil {
			handleMessage(update.Message)
		} else if update.CallbackQuery != nil {
			handleCallbackQuery(update.CallbackQuery)
		}
	}
}

func handleMessage(message *tg.Message) {
	log.Printf("<%d> %s", message.Chat.ID, message.Text)

	sendRooms(message.Chat.ID)
}

// TODO: answerCallbackQuery

func handleCallbackQuery(query *tg.CallbackQuery) {
	data := query.Data
	log.Printf("<%d> %s", query.From.ID, data)

	parts := strings.Split(data, ";")

	if parts[0] == "free" && parts[1] == "povo" {
		group := parts[2]

		if group == "now" {
			editRoomsMessage(query.Message.Chat.ID, query.Message.MessageID, "now")
		} else if group == "future" {
			editRoomsMessage(query.Message.Chat.ID, query.Message.MessageID, "future")
		}

		answ := tg.AnswerCallbackQueryRequest{
			CallbackQueryId: query.ID,
			Text:            "Aggiornato alle " + formatHour(time.Now()),
		}

		bot.Send(&answ)
	}
}

func sendRooms(chatId int64) {
	editRoomsMessage(chatId, -1, "now")
}

func editRoomsMessage(chatId int64, mid int, group string) {
	now := time.Now()

	grouped := getFreeRoms(now)
	var out string
	var btn1 tg.InlineKeyboardButton
	var btn2 tg.InlineKeyboardButton

	if group == "now" {
		out += fmt.Sprintf("<strong>Aule libere alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		for _, r := range grouped.FreeNow {
			out += fmt.Sprintf("✳️ <strong>%s</strong>: %s\n", r.Name, r.Text)
		}

		btn1 = tg.InlineKeyboardButton{"✅ Libere", "free;povo;now"}
		btn2 = tg.InlineKeyboardButton{"Occupate", "free;povo;future"}
	} else {
		out += fmt.Sprintf("<strong>Aule occupate alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		for _, r := range grouped.FreeFuture {
			out += fmt.Sprintf("❌ <strong>%s</strong>: %s\n", r.Name, r.Text)
		}

		btn1 = tg.InlineKeyboardButton{"Libere", "free;povo;now"}
		btn2 = tg.InlineKeyboardButton{"✅ Occupate", "free;povo;future"}
	}

	markup := tg.InlineKeyboardMarkup{
		InlineKeyboard: [][]tg.InlineKeyboardButton{
			{
				btn1,
				btn2,
			},
		},
	}

	var msg interface{}

	if mid != -1 {
		msg = &tg.EditMessageRequest{
			ChatID:      chatId,
			MessageID:   mid,
			Text:        out,
			ParseMode:   "HTML",
			ReplyMarkup: markup,
		}
	} else {
		msg = &tg.MessageRequest{
			ChatID:      chatId,
			Text:        out,
			ParseMode:   "HTML",
			ReplyMarkup: markup,
		}
	}

	err := bot.Send(msg)

	if err != nil {
		log.Println(err)
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
