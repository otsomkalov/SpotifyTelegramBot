module Bot.Startup

open System
open System.Reflection
open Bot.Helpers
open Bot.Services
open Bot.Services.Spotify
open Bot.Services.Telegram
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Microsoft.Extensions.Options
open MongoDB.ApplicationInsights
open MongoDB.Driver
open SpotifyAPI.Web
open Telegram.Bot
open Microsoft.Azure.Functions.Worker
open otsom.fs.Telegram.Bot
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Telegram.Bot.Auth.Spotify.Settings
open MongoDB.ApplicationInsights.DependencyInjection

#nowarn "20"

let configureTelegram (provider: IServiceProvider) =
  let settings =
    provider
      .GetRequiredService<IOptions<Settings.Telegram.T>>()
      .Value

  TelegramBotClient(settings.Token) :> ITelegramBotClient

let configureDefaultSpotifyClient (options: IOptions<SpotifySettings>) =
  let settings = options.Value

  let config =
    SpotifyClientConfig
      .CreateDefault()
      .WithAuthenticator(ClientCredentialsAuthenticator(settings.ClientId, settings.ClientSecret))

  SpotifyClient(config) :> ISpotifyClient

let private configureMongoClient (factory: IMongoClientFactory) (options: IOptions<Settings.DatabaseSettings>) =
  let settings = options.Value

  factory.GetClient(settings.ConnectionString)

let private configureMongoDatabase (options: IOptions<Settings.DatabaseSettings>) (mongoClient: IMongoClient) =
  let settings = options.Value

  mongoClient.GetDatabase(settings.Name)

let configureServices (builderContext: HostBuilderContext) (services: IServiceCollection) : unit =
  let configuration = builderContext.Configuration

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights();

  services.Configure<Settings.Telegram.T>(configuration.GetSection(Settings.Telegram.SectionName))
  services.Configure<Settings.DatabaseSettings>(configuration.GetSection(Settings.DatabaseSettings.SectionName))

  services.AddMongoClientFactory()
  services.BuildSingleton<IMongoClient, IMongoClientFactory, IOptions<Settings.DatabaseSettings>>(configureMongoClient)
  services.BuildSingleton<IMongoDatabase, IOptions<Settings.DatabaseSettings>, IMongoClient>(configureMongoDatabase)

  services
    .AddSingleton<ITelegramBotClient>(configureTelegram)
    .BuildSingleton<ISpotifyClient, IOptions<SpotifySettings>>(configureDefaultSpotifyClient)

  services
    .AddSingleton<SpotifyRefreshTokenStore>()
    .AddSingleton<SpotifyClientStore>()

    .AddScoped<SpotifyClientProvider>()
    .AddScoped<MessageService>()
    .AddScoped<InlineQueryService>()

  services
  |> Startup.addTelegramBotCore
  |> Auth.Spotify.Startup.addTelegramBotSpotifyAuthCore configuration
  |> Auth.Spotify.Mongo.Startup.addMongoSpotifyAuth

  services.AddMvcCore().AddNewtonsoftJson()

  ()

let private configureLogging (builder: ILoggingBuilder) =
  builder.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  ()

let configureAppConfiguration _ (configBuilder: IConfigurationBuilder) =

  configBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true)

  ()

let host =
  HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(configureAppConfiguration)
    .ConfigureLogging(configureLogging)
    .ConfigureServices(configureServices)
    .Build()

host.Run()