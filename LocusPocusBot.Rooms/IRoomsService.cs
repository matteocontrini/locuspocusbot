using System.Threading.Tasks;

namespace LocusPocusBot.Rooms
{
    public interface IRoomsService
    {
        Task Update(Department department);
    }
}
