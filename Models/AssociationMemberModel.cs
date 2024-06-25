using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMotorcycleBot.Models
{
    public class AssociationMemberModel
    {
        public required int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public ulong? DiscordId { get; set; }
        public required DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public required MemberStatus MemberStatus { get; set; }
    }

    public enum MemberStatus
    {
        Active,
        Inactive,
        Deleted
    }
}
