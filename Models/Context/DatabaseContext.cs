using Microsoft.EntityFrameworkCore;

namespace DiscordMotorcycleBot.Models.Context
{
    internal class DatabaseContext : DbContext
    {

        public DbSet<UserModel> Users { get; set; }
        public DbSet<MotorcycleModel> Motorcycles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=45.88.109.189;Database=motorcycle;Uid=postgres;Pwd=2AJdf#fA24ASf!13G!F4;");
        }
    }
}
