package main

import (
	"fmt"
	"log"
	"sort"
	"strings"
	"time"

	"github.com/matteocontrini/go-tg"
	"github.com/pelletier/go-toml"
)

var conf Config

type Config struct {
	BotToken string
}

type FreeRoomText struct {
	FreeRoom
	Text      string
	IsFreeNow bool
}

type GroupedRooms struct {
	FreeNow    []FreeRoomText
	FreeFuture []FreeRoomText
	All        []*FreeRoomText
}

func (g *GroupedRooms) AddFreeNow(room FreeRoom, text string) {
	r := FreeRoomText{
		FreeRoom:  room,
		Text:      text,
		IsFreeNow: true,
	}

	g.FreeNow = append(g.FreeNow, r)
	g.All = append(g.All, &r)
}

func (g *GroupedRooms) AddFreeFuture(room FreeRoom, text string) {
	r := FreeRoomText{
		FreeRoom:  room,
		Text:      text,
		IsFreeNow: false,
	}

	g.FreeFuture = append(g.FreeFuture, r)
	g.All = append(g.All, &r)
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
			// Ignore non-text messages
			if update.Message.Text != "" {
				handleMessage(update.Message)
			}
		} else if update.CallbackQuery != nil {
			handleCallbackQuery(update.CallbackQuery)
		}
	}
}

func handleMessage(message *tg.Message) {
	log.Printf("<%d> %s", message.Chat.ID, message.Text)

	if message.Text == "/start" {
		msg := tg.MessageRequest{
			ChatID:                message.Chat.ID,
			Text:                  "Ciao! 🤓\n\nSono *LocusPocus* e ti posso aiutare a trovare le aule libere presso il Polo Ferrari dell'Università di Trento 🎓\n\nScrivimi /povo (o qualsiasi altra cosa) per ottenere la lista delle aule libere.\n\nAltre info in /aiuto",
			ParseMode:             "Markdown",
			DisableWebPagePreview: true,
		}

		err := bot.Send(&msg)

		if err != nil {
			log.Println(err)
		}
	} else if message.Text == "/aiuto" {
		msg := tg.MessageRequest{
			ChatID:    message.Chat.ID,
			Text:      "*LocusPocus* è il bot per controllare la disponibilità delle aule presso il Polo Ferrari dell'Università di Trento 🎓\n\nScrivi /povo per ottenere la lista delle aule libere.\n\nSviluppato da Matteo Contrini (@matteocontrini). Si ringraziano Alessandro Conti per il nome del bot e Dario Crisafulli per il logo.\n\n[Codice sorgente](https://github.com/matteocontrini/locuspocusbot)",
			ParseMode: "Markdown",
		}

		err := bot.Send(&msg)

		if err != nil {
			log.Println(err)
		}
	} else {
		sendRooms(message.Chat.ID)
	}
}

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
		} else if group == "all" {
			editRoomsMessage(query.Message.Chat.ID, query.Message.MessageID, "all")
		}

		answ := tg.AnswerCallbackQueryRequest{
			CallbackQueryId: query.ID,
			Text:            "Aggiornato alle " + formatHour(time.Now()),
		}

		bot.Send(&answ)
	}
}

func sendRooms(chatID int64) {
	editRoomsMessage(chatID, -1, "now")
}

