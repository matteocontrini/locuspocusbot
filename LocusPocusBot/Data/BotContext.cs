using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LocusPocusBot.Data
{
    public class BotContext : DbContext
    {
        public DbSet<ChatEntity> Chats { get; set; }

        public DbSet<LogEntity> Logs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IHost host = Program.Host;

                IOptions<DatabaseConfiguration> options =
                    host.Services.GetRequiredService<IOptions<DatabaseConfiguration>>();

                string connectionString = options.Value.ConnectionString;
                
                optionsBuilder.UseMySql(connectionString);
            }
        }
    }
}
