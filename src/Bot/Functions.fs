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
open otsom.fs.Extensions
open otsom.fs.Telegram.Bot.Auth.Spotify
open Helpers.IQueryCollection

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

type Spotify(_telegramOptions: IOptions<Settings.Telegram.T>, fulfillAuth: Auth.Fulfill) =
  let _telegramSettings = _telegramOptions.Value

  [<Function("ProcessCallbackAsync")>]
  member this.HandleCallbackAsync
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "spotify/callback")>] request: HttpRequest)
    =
    let onSuccess (state: string) =
      RedirectResult($"{_telegramSettings.BotUrl}?start={state}", true) :> IActionResult

    let onError error =
      match error with
      | Auth.StateNotFound -> BadRequestObjectResult("State not found in the cache") :> IActionResult

    match request.Query["state"], request.Query["code"] with
    | QueryParam state, QueryParam code -> fulfillAuth state code |> TaskResult.either onSuccess onError
    | QueryParam _, _ -> BadRequestObjectResult("Code is empty") :> IActionResult |> Task.FromResult
    | _, QueryParam _ -> BadRequestObjectResult("State is empty") :> IActionResult |> Task.FromResult
    | _, _ ->
      BadRequestObjectResult("State and code are empty") :> IActionResult
      |> Task.FromResult
