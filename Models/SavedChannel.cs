
namespace DiscordMotorcycleBot.Models
{
    public class SavedChannel
    {
        public required int Id { get; set; }
        public required ulong ChannelId { get; set; }  
        public required ChannelType ChannelType { get; set; }
    }

    public enum ChannelType 
    {
        Fleet,
        BotInteraction
    }
}
