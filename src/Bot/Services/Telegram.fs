module Bot.Services.Telegram

open Bot.Services.Spotify
open Microsoft.Extensions.Options
open Resources
open System
open System.Collections.Generic
open System.Threading.Tasks
open SpotifyAPI.Web
open Telegram.Bot
open Telegram.Bot.Types
open Bot.Helpers
open Bot.Helpers.String
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.InlineQueryResults
open Telegram.Bot.Types.ReplyMarkups
open otsom.fs.Extensions
open otsom.fs.Telegram.Bot.Auth.Spotify
open otsom.fs.Telegram.Bot.Auth.Spotify.Settings
open otsom.fs.Telegram.Bot.Core

type SendMessage = string -> Task<unit>
type ReplyToMessage = string -> Task<unit>

let sendMessage (bot: ITelegramBotClient) userId : SendMessage =
  fun text ->
    bot.SendTextMessageAsync((userId |> UserId.value |> ChatId), text, parseMode = ParseMode.MarkdownV2)
    |> Task.map ignore

let replyToMessage (bot: ITelegramBotClient) userId (messageId: int) : ReplyToMessage =
  fun text ->
    bot.SendTextMessageAsync(
      (userId |> UserId.value |> ChatId),
      text,
      parseMode = ParseMode.MarkdownV2,
      replyToMessageId = messageId
    )
    |> Task.map ignore

type MessageService
  (
    _bot: ITelegramBotClient,
    _spotifyClientStore: SpotifyClientStore,
    _spotifyRefreshTokenStore: SpotifyRefreshTokenStore,
    initAuth: Auth.Init,
    completeAuth: Auth.Complete,
    _spotifyOptions: IOptions<SpotifySettings>
  ) =
  let spotifySettings = _spotifyOptions.Value

  let processEmptyStartCommandAsync (message: Message) =
    task {
      let! loginLink = initAuth (message.From.Id |> UserId) [ Scopes.UserReadRecentlyPlayed; Scopes.UserLibraryRead ]

      let markup =
        seq {
          seq {
            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(Message.Search)
            InlineKeyboardButton.WithSwitchInlineQuery(Message.Share)
          }

          seq { InlineKeyboardButton.WithUrl("Login", loginLink) }
        }
        |> InlineKeyboardMarkup

      let! _ = _bot.SendTextMessageAsync(ChatId(message.From.Id), Message.Welcome, replyMarkup = markup)

      return ()
    }


  let processStartCommandDataAsync state (message: Message) =
    let userId = message.From.Id |> UserId

    let processSuccessfulLogin =
      fun () ->
        _bot.SendTextMessageAsync(ChatId(message.From.Id), "You've successfully logged in!") |> Task.map ignore

    let sendErrorMessage =
      function
      | Auth.CompleteError.StateNotFound ->
        _bot.SendTextMessageAsync(ChatId(message.From.Id), "State not found. Try to login via fresh link.", replyToMessageId = message.MessageId)
        |> Task.map ignore
      | Auth.CompleteError.StateDoesntBelongToUser ->
        _bot.SendTextMessageAsync(ChatId(message.From.Id), "State provided does not belong to your login request. Try to login via fresh link.", replyToMessageId = message.MessageId)
        |> Task.map ignore

    completeAuth userId state
    |> TaskResult.taskEither processSuccessfulLogin sendErrorMessage

  let processStartCommandAsync (message: Message) =
    let processMessageFunc =
      match message.Text with
      | CommandData data -> processStartCommandDataAsync data
      | _ -> processEmptyStartCommandAsync

    processMessageFunc message

  let processUnknownCommandAsync (message: Message) =
    task {
      let! _ = _bot.SendTextMessageAsync(ChatId(message.From.Id), "Unsupported command")

      return ()
    }

  member this.ProcessAsync(message: Message) =
    task {
      let processMessageFunc =
        match message.Text with
        | StartsWith "/start" -> processStartCommandAsync
        | _ -> processUnknownCommandAsync

      return! processMessageFunc message
    }

