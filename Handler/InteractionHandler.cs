using Discord.Interactions;
using Discord.WebSocket;
using DiscordMotorcycleBot.Models.Context;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DiscordMotorcycleBot.Handler
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private readonly ulong _interactionChannelId;
        private readonly ulong _guildId;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services, DatabaseContext context, IConfigurationRoot config, ILogger<InteractionHandler> logger)
        {
            _client = client;
            _commands = commands;
            _services = services;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _guildId = ulong.Parse(config["DevGuild"]);
                logger.LogInformation("Listening for Commands in Dev!");
            }
            else
            {
                _guildId = ulong.Parse(config["ProdGuild"]);
                logger.LogInformation("Listening for Commands in Prod!");
            }

            var channel = context.DiscordEntities.FirstOrDefault(o => o.EntityType.HasFlag(Models.EntityType.Interaction));
            if (channel != null)
            {
                _interactionChannelId = channel.EntityId;
            }
        }

        public async Task InitAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            if (_guildId != arg.GuildId) return;

            if (arg.ChannelId != _interactionChannelId && ((SocketSlashCommand)arg).CommandName != "install" && ((SocketSlashCommand)arg).CommandName != "say")
            {
                await ((SocketSlashCommand)arg).RespondAsync($"Commands kannst du nur in diesem Channel nutzen: <#{_interactionChannelId}>", ephemeral: true);
                return;
            }

            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
