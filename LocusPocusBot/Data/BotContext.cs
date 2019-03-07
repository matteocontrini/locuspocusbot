using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LocusPocusBot.Data
{
    public class BotContext : DbContext
    {
        public DbSet<ChatEntity> Chats { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IHost host = Program.Host;
                DatabaseConfiguration conf = host.Services.GetRequiredService<DatabaseConfiguration>();
                string connectionString = conf.ConnectionString;
                
                optionsBuilder.UseMySql(connectionString);
            }
        }
    }
}
