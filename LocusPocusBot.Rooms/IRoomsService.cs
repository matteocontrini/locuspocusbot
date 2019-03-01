using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocusPocusBot.Rooms
{
    public interface IRoomsService
    {
        Task<List<Room>> LoadRooms(Department department);
    }
}