type InlineQueryService(_bot: ITelegramBotClient, _spotifyClientProvider: SpotifyClientProvider, _appSpotifyClient: ISpotifyClient) =
  let processAnonymousInlineQueryAsync (inlineQuery: InlineQuery) =
    task {
      let! response = _appSpotifyClient.Search.Item(SearchRequest(SearchRequest.Types.All, inlineQuery.Query, Limit = 4))

      let tracks =
        response.Tracks.Items
        |> Seq.map Telegram.InlineQueryResult.FromTrackFromAnonymousUser

      let albums =
        response.Albums.Items
        |> Seq.map Telegram.InlineQueryResult.FromAlbumForAnonymousUser

      let artists =
        response.Artists.Items
        |> Seq.map Telegram.InlineQueryResult.FromArtist

      let playlists =
        response.Playlists.Items
        |> Seq.map Telegram.InlineQueryResult.FromPlaylist

      return
        [ tracks; albums; artists; playlists ]
        |> Seq.collect id
    }

  let processUserInlineQueryAsync (client: ISpotifyClient) (inlineQuery: InlineQuery) =
    task {
      let! response = client.Search.Item(SearchRequest(SearchRequest.Types.All, inlineQuery.Query, Limit = 4))

      let! likedTracks =
        response.Tracks.Items
        |> Seq.map (fun i -> i.Id)
        |> List
        |> LibraryCheckTracksRequest
        |> client.Library.CheckTracks

      let tracks =
        Seq.zip response.Tracks.Items likedTracks
        |> Seq.map (fun (track, liked) -> Telegram.InlineQueryResult.FromTrackForUser track liked)

      let! likedAlbums =
        response.Albums.Items
        |> Seq.map (fun i -> i.Id)
        |> List
        |> LibraryCheckAlbumsRequest
        |> client.Library.CheckAlbums

      let albums =
        Seq.zip response.Albums.Items likedAlbums
        |> Seq.map (fun (album, liked) -> Telegram.InlineQueryResult.FromAlbumForUser album liked)

      let artists =
        response.Artists.Items
        |> Seq.map Telegram.InlineQueryResult.FromArtist

      let playlists =
        response.Playlists.Items
        |> Seq.map Telegram.InlineQueryResult.FromPlaylist

      return
        [ tracks; albums; artists; playlists ]
        |> Seq.collect id
    }

  let processEmptyInlineQueryAsync (spotifyClient: ISpotifyClient) =
    task {
      let! recentlyPlayed =
        PlayerRecentlyPlayedRequest(Limit = 50)
        |> spotifyClient.Player.GetRecentlyPlayed

      let recentlyPlayedTracks =
        recentlyPlayed.Items
        |> Seq.map (fun i -> i.Track)
        |> Seq.distinctBy (fun t -> t.Id)
        |> Seq.toList

      let! areTracksLiked =
        recentlyPlayedTracks
        |> List.map (fun t -> t.Id)
        |> List
        |> LibraryCheckTracksRequest
        |> spotifyClient.Library.CheckTracks

      return
        Seq.zip recentlyPlayedTracks areTracksLiked
        |> Seq.map (fun (track, liked) -> Telegram.InlineQueryResult.FromTrackForUser track liked)
    }

  member this.ProcessAsync(inlineQuery: InlineQuery) =
    task {
      let userId = inlineQuery.From.Id |> UserId

      let! userSpotifyClient = _spotifyClientProvider.GetAsync userId

      let! results =
        match userSpotifyClient, String.IsNullOrEmpty inlineQuery.Query with
        | Some client, true -> processEmptyInlineQueryAsync client
        | Some client, false -> processUserInlineQueryAsync client inlineQuery
        | None, true -> Task.FromResult Seq.empty
        | _ -> processAnonymousInlineQueryAsync inlineQuery

      let filteredResults =
        results
        |> Seq.distinctBy (fun a -> a.Id)
        |> Seq.map (fun a -> a :> InlineQueryResult)

      let! _ = _bot.AnswerInlineQueryAsync(inlineQuery.Id, filteredResults, 0)

      return ()
    }