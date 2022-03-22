using LocusPocusBot.Rooms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LocusPocusBot
{
    public class FetchSchedulerHostedService : IHostedService
    {
        private readonly ILogger<FetchSchedulerHostedService> logger;
        private readonly Department[] departments;
        private readonly IServiceProvider serviceProvider;
        private Timer timer;

        public FetchSchedulerHostedService(ILogger<FetchSchedulerHostedService> logger,
            Department[] departments,
            IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.departments = departments;
            this.serviceProvider = serviceProvider;
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
            using IServiceScope scope = this.serviceProvider.CreateScope();
            IRoomsService service = scope.ServiceProvider.GetRequiredService<IRoomsService>();

            foreach (Department department in this.departments)
            {
                this.logger.LogInformation("Refreshing data for {Id}/{Name}", department.Id, department.Name);

                try
                {
                    department.Rooms = await service.LoadRooms(department);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Exception while refreshing {Id}/{Name}", department.Id, department.Name);
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
