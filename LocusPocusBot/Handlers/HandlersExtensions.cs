using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LocusPocusBot.Handlers
{
    public static class HandlersExtensions
    {
        public static void AddHandlers(this IServiceCollection services)
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            foreach (TypeInfo info in assembly.DefinedTypes)
            {
                if (!info.IsAbstract && info.IsSubclassOf(typeof(HandlerBase)))
                {
                    services.AddScoped(info.AsType());
                }
            }

            services.AddScoped<IHandlersFactory, HandlersFactory>();
        }
    }
}
