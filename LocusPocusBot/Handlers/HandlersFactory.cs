using Microsoft.Extensions.DependencyInjection;
using System;

namespace LocusPocusBot.Handlers
{
    public interface IHandlersFactory
    {
        T GetHandler<T>();
    }

    public class HandlersFactory : IHandlersFactory
    {
        private readonly IServiceProvider services;

        public HandlersFactory(IServiceProvider services)
        {
            this.services = services;
        }

        public T GetHandler<T>()
        {
            return this.services.GetRequiredService<T>();
        }
    }
}