func editRoomsMessage(chatID int64, mid int, group string) {
	now := time.Now()

	grouped := getFreeRoms(now, group)
	var out string
	var btn1 tg.InlineKeyboardButton
	var btn2 tg.InlineKeyboardButton
	var btn3 tg.InlineKeyboardButton

	if group == "now" {
		out += fmt.Sprintf("<strong>Aule libere alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		if len(grouped.FreeNow) > 0 {
			for _, r := range grouped.FreeNow {
				out += fmt.Sprintf("✳️ <strong>%s</strong>: %s\n", r.Name, r.Text)
			}
		} else {
			out += "❌ Tutte le aule sono occupate."
		}

		btn1 = tg.InlineKeyboardButton{
			Text:         "✅ Libere",
			CallbackData: "free;povo;now",
		}

		btn2 = tg.InlineKeyboardButton{
			Text:         "Occupate",
			CallbackData: "free;povo;future",
		}

		btn3 = tg.InlineKeyboardButton{
			Text:         "Tutte le aule",
			CallbackData: "free;povo;all",
		}
	} else if group == "future" {
		out += fmt.Sprintf("<strong>Aule occupate alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		if len(grouped.FreeFuture) > 0 {
			for _, r := range grouped.FreeFuture {
				out += fmt.Sprintf("❌ <strong>%s</strong>: %s\n", r.Name, r.Text)
			}
		} else {
			out += "✳️ Tutte le aule sono libere."
		}

		btn1 = tg.InlineKeyboardButton{
			Text:         "Libere",
			CallbackData: "free;povo;now",
		}

		btn2 = tg.InlineKeyboardButton{
			Text:         "✅ Occupate",
			CallbackData: "free;povo;future",
		}

		btn3 = tg.InlineKeyboardButton{
			Text:         "Tutte le aule",
			CallbackData: "free;povo;all",
		}
	} else {
		out += fmt.Sprintf("<strong>Situazione aule alle %02d:%02d</strong>\n\n", now.Hour(), now.Minute())

		for _, r := range grouped.All {
			if r.IsFreeNow {
				out += fmt.Sprintf("✳️ <strong>%s</strong>: %s\n", r.Name, r.Text)
			} else {
				out += fmt.Sprintf("❌ <strong>%s</strong>: %s\n", r.Name, r.Text)
			}
		}

		btn1 = tg.InlineKeyboardButton{
			Text:         "Libere",
			CallbackData: "free;povo;now",
		}

		btn2 = tg.InlineKeyboardButton{
			Text:         "Occupate",
			CallbackData: "free;povo;future",
		}

		btn3 = tg.InlineKeyboardButton{
			Text:         "✅ Tutte le aule",
			CallbackData: "free;povo;all",
		}
	}

	markup := tg.InlineKeyboardMarkup{
		InlineKeyboard: [][]tg.InlineKeyboardButton{
			{
				btn1,
				btn2,
			},
			{
				btn3,
			},
		},
	}

	var msg interface{}

	if mid != -1 {
		msg = &tg.EditMessageRequest{
			ChatID:      chatID,
			MessageID:   mid,
			Text:        out,
			ParseMode:   "HTML",
			ReplyMarkup: markup,
		}
	} else {
		msg = &tg.MessageRequest{
			ChatID:      chatID,
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

func getFreeRoms(t time.Time, group string) GroupedRooms {
	rooms := Departments[0].FindFreeRooms(t)

	var grouped GroupedRooms

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
				var text string

				if group == "now" {
					text = fmt.Sprintf("Libera dalle %s", formatHour(room.FreeSince))

					if room.FreeUntil.IsZero() {
						text += fmt.Sprintf(" in poi")
					} else {
						text += fmt.Sprintf(" alle %s", formatHour(room.FreeUntil))
					}
				} else {
					if room.FreeUntil.IsZero() {
						text = fmt.Sprintf("Libera dalle %s in poi", formatHour(room.FreeSince))
					} else {
						text = fmt.Sprintf("Libera ore %s - %s",
							formatHour(room.FreeSince),
							formatHour(room.FreeUntil),
						)
					}
				}

				grouped.AddFreeFuture(room, text)
			}
		}
	}

	sort.Slice(grouped.FreeNow, func(i, j int) bool {
		if grouped.FreeNow[i].FreeUntil.IsZero() {
			return true
		}

		if grouped.FreeNow[j].FreeUntil.IsZero() {
			return false
		}

		return grouped.FreeNow[i].FreeUntil.After(grouped.FreeNow[j].FreeUntil)
	})

	sort.Slice(grouped.FreeFuture, func(i, j int) bool {
		return grouped.FreeFuture[i].FreeSince.Before(grouped.FreeFuture[j].FreeSince)
	})

	sort.Slice(grouped.All, func(i, j int) bool {
		return grouped.All[i].Name < grouped.All[j].Name
	})

	return grouped
}
