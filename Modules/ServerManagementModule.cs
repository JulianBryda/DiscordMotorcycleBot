using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMotorcycleBot.Models;
using DiscordMotorcycleBot.Models.Context;
using Microsoft.Extensions.Logging;

namespace DiscordMotorcycleBot.Modules
{
    public class ServerManagementModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<MotorcycleModule> _logger;

        public ServerManagementModule(DatabaseContext context, ILogger<MotorcycleModule> logger)
        {
            _context = context;
            _logger = logger;
        }

        [SlashCommand("create", "Erstellt alle notwendigen Kategorien, Channels und Rollen!")]
        public async Task HandleChannelCreation()
        {
            var guild = Context.Guild;
            if (guild == null) return;

            if (guild.CategoryChannels.Any(o => o.Name.Equals("features", StringComparison.CurrentCultureIgnoreCase)))
            {
                await RespondAsync("Die Kategorie \"Features\" existiert bereits!");
                return;
            }

            if (await guild.CreateCategoryChannelAsync("Features") is not ICategoryChannel category)
            {
                Console.WriteLine("Failed to create category \"Features\"!");
                return;
            }


            // roles
            await CreateRoles(guild);

            // channels
            await CreateFleetChannel(guild, category);
            await CreateBotInteractionChannel(guild, category);


            _context.SaveChanges();

            await RespondAsync("Alle Kategorien, Channles und Rollen wurden erstellt!");
        }


        private async Task CreateRoles(SocketGuild guild)
        {
            if (await guild.CreateRoleAsync("Bot-Manager", color: Color.LightGrey) is not IRole role)
            {
                _logger.LogError("Failed to create role \"Bot-Manager\"!");
                return;
            }

            _context.SavedRoles.Add(new SavedRole()
            {
                Id = 0,
                RoleId = role.Id,
                RoleType = RoleType.BotManager
            });

            _context.SaveChanges();
        }

        private async Task CreateFleetChannel(SocketGuild guild, ICategoryChannel category)
        {
            if (await guild.CreateTextChannelAsync("Fuhrpark", prop => prop.CategoryId = category.Id) is not IMessageChannel channel)
            {
                Console.WriteLine("Failed to create channel \"Fuhrpark\"!");
                return;
            }

            await channel.SendMessageAsync("# __Übersicht aller eingetragenen Motorräder__");

            _context.SavedChannels.Add(new SavedChannel()
            {
                Id = 0,
                ChannelId = channel.Id,
                ChannelType = Models.ChannelType.Fleet
            });
        }

        private async Task CreateBotInteractionChannel(SocketGuild guild, ICategoryChannel category)
        {
            if (await guild.CreateTextChannelAsync("Interaktionen", prop => ConfigureBotInteractionChannel(prop, category)) is not IMessageChannel channel)
            {
                Console.WriteLine("Failed to create channel \"Interaktionen\"!");
                return;
            }

            _context.SavedChannels.Add(new SavedChannel()
            {
                Id = 0,
                ChannelId = channel.Id,
                ChannelType = Models.ChannelType.BotInteraction
            });
        }

        private void ConfigureBotInteractionChannel(TextChannelProperties prop, ICategoryChannel category)
        {
            if (_context.SavedRoles.FirstOrDefault(o => o.RoleType == RoleType.BotManager) is not SavedRole savedRole)
            {
                _logger.LogCritical("Couldn't find role with RoleType.BotManager in Database!");
                return;
            }

            List<Overwrite> overwrites = new();

            var everyonePerm = new OverwritePermissions(
                sendMessages: PermValue.Deny,
                readMessageHistory: PermValue.Allow
                );

            var saveRolePerm = new OverwritePermissions(
                sendMessages: PermValue.Allow,
                readMessageHistory: PermValue.Allow
                );

            overwrites.Add(new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, everyonePerm));
            overwrites.Add(new Overwrite(savedRole.RoleId, PermissionTarget.Role, saveRolePerm));


            prop.CategoryId = category.Id;
            prop.PermissionOverwrites = overwrites;
        }
    }
}
