using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LocusPocusBot.Data
{
    public class BotContext : DbContext
    {
        public DbSet<ChatEntity> Chats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=bot.db");
        }
    }
}
