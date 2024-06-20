using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMotorcycleBot.Models;
using DiscordMotorcycleBot.Models.Context;
using Microsoft.Extensions.Logging;
using System.Data;

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

        [SlashCommand("uninstall", "Löscht alle vom Bot erstellten Kategorien, Channles und Rollen!")]
        public async Task HandleUninstall()
        {
            var guild = Context.Guild;
            if (guild == null) return;

            var entities = _context.DiscordEntities.Where(o => o.GuildId == guild.Id);

            foreach (var item in entities)
            {
                if (item.EntityType.HasFlag(EntityType.Category))
                {
                    await guild.GetCategoryChannel(item.EntityId).DeleteAsync();
                }
                else if (item.EntityType.HasFlag(EntityType.Channel))
                {
                    await guild.GetTextChannel(item.EntityId).DeleteAsync();
                }
                else if (item.EntityType.HasFlag(EntityType.Role))
                {
                    await guild.GetRole(item.EntityId).DeleteAsync();
                }
            }

            _context.DiscordEntities.RemoveRange(entities);
            _context.SaveChanges();

            await RespondAsync("Alle Kategorien, Channels und Rollen wurden gelöscht!", ephemeral: true);
        }

        [SlashCommand("install", "Erstellt alle notwendigen Kategorien, Channels und Rollen!")]
        public async Task HandleInstall()
        {
            var guild = Context.Guild;
            if (guild == null) return;

            var entities = _context.DiscordEntities.Where(o => o.GuildId == guild.Id);
            _context.DiscordEntities.RemoveRange(entities);

            // category
            ICategoryChannel? category = await CreateCategory(guild);
            if (category == null) return;

            // roles
            await CreateRole(guild);

            // channels
            await CreateFleetChannel(guild, category);
            await CreateBotInteractionChannel(guild, category);

            _context.SaveChanges();

            await RespondAsync("Alle Kategorien, Channels und Rollen wurden erstellt!");
        }


        private async Task<ICategoryChannel?> CreateCategory(SocketGuild guild)
        {
            ICategoryChannel category;

            if (guild.CategoryChannels.FirstOrDefault(o => o.Name.Equals("features", StringComparison.CurrentCultureIgnoreCase)) is not ICategoryChannel cat)
            {
                category = await guild.CreateCategoryChannelAsync("features");

                if (category == null)
                {
                    await RespondAsync("Failed to create category \"Features\"!", ephemeral: true);
                    return null;
                }
            }
            else
            {
                category = cat;
            }

            _context.DiscordEntities.Add(new DiscordEntity()
            {
                Id = 0,
                EntityId = category.Id,
                EntityType = EntityType.Category,
                GuildId = guild.Id
            });

            return category;
        }

        private async Task CreateRole(SocketGuild guild)
        {
            IRole role;

            if (guild.Roles.FirstOrDefault(o => o.Name.Equals("Bot-Manager")) is not IRole rol)
            {
                role = await guild.CreateRoleAsync("Bot-Manager", color: Color.LightGrey);

                if (role == null)
                {
                    await RespondAsync("Failed to create role \"Bot-Manager\"!", ephemeral: true);
                    return;
                }
            }
            else
            {
                role = rol;
            }

            _context.DiscordEntities.Add(new DiscordEntity()
            {
                Id = 0,
                EntityId = role.Id,
                EntityType = EntityType.Role | EntityType.BotManager,
                GuildId = guild.Id
            });

            _context.SaveChanges();
        }

        private async Task CreateFleetChannel(SocketGuild guild, ICategoryChannel category)
        {
            IMessageChannel channel;

            if (guild.TextChannels.FirstOrDefault(o => o.Name.Equals("Fuhrpark", StringComparison.CurrentCultureIgnoreCase) && o.CategoryId == category.Id) is not IMessageChannel chan)
            {
                channel = await guild.CreateTextChannelAsync("Fuhrpark", prop => ConfigureFleetChannel(prop, category));

                if (channel == null)
                {
                    await RespondAsync("Failed to create channel \"Fuhrpark\"!", ephemeral: true);
                    return;
                }

                await channel.SendMessageAsync("# __Übersicht aller eingetragenen Motorräder__");
            }
            else
            {
                channel = chan;
            }

            _context.DiscordEntities.Add(new DiscordEntity()
            {
                Id = 0,
                EntityId = channel.Id,
                EntityType = EntityType.Channel | EntityType.Fleet,
                GuildId = guild.Id
            });
        }

        private async Task CreateBotInteractionChannel(SocketGuild guild, ICategoryChannel category)
        {
            IMessageChannel channel;

            if (guild.TextChannels.FirstOrDefault(o => o.Name.Equals("Interaktionen", StringComparison.CurrentCultureIgnoreCase) && o.CategoryId == category.Id) is not IMessageChannel chan)
            {
                channel = await guild.CreateTextChannelAsync("Interaktionen", prop => ConfigureBotInteractionChannel(prop, category));

                if (channel == null)
                {
                    await RespondAsync("Failed to create channel \"Interaktionen\"!", ephemeral: true);
                    return;
                }
            }
            else
            {
                channel = chan;
            }

            _context.DiscordEntities.Add(new DiscordEntity()
            {
                Id = 0,
                EntityId = channel.Id,
                EntityType = EntityType.Channel | EntityType.Interaction,
                GuildId = guild.Id
            });
        }

        private void ConfigureFleetChannel(TextChannelProperties prop, ICategoryChannel category)
        {
            if (_context.DiscordEntities.FirstOrDefault(o => o.EntityType.HasFlag(EntityType.Role)) is not DiscordEntity savedRole)
            {
                _logger.LogCritical("Couldn't find role with RoleType.BotManager in Database!");
                return;
            }

            List<Overwrite> overwrites = new();

            var everyonePerm = new OverwritePermissions(
                sendMessages: PermValue.Deny,
                readMessageHistory: PermValue.Allow
                );

            var botRolePerm = new OverwritePermissions(
                sendMessages: PermValue.Allow,
                readMessageHistory: PermValue.Allow
                );

            overwrites.Add(new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, everyonePerm));
            if (Context.Guild.Roles.FirstOrDefault(o => o.Name == "MotorcycleBot") is SocketRole botRole)
            {
                overwrites.Add(new Overwrite(botRole.Id, PermissionTarget.Role, botRolePerm));
            }
            else
            {
                _logger.LogError("Failed tot assign bot to Fleet channel!");
            }

            prop.CategoryId = category.Id;
            prop.PermissionOverwrites = overwrites;
        }

        private void ConfigureBotInteractionChannel(TextChannelProperties prop, ICategoryChannel category)
        {
            if (_context.DiscordEntities.FirstOrDefault(o => o.EntityType.HasFlag(EntityType.Role)) is not DiscordEntity savedRole)
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
            overwrites.Add(new Overwrite(savedRole.EntityId, PermissionTarget.Role, saveRolePerm));


            prop.CategoryId = category.Id;
            prop.PermissionOverwrites = overwrites;
        }
    }
}
