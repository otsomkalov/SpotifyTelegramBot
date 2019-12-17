using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using SpotifyTelegramBot;
using SpotifyTelegramBot.Services;
using SpotifyTelegramBot.Services.Interfaces;
using SpotifyTelegramBot.Settings;
using SpotifyTelegramBot.Settings.Interfaces;
using Telegram.Bot;

[assembly: WebJobsStartup(typeof(Startup))]

namespace SpotifyTelegramBot
{
    internal class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            ISpotifySettings spotifySettings = new SpotifySettings();

            config.Bind("Spotify", spotifySettings);

            ITelegramBotClient bot = new TelegramBotClient(config["Token"]);

            builder.Services
                .AddSingleton(bot)
                .AddSingleton<ISpotifyAuthService, SpotifyAuthService>()
                .AddSingleton(provider => new SpotifyWebAPI())
                .AddSingleton(spotifySettings)
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IInlineQueryService, InlineQueryService>();
        }
    }
}
