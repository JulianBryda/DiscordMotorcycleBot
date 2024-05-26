
namespace DiscordMotorcycleBot.Models
{
    public class SavedRole
    {
        public required int Id { get; set; }
        public required ulong RoleId { get; set; }  
        public required RoleType RoleType { get; set; }
    }

    public enum RoleType
    {
        BotManager
    }
}
