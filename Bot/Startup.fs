namespace Bot

open System
open Bot.Services
open Bot.Settings
open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection
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

  override this.Configure(builder: IFunctionsHostBuilder) : unit =
    let configuration =
      builder.GetContext().Configuration

    builder
      .Services
      .Configure<TelegramSettings.T>(configuration.GetSection(TelegramSettings.SectionName))
      .Configure<SpotifySettings.T>(configuration.GetSection(SpotifySettings.SectionName))

    builder
      .Services
      .AddSingleton<ITelegramBotClient>(configureTelegram)
      .AddSingleton<ISpotifyClient>(configureSpotify)

    builder
      .Services
      .AddSingleton<MessageService>()
      .AddSingleton<InlineQueryService>()

    ()

[<assembly: FunctionsStartup(typeof<Startup>)>]
do ()
