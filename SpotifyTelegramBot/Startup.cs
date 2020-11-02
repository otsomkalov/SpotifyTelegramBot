using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyAPI.Web;
using SpotifyTelegramBot.Services;
using SpotifyTelegramBot.Services.Interfaces;
using SpotifyTelegramBot.Settings;
using Telegram.Bot;

namespace SpotifyTelegramBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = _configuration.Get<AppSettings>();

            ITelegramBotClient bot = new TelegramBotClient(appSettings.Token);

            services.AddSingleton(bot)
                .AddSingleton<ISpotifyAuthService, SpotifyAuthService>()
                .AddSingleton(provider => new SpotifyWebAPI())
                .AddSingleton(appSettings)
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IInlineQueryService, InlineQueryService>();

            services.AddControllers().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
