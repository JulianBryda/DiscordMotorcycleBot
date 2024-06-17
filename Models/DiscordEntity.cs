
namespace DiscordMotorcycleBot.Models
{
    public class DiscordEntity
    {
        public required int Id { get; set; }
        public required ulong EntityId { get; set; }  
        public required EntityType EntityType { get; set; }
        public required ulong GuildId { get; set; }
    }

    public enum EntityType 
    {
        Category = 1,
        Channel = 2,
        Role = 4,

        // specific types
        Fleet = 128,
        Interaction = 256,
        BotManager = 512
    }
}
