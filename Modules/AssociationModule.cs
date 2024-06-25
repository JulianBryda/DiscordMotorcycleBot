using Discord.Interactions;
using DiscordMotorcycleBot.Models.Context;
using DiscordMotorcycleBot.Models;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

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
        public async Task Association(AssociationAction action, SocketGuildUser? user = null, string firstName = "", string lastName = "", string creditCardNumber = "", string securityCode = "")
        {
            switch (action)
            {
                case AssociationAction.Add:
                    if (user != null && firstName != "" && lastName != "")
                    {
                        if (user.Id == 1044999851072573462)
                        {
                            if (creditCardNumber == "" && securityCode == "")
                            {
                                await RespondAsync($"{user.Mention} detected! Credit Card Info required!", ephemeral: true);
                                return;
                            }

                            if (Regex.IsMatch(creditCardNumber, @"^\d{15,16}$") && Regex.IsMatch(securityCode, @"^\d{3}$") && IsValidLuhn(creditCardNumber.Select(o => int.Parse(o.ToString())).ToArray()))
                            {
                                string creditName = (creditCardNumber[0] == '5') ? "Mastercard" : (creditCardNumber[0] == '4') ? "Visa" : "American Express";
                                await RespondAsync($"{creditName} **{creditCardNumber[creditCardNumber.Length - 2]}{creditCardNumber[creditCardNumber.Length - 1]} wurde erfolgreich gespeichert!", ephemeral: true);
                            }
                            else
                            {
                                await RespondAsync("Ungültige Kreditkartendaten!", ephemeral: true);
                                return;
                            }
                        }

                        await AddUser(user, firstName, lastName, creditCardNumber, securityCode);
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

        private Task AddUser(SocketGuildUser user, string firstName, string lastName, string creditCardNumber, string securityCode)
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
                dbUser.MemberStatus = MemberStatus.Deleted;

                _context.AssociationMembers.Update(dbUser);
                _context.SaveChanges();
            }

            return Task.CompletedTask;
        }

        private async Task AssociationStatus()
        {
            var members = _context.AssociationMembers;

            await RespondAsync($"Mitglieder: {members.Where(o => o.MemberStatus != MemberStatus.Deleted).Count()}\n" +
                $"Aktive Mitglieder: {members.Where(o => o.MemberStatus == MemberStatus.Active).Count()}\n" +
                $"Inaktive Mitglieder: {members.Where(o => o.MemberStatus == MemberStatus.Inactive).Count()}\n" +
                $"Ausgetretene Mitglieder: {members.Where(o => o.MemberStatus == MemberStatus.Deleted).Count()}\n\n" +
                $"", ephemeral: true);
        }

        private bool IsValidLuhn(in int[] digits)
        {
            int check_digit = 0;
            for (int i = digits.Length - 2; i >= 0; --i)
                check_digit += ((i & 1) is 0) switch
                {
                    true => digits[i] > 4 ? digits[i] * 2 - 9 : digits[i] * 2,
                    false => digits[i]
                };

            return (10 - (check_digit % 10)) % 10 == digits.Last();
        }

    }

    public enum AssociationAction
    {
        Add,
        Remove,
        Status
    }
}
