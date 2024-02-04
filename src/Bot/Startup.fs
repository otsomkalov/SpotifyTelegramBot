module Bot.Startup

open System
open System.Reflection
open Bot.Data
open Bot.Helpers
open Bot.Services
open Bot.Services.Spotify
open Bot.Services.Telegram
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open SpotifyAPI.Web
open Telegram.Bot
open Microsoft.Azure.Functions.Worker

#nowarn "20"

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
      .GetRequiredService<IOptions<Settings.Database.T>>()
      .Value

  builder.UseNpgsql(settings.ConnectionString)

  builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)

  ()

let configureServices (builderContext: HostBuilderContext) (services: IServiceCollection) : unit =
  let configuration = builderContext.Configuration

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights();

  (services, configuration)
  |> Startup.ConfigureAndValidate<Settings.Telegram.T> Settings.Telegram.SectionName
  |> Startup.ConfigureAndValidate<Settings.Spotify.T> Settings.Spotify.SectionName
  |> Startup.ConfigureAndValidate<Settings.Database.T> Settings.Database.SectionName

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

  services.AddMvcCore().AddNewtonsoftJson()

  ()

let configureAppConfiguration _ (configBuilder: IConfigurationBuilder) =

  configBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true)

  ()

let host =
  HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(configureAppConfiguration)
    .ConfigureServices(configureServices)
    .Build()

host.Run()