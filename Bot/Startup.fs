namespace Bot

open System
open System.Reflection
open Bot.Data
open Bot.Services
open Bot.Settings
open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open SpotifyAPI.Web
open Telegram.Bot

#nowarn "20"

type Startup() =
  inherit FunctionsStartup()

  let configureTelegram (provider: IServiceProvider) =
    let settings =
      provider
        .GetRequiredService<IOptions<TelegramSettings.T>>()
        .Value

    TelegramBotClient(settings.Token) :> ITelegramBotClient

  let configureSpotify (provider: IServiceProvider) =
    let settings =
      provider
        .GetRequiredService<IOptions<SpotifySettings.T>>()
        .Value

    let config =
      SpotifyClientConfig
        .CreateDefault()
        .WithAuthenticator(ClientCredentialsAuthenticator(settings.ClientId, settings.ClientSecret))

    SpotifyClient(config) :> ISpotifyClient

  let configureDbContext (provider: IServiceProvider) (builder: DbContextOptionsBuilder) =
    let settings =
      provider
        .GetRequiredService<IOptions<DatabaseSettings.T>>()
        .Value

    builder.UseNpgsql(settings.ConnectionString)

    builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)

    ()

  override this.Configure(builder: IFunctionsHostBuilder) : unit =
    let configuration =
      builder.GetContext().Configuration

    builder
      .Services
      .Configure<TelegramSettings.T>(configuration.GetSection(TelegramSettings.SectionName))
      .Configure<SpotifySettings.T>(configuration.GetSection(SpotifySettings.SectionName))
      .Configure<DatabaseSettings.T>(configuration.GetSection(DatabaseSettings.SectionName))

    builder.Services.AddDbContext<AppDbContext>(configureDbContext)

    builder
      .Services
      .AddSingleton<ITelegramBotClient>(configureTelegram)
      .AddSingleton<ISpotifyClient>(configureSpotify)

    builder
      .Services
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
