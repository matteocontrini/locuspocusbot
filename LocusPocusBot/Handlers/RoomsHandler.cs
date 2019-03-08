using LocusPocusBot.Rooms;
using NodaTime;
using NodaTime.Text;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace LocusPocusBot.Handlers
{
    public class RoomsHandler : HandlerBase
    {
        public Department RequestedDepartment { get; set; }

        public AvailabilityType RequestedGroup { get; set; }

        private readonly IBotService bot;

        public RoomsHandler(IBotService botService)
        {
            this.bot = botService;
        }

        public override async Task Run()
        {
            if (this.RequestedDepartment.Rooms == null ||
                this.RequestedDepartment.Rooms.Count == 0)
            {
                if (this.CallbackQuery != null)
                {
                    await this.bot.Client.AnswerCallbackQueryAsync(
                        callbackQueryId: this.CallbackQuery.Id,
                        text: "❗ Dati aggiornati non disponibili",
                        showAlert: true
                    );
                }
                else
                {
                    await this.bot.Client.SendTextMessageAsync(
                        chatId: this.Chat.Id,
                        text: "❗ Dati aggiornati non disponibili"
                    );
                }

                return;
            }

            Instant now = SystemClock.Instance.GetCurrentInstant();
            AvailabilityGroup[] groups = this.RequestedDepartment.FindFreeRoomsAt(now);

            StringBuilder msg = new StringBuilder();
            InlineKeyboardButton[] buttons = new InlineKeyboardButton[3];

            string slug = this.RequestedDepartment.Slug;
            string prefix = this.RequestedDepartment.Name.ToUpper();
            string nowPretty = InstantToPrettyLocalTime(now);

            if (this.RequestedGroup == AvailabilityType.Free)
            {
                msg.Append("<strong>");
                msg.Append(prefix);
                msg.Append(" - Aule libere alle ");
                msg.Append(nowPretty);
                msg.Append("</strong>");
                msg.AppendLine();
                msg.AppendLine();

                if (groups[0].Rooms.Count > 0)
                {
                    foreach (RoomAvailability room in groups[0].Rooms)
                    {
                        AddRoomAvailability(msg, room);
                    }
                }
                else
                {
                    msg.Append("❌ Tutte le aule sono occupate.");
                }

                buttons[0] = InlineKeyboardButton.WithCallbackData("✅ Libere", $"free;{slug};now");
                buttons[1] = InlineKeyboardButton.WithCallbackData("Occupate", $"free;{slug};future");
                buttons[2] = InlineKeyboardButton.WithCallbackData("Tutte le aule", $"free;{slug};all");
            }
            else if (this.RequestedGroup == AvailabilityType.Occupied)
            {
                msg.Append("<strong>");
                msg.Append(prefix);
                msg.Append(" - Aule occupate alle ");
                msg.Append(nowPretty);
                msg.Append("</strong>");
                msg.AppendLine();
                msg.AppendLine();

                if (groups[1].Rooms.Count > 0)
                {
                    foreach (RoomAvailability room in groups[1].Rooms)
                    {
                        AddRoomAvailability(msg, room);
                    }
                }
                else
                {
                    msg.Append("✳️ Tutte le aule sono libere.");
                }

                buttons[0] = InlineKeyboardButton.WithCallbackData("Libere", $"free;{slug};now");
                buttons[1] = InlineKeyboardButton.WithCallbackData("✅ Occupate", $"free;{slug};future");
                buttons[2] = InlineKeyboardButton.WithCallbackData("Tutte le aule", $"free;{slug};all");
            }
            else
            {
                msg.Append("<strong>");
                msg.Append(prefix);
                msg.Append(" - Situazione aule alle ");
                msg.Append(nowPretty);
                msg.Append("</strong>");
                msg.AppendLine();
                msg.AppendLine();
                
                foreach (RoomAvailability room in groups[2].Rooms)
                {
                    AddRoomAvailability(msg, room);
                }

                buttons[0] = InlineKeyboardButton.WithCallbackData("Libere", $"free;{slug};now");
                buttons[1] = InlineKeyboardButton.WithCallbackData("Occupate", $"free;{slug};future");
                buttons[2] = InlineKeyboardButton.WithCallbackData("✅ Tutte le aule", $"free;{slug};all");
            }

            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(
                new List<List<InlineKeyboardButton>>
                {
                    new List<InlineKeyboardButton>
                    {
                        buttons[0],
                        buttons[1]
                    },
                    new List<InlineKeyboardButton>
                    {
                        buttons[2]
                    }
                }
            );

            if (this.CallbackQuery != null)
            {
                await this.bot.Client.EditMessageTextAsync(
                    chatId: this.CallbackQuery.Message.Chat.Id,
                    messageId: this.CallbackQuery.Message.MessageId,
                    text: msg.ToString(),
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard
                );

                await this.bot.Client.AnswerCallbackQueryAsync(
                    callbackQueryId: this.CallbackQuery.Id,
                    text: $"Aggiornato alle {nowPretty}"
                );
            }
            else
            {
                await this.bot.Client.SendTextMessageAsync(
                    chatId: this.Chat.Id,
                    text: msg.ToString(),
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard
                );
            }
        }

        private void AddRoomAvailability(StringBuilder msg, RoomAvailability room)
        {
            if (room.IsFreeNow)
            {
                msg.Append("✳️ <strong>");
                msg.Append(room.Name);
                msg.Append("</strong>: ");

                if (room.FreeInterval.HasEnd)
                {
                    msg.Append("Libera fino alle ");
                    msg.Append(InstantToPrettyLocalTime(room.FreeInterval.End));
                }
                else
                {
                    msg.Append("Libera tutto il giorno");
                }
            }
            else
            {
                msg.Append("❌ <strong>");
                msg.Append(room.Name);
                msg.Append("</strong>: ");

                if (room.FreeInterval.HasEnd)
                {
                    msg.Append("Libera ore ");
                    msg.Append(InstantToPrettyLocalTime(room.FreeInterval.Start));
                    msg.Append(" - ");
                    msg.Append(InstantToPrettyLocalTime(room.FreeInterval.End));
                }
                else
                {
                    msg.Append("Libera dalle ");
                    msg.Append(InstantToPrettyLocalTime(room.FreeInterval.Start));
                    msg.Append(" in poi");
                }
            }

            msg.AppendLine();
        }

        private string InstantToPrettyLocalTime(Instant instant)
        {
            LocalTimePattern pattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");
            DateTimeZone tz = DateTimeZoneProviders.Tzdb["Europe/Rome"];
            LocalTime time = instant.InZone(tz).TimeOfDay;
            return pattern.Format(time);
        }
    }
}
