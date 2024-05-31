using Discord.Commands;
using Discord.Interactions;

namespace DiscordMotorcycleBot.Modules
{
    public class UtilityModule : InteractionModuleBase<SocketInteractionContext>
    {
        #region Ping Command
        [Discord.Commands.RequireRole("Bot-Manager")]
        [SlashCommand("ping", "Ping Message!")]
        public async Task HandlePing()
        {
            await RespondAsync("Pong!");
        }
        #endregion

        #region Say Command
        [Discord.Commands.RequireRole("Bot-Manager")]
        [SlashCommand("say", "Lass den Bot sprechen!")]
        public async Task HandleSay([Remainder] string text)
        {
            await RespondAsync(text);
        }
        #endregion
    }
}
