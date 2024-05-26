using Discord.Interactions;
using Discord.WebSocket;
using DiscordMotorcycleBot.Models.Context;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Reflection;

namespace DiscordMotorcycleBot.Handler
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private readonly ulong _interactionChannelId;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services, DatabaseContext context)
        {
            _client = client;
            _commands = commands;
            _services = services;

            var channel = context.SavedChannels.FirstOrDefault(o => o.ChannelType == Models.ChannelType.BotInteraction);
            if (channel != null)
            {
                _interactionChannelId = channel.ChannelId;
            }
        }

        public async Task InitAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            if (arg.ChannelId != _interactionChannelId && ((SocketSlashCommand)arg).CommandName != "setup")
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
