using Discord.Interactions;
using DiscordMotorcycleBot.Models.Context;
using DiscordMotorcycleBot.Models;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;

namespace DiscordMotorcycleBot.Modules
{
    public class AssociationModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<AssociationModule> _logger;

        public AssociationModule(DatabaseContext context, ILogger<AssociationModule> logger)
        {
            _context = context;
            _logger = logger;
        }

        [SlashCommand("association", "test")]
        public async Task Association(AssociationAction action, SocketGuildUser? user = null, string firstName = "", string lastName = "")
        {
            switch (action)
            {
                case AssociationAction.Add:
                    if (user != null && firstName != "" && lastName != "")
                    {
                        await AddUser(user, firstName, lastName);
                        await RespondAsync($"Added {user.Mention} to the association!", ephemeral: true);
                    }
                    else
                    {
                        await RespondAsync("Use this format to add a user to the association: /association add @user [firstname] [lastname]", ephemeral: true);
                    }
                    break;
                case AssociationAction.Remove:
                    if (user != null)
                    {
                        await RemoveUser(user);
                        await RespondAsync($"Removed {user.Mention} from the association!", ephemeral: true);
                    }
                    else
                    {
                        await RespondAsync("Use this format to remove a user from the association: /association remove @user", ephemeral: true);
                    }
                    break;
                case AssociationAction.Status:
                    await AssociationStatus();
                    break;
                default:
                    await RespondAsync("Invalid action. Use 'add', 'remove' or 'status'!");
                    break;
            }
        }

        private Task AddUser(SocketGuildUser user, string firstName, string lastName)
        {
            _context.AssociationMembers.Add(new AssociationMemberModel()
            {
                Id = 0,
                FirstName = firstName,
                LastName = lastName,
                DiscordId = user.Id,
                JoinedAt = DateTime.UtcNow,
                MemberStatus = MemberStatus.Active
            });
            _context.SaveChanges();

            return Task.CompletedTask;
        }

        private Task RemoveUser(SocketGuildUser user)
        {
            var dbUser = _context.AssociationMembers.FirstOrDefault(o => o.DiscordId == user.Id);
            if (dbUser != null)
            {
                _context.AssociationMembers.Remove(dbUser);
                _context.SaveChanges();
            }

            return Task.CompletedTask;
        }

        private async Task AssociationStatus()
        {
            var members = _context.AssociationMembers.OrderByDescending(o => o.JoinedAt);

            await RespondAsync($"Total Members: {members.Count()}");
        }


    }

    public enum AssociationAction
    {
        Add,
        Remove,
        Status
    }
}
