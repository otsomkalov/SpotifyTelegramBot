namespace Bot.Functions

open System.Threading.Tasks
open Bot.Services
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.Extensions.Logging
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums

type Telegram(_messageService: MessageService, _inlineQueryService: InlineQueryService) =
    [<FunctionName("ProcessUpdateAsync")>]
    member this.ProcessUpdateAsync
        ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "update")>] update: Update)
        (logger: ILogger)
        =
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
