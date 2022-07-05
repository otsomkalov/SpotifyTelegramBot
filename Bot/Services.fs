module Bot.Services

open Resources
open System.Threading.Tasks
open SpotifyAPI.Web
open Telegram.Bot
open Telegram.Bot.Types
open Helpers
open Telegram.Bot.Types.InlineQueryResults

type MessageService(_bot: ITelegramBotClient) =
  let processStartCommandAsync (message: Message) =
    task {
      let! _ =
        _bot.SendTextMessageAsync(
          ChatId(message.From.Id),
          Message.Welcome,
          replyMarkup = Telegram.InlineKeyboardMarkup.StartKeyboardMarkup
        )

      return ()
    }

  let processUnknownCommandAsync (message: Message) =
    task {
      let! _ = _bot.SendTextMessageAsync(ChatId(message.From.Id), "Unsupported command")

      return ()
    }

  member this.ProcessAsync(message: Message) =
    let processMessageTask =
      match message.Text with
      | StartsWith "/start" -> processStartCommandAsync
      | _ -> processUnknownCommandAsync

    processMessageTask message

type InlineQueryService(_bot: ITelegramBotClient, _spotifyClient: ISpotifyClient) =
  let processInlineQueryAsync (inlineQuery: InlineQuery) =
    task {
      let! response = _spotifyClient.Search.Item(SearchRequest(SearchRequest.Types.All, inlineQuery.Query, Limit = 4))

      let tracks =
        response.Tracks.Items
        |> Seq.map Telegram.InlineQueryResult.GetTrackInlineQueryArticle

      let albums =
        response.Albums.Items
        |> Seq.map Telegram.InlineQueryResult.GetAlbumInlineQueryArticle

      let artists =
        response.Artists.Items
        |> Seq.map Telegram.InlineQueryResult.GetArtistInlineQueryArticle

      let playlists =
        response.Playlists.Items
        |> Seq.map Telegram.InlineQueryResult.GetPlaylistInlineQueryArticle

      let results =
        [ tracks; albums; artists; playlists ]
        |> Seq.collect id
        |> Seq.filter (fun a -> a.Title |> System.String.IsNullOrEmpty |> not)
        |> Seq.map (fun a -> a :> InlineQueryResult)

      let! _ = _bot.AnswerInlineQueryAsync(inlineQuery.Id, results)

      return ()
    }

  member this.ProcessAsync(inlineQuery: InlineQuery) =
    let processInlineQueryFunc =
      if
        System.String.IsNullOrEmpty(inlineQuery.Query)
        |> not
      then
        processInlineQueryAsync
      else
        fun _ -> Task.FromResult()

    processInlineQueryFunc inlineQuery
