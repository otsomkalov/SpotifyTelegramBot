namespace Bot.Functions

open System.Threading.Tasks
open Bot
open Bot.Services
open Bot.Services.Spotify
open Bot.Services.Telegram
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums
open Microsoft.Azure.Functions.Worker.Http

type Telegram(_messageService: MessageService, _inlineQueryService: InlineQueryService, logger: ILogger<Telegram>) =
  [<Function("ProcessUpdateAsync")>]
  member this.ProcessUpdateAsync
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "telegram/update")>] request: HttpRequest, [<FromBody>] update: Update)
    =
    task {
      try
        let processUpdateTask =
          match update.Type with
          | UpdateType.Message -> _messageService.ProcessAsync(update.Message)
          | UpdateType.InlineQuery -> _inlineQueryService.ProcessAsync(update.InlineQuery)
          | _ -> Task.FromResult()

        do! processUpdateTask
      with e ->
        logger.LogError(e, "Error during processing update:")
    }

type Spotify(_spotifyService: SpotifyService, _telegramOptions: IOptions<Settings.Telegram.T>) =
  let _telegramSettings = _telegramOptions.Value

  [<Function("ProcessCallbackAsync")>]
  member this.HandleCallbackAsync
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "spotify/callback")>] request: HttpRequest)
    =
    task {
      let code = request.Query["code"]

      let! spotifyId = _spotifyService.LoginAsync code

      return RedirectResult($"{_telegramSettings.BotUrl}?start={spotifyId}", true)
    }
