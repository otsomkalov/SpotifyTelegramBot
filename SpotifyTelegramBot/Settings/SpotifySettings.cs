using SpotifyTelegramBot.Settings.Interfaces;

namespace SpotifyTelegramBot.Settings
{
    public class SpotifySettings : ISpotifySettings
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
