﻿using LocusPocusBot.Rooms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LocusPocusBot
{
    public class FetchSchedulerHostedService : IHostedService
    {
        private readonly IRoomsService roomsService;
        private readonly ILogger<FetchSchedulerHostedService> logger;

        public FetchSchedulerHostedService(IRoomsService roomsService,
                                           ILogger<FetchSchedulerHostedService> logger)
        {
            this.roomsService = roomsService;
            this.logger = logger;
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
                this.logger.LogInformation($"Refreshing data for {department.Id}/{department.Name}");

                department.Rooms = await this.roomsService.LoadRooms(department);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}