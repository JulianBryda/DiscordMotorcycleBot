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
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
            var logger = provider.GetRequiredService<ILogger<Program>>();

            await provider.GetRequiredService<DatabaseContext>().Database.EnsureCreatedAsync();

            var slashCommands = provider.GetRequiredService<InteractionService>();
            await provider.GetRequiredService<InteractionHandler>().InitAsync();

            await provider.GetRequiredService<MessageHandler>().InitAsync();

            client.Log += (LogMessage msg) => { return Handle_Log(msg, logger); };
            slashCommands.Log += (LogMessage msg) => { return Handle_Log(msg, logger); };

            client.Ready += async () =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await slashCommands.RegisterCommandsToGuildAsync(ulong.Parse(config["DevGuild"]));
                    logger.LogInformation("Commands registered to Dev!");
                }
                else
                {
                    await slashCommands.RegisterCommandsToGuildAsync(ulong.Parse(config["ProdGuild"]));
                    logger.LogInformation("Commands registered to Prod!");
                }
            };


            await client.LoginAsync(TokenType.Bot, config["Token"]);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Handle_Log(LogMessage arg, ILogger<Program> logger)
        {
            if (arg.Exception != null)
            {
                logger.Log(TranslateSeverityToLevel(arg.Severity), "{StackTrace}\n{ExceptionMessage}\n{Message}", arg.Exception.StackTrace, arg.Exception.Message, arg.Message);
            }
            else
            {
                logger.Log(TranslateSeverityToLevel(arg.Severity), "{Message}", arg.Message);
            }

            return Task.CompletedTask;
        }

        private LogLevel TranslateSeverityToLevel(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                    return LogLevel.Critical;
                case LogSeverity.Error:
                    return LogLevel.Error;
                case LogSeverity.Warning:
                    return LogLevel.Warning;
                case LogSeverity.Info:
                    return LogLevel.Information;
                case LogSeverity.Verbose:
                    return LogLevel.Trace;
                case LogSeverity.Debug:
                    return LogLevel.Debug;
                default:
                    return LogLevel.None;
            }
        }
    }
}
