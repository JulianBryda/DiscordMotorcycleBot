using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace DiscordMotorcycleBot.Models.Context
{
    public class DatabaseContext : DbContext
    {

        public DbSet<UserConfigModel> UserConfigs { get; set; }
        public DbSet<MotorcycleModel> Motorcycles { get; set; }
        public DbSet<SavedChannel> SavedChannels { get; set; }
        public DbSet<SavedRole> SavedRoles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                optionsBuilder.UseNpgsql("Server=45.88.109.189;Database=testDb;Uid=postgres;Pwd=2AJdf#fA24ASf!13G!F4;");
            }
            else
            {
                optionsBuilder.UseNpgsql("Server=localhost;Database=motorcycle;Uid=postgres;Pwd=2AJdf#fA24ASf!13G!F4;");
            }
        }
    }
}
