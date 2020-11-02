using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using SpotifyTelegramBot;
using SpotifyTelegramBot.Services;
using SpotifyTelegramBot.Services.Interfaces;
using SpotifyTelegramBot.Settings;
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

            var settings = config.Get<AppSettings>();

            builder.Services
                .AddSingleton(settings.Spotify)
                .AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(settings.Token))
                .AddSingleton<ISpotifyAuthService, SpotifyAuthService>()
                .AddSingleton(provider => new SpotifyWebAPI())
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IInlineQueryService, InlineQueryService>();
        }
    }
}