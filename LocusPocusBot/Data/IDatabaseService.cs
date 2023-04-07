using System.Threading.Tasks;
using LocusPocusBot.Rooms;
using Telegram.Bot.Types;

namespace LocusPocusBot.Data
{
    public interface IDatabaseService
    {
        Task LogUser(User user);
        Task LogChat(Chat chat);

        Task LogUsage(
            long chatId,
            RequestType requestType,
            AvailabilityType availabilityType,
            Department dep);
    }
}
