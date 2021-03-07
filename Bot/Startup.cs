using Bot.Services;
using Bot.Services.Interfaces;
using Bot.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyAPI.Web;
using Telegram.Bot;

namespace Bot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = _configuration.Get<AppSettings>();

            services
                .AddSingleton(settings.Spotify)
                .AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(settings.Token))
                .AddSingleton<ISpotifyAuthService, SpotifyAuthService>()
                .AddSingleton(_ => new SpotifyWebAPI())
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IInlineQueryService, InlineQueryService>();
            
            services.AddControllers().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}