package tg

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"time"
)

type Bot struct {
	Token string
	Me    *User
}

const ApiBase string = "https://api.telegram.org/bot%s/%s"

type User struct {
	ID           int    `json:"id"`
	FirstName    string `json:"first_name"`
	LastName     string `json:"last_name"`
	Username     string `json:"username"`
	LanguageCode string `json:"language_code"`
}

type Chat struct {
	ID        int64  `json:"id"`
	Type      string `json:"type"`
	Title     string `json:"title"`
	Username  string `json:"username"`
	FirstName string `json:"first_name"`
	LastName  string `json:"last_name"`
}

type Response struct {
	Ok          bool            `json:"ok"`
	Result      json.RawMessage `json:"result"`
	Description string          `json:"description"`
}

type Update struct {
	UpdateID      int            `json:"update_id"`
	Message       *Message       `json:"message"`
	CallbackQuery *CallbackQuery `json:"callback_query"`
}

type Message struct {
	MessageID int    `json:"message_id"`
	From      *User  `json:"from"`
	Date      int    `json:"date"`
	Chat      *Chat  `json:"chat"`
	Text      string `json:"text"`
}

type CallbackQuery struct {
	ID      string   `json:"id"`
	From    *User    `json:"from"`
	Message *Message `json:"message"`
	Data    string   `json:"data"`
}

type EditMessageRequest struct {
	ChatID      int64  `json:"chat_id"`
	MessageID   int    `json:"message_id"`
	Text        string `json:"text"`
	ParseMode   string `json:"parse_mode,omitempty"`
	ReplyMarkup InlineKeyboardMarkup
}

type MessageRequest struct {
	ChatID                int64  `json:"chat_id"`
	Text                  string `json:"text"`
	ParseMode             string `json:"parse_mode,omitempty"`
	ReplyMarkup           ReplyMarkup
	DisableWebPagePreview bool `json:"disable_web_page_preview,omitempty"`
}

func (u *MessageRequest) MarshalJSON() ([]byte, error) {
	var serialized string

	if u.ReplyMarkup == nil {
		serialized = ""
	} else {
		var err error
		serialized, err = u.ReplyMarkup.Serialize()

		if err != nil {
			return nil, err
		}
	}

	// http://choly.ca/post/go-json-marshalling/
	type Alias MessageRequest

	return json.Marshal(&struct {
		ReplyMarkup string `json:"reply_markup,omitempty"`
		*Alias
	}{
		ReplyMarkup: serialized,
		Alias:       (*Alias)(u),
	})
}

func (u *EditMessageRequest) MarshalJSON() ([]byte, error) {
	var serialized string

	if len(u.ReplyMarkup.InlineKeyboard) == 0 {
		serialized = ""
	} else {
		var err error
		serialized, err = u.ReplyMarkup.Serialize()

		if err != nil {
			return nil, err
		}
	}

	// http://choly.ca/post/go-json-marshalling/
	type Alias EditMessageRequest

	return json.Marshal(&struct {
		ReplyMarkup string `json:"reply_markup,omitempty"`
		*Alias
	}{
		ReplyMarkup: serialized,
		Alias:       (*Alias)(u),
	})
}

type ReplyMarkup interface {
	Serialize() (string, error)
}

type InlineKeyboardMarkup struct {
	InlineKeyboard [][]InlineKeyboardButton `json:"inline_keyboard"`
}

func (m InlineKeyboardMarkup) Serialize() (string, error) {
	res, err := json.Marshal(m)

	if err != nil {
		return "", err
	}

	return string(res), nil
}

type InlineKeyboardButton struct {
	Text         string `json:"text"`
	CallbackData string `json:"callback_data,omitempty"`
}

type GetUpdatesRequest struct {
	Offset         int      `json:"offset"`
	Timeout        int      `json:"timeout"`
	AllowedUpdates []string `json:"allowed_updates"`
}

type AnswerCallbackQueryRequest struct {
	CallbackQueryId string `json:"callback_query_id"`
	Text            string `json:"text"`
	ShowAlert       bool   `json:"show_alert"`
}

func NewBot(token string) (*Bot, error) {
	bot := &Bot{
		Token: token,
	}

	me, err := bot.GetMe()
	bot.Me = &me

	if err != nil {
		return nil, err
	}

	return bot, nil
}

func (bot *Bot) GetMe() (User, error) {
	resp, err := bot.makeRequest("getMe", nil)

	if err != nil {
		return User{}, err
	}

	var user User
	json.Unmarshal(resp.Result, &user)

	return user, nil
}

func (bot *Bot) GetUpdates(ch chan Update, timeout time.Duration) {
	go func() {
		offset := 0

		for {
			updates, err := bot.getUpdates(offset, timeout)

			if err != nil {
				log.Println("Failed to get updates, retrying in 1 second...")
				log.Println("Reason:", err)
				time.Sleep(time.Second * 3)

				continue
			}

			for _, update := range updates {
				if update.UpdateID >= offset {
					offset = update.UpdateID + 1
					ch <- update
				}
			}
		}
	}()
}

func (bot *Bot) Send(msg interface{}) error {
	switch msg.(type) {
	case *MessageRequest:
		return bot.sendMessage(msg.(*MessageRequest))
	case *EditMessageRequest:
		return bot.editMessageText(msg.(*EditMessageRequest))
	case *AnswerCallbackQueryRequest:
		return bot.answerCallbackQuery(msg.(*AnswerCallbackQueryRequest))
	default:
		return errors.New("Unsupported message type")
	}
}

func (bot *Bot) sendMessage(msg *MessageRequest) error {
	_, err := bot.makeRequest("sendMessage", msg)

	if err != nil {
		return err
	}

	return nil
}

func (bot *Bot) editMessageText(msg *EditMessageRequest) error {
	_, err := bot.makeRequest("editMessageText", msg)

	if err != nil {
		return err
	}

	return nil
}

func (bot *Bot) answerCallbackQuery(msg *AnswerCallbackQueryRequest) error {
	_, err := bot.makeRequest("answerCallbackQuery", msg)

	if err != nil {
		return err
	}

	return nil
}

func (bot *Bot) getUpdates(offset int, timeout time.Duration) ([]Update, error) {
	payload := GetUpdatesRequest{
		Offset:         offset,
		Timeout:        int(timeout),
		AllowedUpdates: []string{"message", "callback_query"},
	}

	resp, err := bot.makeRequest("getUpdates", payload)

	if err != nil {
		return []Update{}, err
	}

	var updates []Update
	json.Unmarshal(resp.Result, &updates)

	return updates, nil
}

func (bot *Bot) makeRequest(endpoint string, payload interface{}) (Response, error) {
	method := fmt.Sprintf(ApiBase, bot.Token, endpoint)

	client := http.Client{}

	var buffer bytes.Buffer
	if err := json.NewEncoder(&buffer).Encode(payload); err != nil {
		return Response{}, err
	}

	resp, err := client.Post(method, "application/json", &buffer)
	if err != nil {
		return Response{}, err
	}

	defer resp.Body.Close()

	bytes, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		return Response{}, err
	}

	if resp.StatusCode == http.StatusForbidden {
		return Response{}, errors.New(http.StatusText(resp.StatusCode))
	}

	if resp.StatusCode != http.StatusOK {
		return Response{}, errors.New(http.StatusText(resp.StatusCode) + ": " + string(bytes))
	}

	var apiResp Response
	json.Unmarshal(bytes, &apiResp)

	if !apiResp.Ok {
		return apiResp, errors.New(apiResp.Description)
	}

	return apiResp, nil
}
