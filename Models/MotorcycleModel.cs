using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMotorcycleBot.Models
{
    internal class MotorcycleModel
    {
        public required int Id { get; set; }
        [MaxLength(50)]
        public required string Manufacturer { get; set; }
        [MaxLength(50)]
        public required string Model { get; set; }
        public required int BuildYear { get; set; }
        public required int UserId { get; set; }
        public UserModel User { get; set; }

    }
}
