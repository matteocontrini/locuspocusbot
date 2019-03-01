using LocusPocusBot.Rooms;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LocusPocusBot
{
    public class FetchSchedulerHostedService : IHostedService
    {
        private readonly IRoomsService roomsService;

        public FetchSchedulerHostedService(IRoomsService roomsService)
        {
            this.roomsService = roomsService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // TODO: schedule every hour

            return Fetch();
        }

        private async Task Fetch()
        {
            // TODO: catch

            Department[] departments = new Department[]
            {
                Department.Povo
            };

            foreach (Department department in departments)
            {
                department.Rooms = await this.roomsService.LoadRooms(department);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
