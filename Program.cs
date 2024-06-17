using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMotorcycleBot.Handler;
using DiscordMotorcycleBot.Models;
using DiscordMotorcycleBot.Models.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace DiscordMotorcycleBot
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            ConfigModel.EnsureCreated();

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config/config.json")
                .Build();

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                services
                .AddSingleton(config)
                .AddDbContext<DatabaseContext>()
                .AddLogging(log =>
                {
                    // logging options

                })
                .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.All,
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(x => new CommandService())
                .AddSingleton<MessageHandler>()
                )
                .Build();

            await RunAsync(host);
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var client = provider.GetRequiredService<DiscordSocketClient>();
            var config = provider.GetRequiredService<IConfigurationRoot>();

            await provider.GetRequiredService<DatabaseContext>().Database.EnsureCreatedAsync();

            var slashCommands = provider.GetRequiredService<InteractionService>();
            await provider.GetRequiredService<InteractionHandler>().InitAsync();

            await provider.GetRequiredService<MessageHandler>().InitAsync();

            client.Log += async (LogMessage msg) => { Console.WriteLine(msg.Message); };
            slashCommands.Log += async (LogMessage msg) => { Console.WriteLine(msg.Message); };

            client.Ready += async () =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await slashCommands.RegisterCommandsToGuildAsync(ulong.Parse(config["DevGuild"]));
                    Console.WriteLine("Bot ready in Dev!");
                }
                else
                {
                    await slashCommands.RegisterCommandsToGuildAsync(ulong.Parse(config["ProdGuild"]));
                    Console.WriteLine("Bot ready in Prod!");
                }
            };


            await client.LoginAsync(TokenType.Bot, config["Token"]);
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
