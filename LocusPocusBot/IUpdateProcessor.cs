using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace LocusPocusBot
{
    public interface IUpdateProcessor
    {
        Task ProcessUpdate(Update update);
    }
}
