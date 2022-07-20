namespace Bot

open System
open System.Reflection
open Bot.Data
open Bot.Helpers
open Bot.Services
open Bot.Services.Spotify
open Bot.Services.Telegram
open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open SpotifyAPI.Web
open Swan
open Telegram.Bot

#nowarn "20"

type Startup() =
  inherit FunctionsStartup()

  let configureTelegram (provider: IServiceProvider) =
    let settings =
      provider
        .GetRequiredService<IOptions<Settings.Telegram.T>>()
        .Value

    TelegramBotClient(settings.Token) :> ITelegramBotClient

  let configureSpotify (provider: IServiceProvider) =
    let settings =
      provider
        .GetRequiredService<IOptions<Settings.Spotify.T>>()
        .Value

    let config =
      SpotifyClientConfig
        .CreateDefault()
        .WithAuthenticator(ClientCredentialsAuthenticator(settings.ClientId, settings.ClientSecret))

    SpotifyClient(config) :> ISpotifyClient

  let configureDbContext (provider: IServiceProvider) (builder: DbContextOptionsBuilder) =
    let settings =
      provider
        .GetRequiredService<IOptions<Settings.DatabaseSettings.T>>()
        .Value

    builder.UseNpgsql(settings.ConnectionString)

    builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)

    ()

  override this.Configure(builder: IFunctionsHostBuilder) : unit =
    let configuration =
      builder.GetContext().Configuration
    let services = builder.Services

    (services, configuration)
    |> Startup.ConfigureAndValidate<Settings.Telegram.T> Settings.Telegram.SectionName
    |> Startup.ConfigureAndValidate<Settings.Spotify.T> Settings.Spotify.SectionName
    |> Startup.ConfigureAndValidate<Settings.DatabaseSettings.T> Settings.DatabaseSettings.SectionName

    services.AddDbContext<AppDbContext>(configureDbContext)

    services
      .AddSingleton<ITelegramBotClient>(configureTelegram)
      .AddSingleton<ISpotifyClient>(configureSpotify)

    services
      .AddSingleton<SpotifyRefreshTokenStore>()
      .AddSingleton<SpotifyClientStore>()

      .AddScoped<SpotifyClientProvider>()
      .AddScoped<SpotifyService>()
      .AddScoped<MessageService>()
      .AddScoped<InlineQueryService>()

    ()

  override this.ConfigureAppConfiguration(builder: IFunctionsConfigurationBuilder) =

    builder.ConfigurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true)

    ()

[<assembly: FunctionsStartup(typeof<Startup>)>]
do ()
