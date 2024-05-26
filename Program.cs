using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMotorcycleBot.Handler;
using DiscordMotorcycleBot.Models.Context;
using DiscordMotorcycleBot.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordMotorcycleBot
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json")
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
                Console.WriteLine("Bot ready!");
                await slashCommands.RegisterCommandsToGuildAsync(UInt64.Parse(config["testGuild"]));
            };


            await client.LoginAsync(TokenType.Bot, config["tokens:discord"]);
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
