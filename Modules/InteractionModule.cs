using Discord;
using Discord.Commands;
using Discord.Interactions;
using DiscordMotorcycleBot.Models;
using DiscordMotorcycleBot.Models.Context;

namespace DiscordMotorcycleBot.Modules
{
    public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        #region Ping Command
        [RequireRole("BotModerator")]
        [SlashCommand("ping", "Ping Message!")]
        public async Task HandlePing()
        {
            await RespondAsync("Pong!");
        }
        #endregion

        #region Save Motorcycle Command
        [RequireRole("BotModerator")]
        [SlashCommand("motorcycle", "Speichere dein Motorrad!")]
        public async Task HandleSaveMotorcycle()
        {
            var button = new ButtonBuilder()
            {
                Label = "Formular öffnen",
                CustomId = "motorcycle_button",
                Style = ButtonStyle.Primary
            };

            var component = new ComponentBuilder()
            .WithButton(button);

            await RespondAsync("Hier kannst du ein oder mehrere Motorräder hinzufügen :D\n" +
                "Deine gespeicherten Motorräder können von anderen Mitgliedern eingesehen werden! " +
                "Solltest du das nicht wollen, änder dies bitte in deinen Benutzer Einstellungen mit dem Command\"/user\"", components: component.Build());
        }

        [ComponentInteraction("motorcycle_button")]
        public async Task HandleFormButtonInput()
        {
            await RespondWithModalAsync<MotorcycleModal>("motorcycle_save_modal");
        }

        [ModalInteraction("motorcycle_save_modal")]
        public async Task HandleModalInput(MotorcycleModal modal)
        {
            using DatabaseContext context = new();
            UserModel? user = context.Users.Where(o => o.DiscordId == Context.User.Id).FirstOrDefault();
            if (user == null)
            {
                user = new UserModel()
                {
                    Id = 0,
                    DiscordId = Context.User.Id,
                    MotorcycleShareOption = ShareOption.Public
                };

                context.Users.Add(user);
                context.SaveChanges();
            }

            context.Motorcycles.Add(new MotorcycleModel()
            {
                Id = 0,
                Manufacturer = modal.Manufacturer,
                Model = modal.Model,
                BuildYear = modal.BuildYear,
                UserId = user.Id
            });
            context.SaveChanges();

            await RespondAsync("Dein Motorrad wurde gespeichert!\n" +
                $"**{modal.Manufacturer} {modal.Model} {modal.BuildYear}**");
        }
        #endregion

        #region Say Command
        [SlashCommand("say", "Lass den Bot sprechen!")]
        public async Task HandleSay([Remainder] string text)
        {
            await RespondAsync(text);
        }
        #endregion
    }

    public class MotorcycleModal : IModal
    {
        public string Title => "Motorrad Form";
        [InputLabel("Marke")]
        [ModalTextInput("manufacturer_input", TextInputStyle.Short, maxLength: 50)]
        public required string Manufacturer { get; set; }
        [InputLabel("Modell")]
        [ModalTextInput("model_input", TextInputStyle.Short, maxLength: 50)]
        public required string Model { get; set; }
        [InputLabel("Baujahr")]
        [ModalTextInput("build_year_input", TextInputStyle.Short, minLength: 4, maxLength: 4)]
        public required int BuildYear { get; set; }
    }
}
