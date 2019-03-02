using LocusPocusBot.Rooms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocusPocusBot
{
    public class FetchSchedulerHostedService : IHostedService
    {
        private readonly IRoomsService roomsService;
        private readonly ILogger<FetchSchedulerHostedService> logger;
        private Timer timer;

        public FetchSchedulerHostedService(IRoomsService roomsService,
                                           ILogger<FetchSchedulerHostedService> logger)
        {
            this.roomsService = roomsService;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.timer = new Timer(
                callback: Callback,
                state: null,
                dueTime: TimeSpanUntilNextTick(),
                period: TimeSpan.FromMilliseconds(-1)
            );

            return Fetch();
        }

        async void Callback(object state)
        {
            await Fetch();

            this.timer.Change(
                dueTime: TimeSpanUntilNextTick(),
                period: TimeSpan.FromMilliseconds(-1)
            );
        }

        private TimeSpan TimeSpanUntilNextTick()
        {
            return TimeSpan.FromMinutes(60 - DateTime.UtcNow.Minute);
        }

        private async Task Fetch()
        {
            Department[] departments = new Department[]
            {
                Department.Povo,
                Department.Mesiano
            };

            foreach (Department department in departments)
            {
                this.logger.LogInformation($"Refreshing data for {department.Id}/{department.Name}");

                try
                {
                    department.Rooms = await this.roomsService.LoadRooms(department);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Exception while refreshing {department.Id}/{department.Name}");
                }
            }

            this.logger.LogInformation("Done refreshing");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
