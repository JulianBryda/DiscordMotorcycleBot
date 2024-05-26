using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace DiscordMotorcycleBot.Handler
{
    public class MessageHandler
    {
        private readonly DiscordSocketClient _client;

        public MessageHandler(DiscordSocketClient client)
        {
            _client = client;
        }
         
        public async Task InitAsync()
        {
            // TODO not needed for now!
            // _client.MessageReceived += MessageReceivedAsync;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            Console.WriteLine(message.Content);
        }
    }
}
