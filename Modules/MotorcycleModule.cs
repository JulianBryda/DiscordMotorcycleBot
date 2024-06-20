using Discord;
using Discord.Interactions;
using DiscordMotorcycleBot.Models;
using DiscordMotorcycleBot.Models.Context;
using Microsoft.Extensions.Logging;

namespace DiscordMotorcycleBot.Modules
{
    public class MotorcycleModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<MotorcycleModule> _logger;
        private readonly ulong _fleetChannelId;

        public MotorcycleModule(DatabaseContext context, ILogger<MotorcycleModule> logger)
        {
            _context = context;
            _logger = logger;

            if (context.DiscordEntities.FirstOrDefault(o => o.EntityType.HasFlag(EntityType.Fleet)) is DiscordEntity channel)
            {
                _fleetChannelId = channel.EntityId;
            }
            else
            {
                _logger.LogWarning("SavedChannel \"Fleet\" not found in Database!");
            }
        }


        #region Save Motorcycle Command
        [RequireRole("Bot-Manager")]
        [SlashCommand("motorcycle", "Speichere dein Motorrad!")]
        public async Task HandleMotorcycleInfoCommand(string arg = "")
        {
            if (arg == "refresh")
            {
                await UpdateMessage();

                await RespondAsync("Refreshed Fleet List!", ephemeral: true);
            }
            else
            {
                var formButton = new ButtonBuilder()
                {
                    Label = "Formular öffnen",
                    CustomId = "motorcycle_button",
                    Style = ButtonStyle.Primary
                };

                var component = new ComponentBuilder()
                .WithButton(formButton);

                await RespondAsync("Hier kannst du ein oder mehrere Motorräder hinzufügen :wink:\n" +
                    $"Deine gespeicherten Motorräder können von anderen Mitgliedern im Channel <#{_fleetChannelId}> eingesehen werden!", components: component.Build());
            }
        }

        [ComponentInteraction("motorcycle_button")]
        public async Task HandleFormButtonInput()
        {
            await RespondWithModalAsync<MotorcycleModal>("motorcycle_save_modal");
        }

        [ModalInteraction("motorcycle_save_modal")]
        public async Task HandleModalInput(MotorcycleModal modal)
        {
            _context.Motorcycles.Add(new MotorcycleModel()
            {
                Id = 0,
                Manufacturer = modal.Manufacturer,
                Model = modal.Model,
                BuildYear = modal.BuildYear,
                DiscordId = Context.User.Id
            });

            _context.SaveChanges();

            _ = UpdateMessage();

            await RespondAsync("Dein Motorrad wurde gespeichert!\n" +
                $"**{modal.Manufacturer} {modal.Model} {modal.BuildYear}**", ephemeral: true);
        }
        #endregion

        private async Task UpdateMessage()
        {
            if (_context.DiscordEntities.FirstOrDefault(o => o.EntityType.HasFlag(EntityType.Fleet)) is not DiscordEntity savedChannel)
            {
                _logger.LogError("Couldn't find channel with ChannelType.Fleet in Database!");
                return;
            }

            if (await Context.Client.GetChannelAsync(savedChannel.EntityId) is not IMessageChannel channel)
            {
                Console.WriteLine($"Failed to get Channel with id \"{savedChannel.EntityId}\"!");
                return;
            }

            var messages = await channel.GetMessagesAsync(20).FlattenAsync();
            int messageIndex = messages.Count() - 1;

            string newContent = "# __Übersicht aller eingetragenen Motorräder__\n\n";
            ulong lastUser = 0;

            foreach (var item in _context.Motorcycles.OrderBy(o => o.DiscordId))
            {
                if (lastUser != item.DiscordId)
                {
                    if (newContent.Length > 1800)
                    {
                        if (messageIndex >= 0)

                        {
                            await channel.ModifyMessageAsync(messages.ElementAt(messageIndex).Id, msg => msg.Content = newContent);
                            messageIndex--;
                        }
                        else
                        {
                            await channel.SendMessageAsync(newContent);
                        }
                        newContent = "";
                    }

                    newContent += $"### Motorräder von <@{item.DiscordId}>\n";
                }

                newContent += $"\t- {item.Manufacturer} {item.Model} {item.BuildYear}\n";

                lastUser = item.DiscordId;
            }

            if (messageIndex >= 0)
            {
                await channel.ModifyMessageAsync(messages.ElementAt(messageIndex).Id, msg => msg.Content = newContent);
            }
            else
            {
                await channel.SendMessageAsync(newContent);
            }
        }
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
