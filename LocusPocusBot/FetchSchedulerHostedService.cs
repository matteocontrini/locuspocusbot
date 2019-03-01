using LocusPocusBot.Rooms;
using Microsoft.Extensions.Hosting;
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

        private Task Fetch()
        {
            // TODO: catch

            return this.roomsService.Update(Department.Povo);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
