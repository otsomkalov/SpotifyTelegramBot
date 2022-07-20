namespace Bot.Functions

open System.Threading.Tasks
open Bot
open Bot.Services
open Bot.Services.Spotify
open Bot.Services.Telegram
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums

type Telegram(_messageService: MessageService, _inlineQueryService: InlineQueryService) =
  [<FunctionName("ProcessUpdateAsync")>]
  member this.ProcessUpdateAsync ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "telegram/update")>] update: Update) (logger: ILogger) =
    task {
      let processUpdateTask =
        match update.Type with
        | UpdateType.Message -> _messageService.ProcessAsync(update.Message)
        | UpdateType.InlineQuery -> _inlineQueryService.ProcessAsync(update.InlineQuery)
        | _ -> Task.FromResult()

      try
        do! processUpdateTask
      with
      | e -> logger.LogError(e, "Error during processing update:")
    }

type Spotify(_spotifyService: SpotifyService, _telegramOptions: IOptions<Settings.Telegram.T>) =
  let _telegramSettings = _telegramOptions.Value

  [<FunctionName("ProcessCallbackAsync")>]
  member this.HandleCallbackAsync([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "spotify/callback")>] request: HttpRequest) (logger: ILogger) =
    task{
      let code = request.Query["code"]

      let! spotifyId = _spotifyService.LoginAsync code

      return RedirectResult($"{_telegramSettings.BotUrl}?start={spotifyId}", true)
    }