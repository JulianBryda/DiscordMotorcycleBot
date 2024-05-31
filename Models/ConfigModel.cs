using Newtonsoft.Json;

namespace DiscordMotorcycleBot.Models
{
    public class ConfigModel
    {
        public string Token { get; set; } = string.Empty;
        public ulong DevGuild { get; set; }
        public ulong ProdGuild { get; set; }


        public static void EnsureCreated()
        {
            Directory.CreateDirectory(AppContext.BaseDirectory + "config");

            if (!File.Exists(AppContext.BaseDirectory + "config/config.json"))
            {
                File.WriteAllText(AppContext.BaseDirectory + "config/config.json", JsonConvert.SerializeObject(new ConfigModel()));
            }
        }
    }
}
