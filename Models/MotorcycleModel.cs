using System.ComponentModel.DataAnnotations;

namespace DiscordMotorcycleBot.Models
{
    public class MotorcycleModel
    {
        public required int Id { get; set; }
        [MaxLength(50)]
        public required string Manufacturer { get; set; }
        [MaxLength(50)]
        public required string Model { get; set; }
        public required int BuildYear { get; set; }
        public required ulong DiscordId { get; set; }

    }
}
