module Bot.Services.Telegram

open Bot.Data
open Bot.Services.Spotify
open Resources
open System
open System.Collections.Generic
open System.Threading.Tasks
open SpotifyAPI.Web
open Telegram.Bot
open Telegram.Bot.Types
open Bot.Helpers
open Bot.Helpers.String
open Telegram.Bot.Types.InlineQueryResults
open Telegram.Bot.Types.ReplyMarkups
open EntityFrameworkCore.FSharp.DbContextHelpers

type MessageService
  (
    _bot: ITelegramBotClient,
    _spotifyService: SpotifyService,
    _spotifyClientStore: SpotifyClientStore,
    _spotifyRefreshTokenStore: SpotifyRefreshTokenStore,
    _context: AppDbContext
  ) =
  let processEmptyStartCommandAsync (message: Message) =
    task {
      let markup =
        seq {
          seq {
            InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(Message.Search)
            InlineKeyboardButton.WithSwitchInlineQuery(Message.Share)
          }

          seq { InlineKeyboardButton.WithUrl("Login", _spotifyService.GetLoginUrl()) }
        }
        |> InlineKeyboardMarkup

      let! _ = _bot.SendTextMessageAsync(ChatId(message.From.Id), Message.Welcome, replyMarkup = markup)

      return ()
    }

  let createUserAsync telegramId spotifyId refreshToken =
    task {
      let user =
        { Id = telegramId
          SpotifyId = spotifyId
          SpotifyRefreshToken = refreshToken }

      let! _ = _context.Users.AddAsync user
      let! _ = _context.SaveChangesAsync()

      ()
    }

  let updateUserAsync user refreshToken =
    task {
      let updatedUser =
        { user with SpotifyRefreshToken = refreshToken }

      _context.Users.Update(updatedUser) |> ignore
      let! _ = _context.SaveChangesAsync()

      ()
    }

  let processStartCommandDataAsync spotifyId (message: Message) =
    task {
      let refreshToken =
        _spotifyRefreshTokenStore.GetToken spotifyId

      let! user = _context.Users.TryFirstTaskAsync(fun u -> u.Id = message.From.Id)

      let processUserFunc =
        match user with
        | Some u -> updateUserAsync u
        | None -> createUserAsync message.From.Id spotifyId

      do! processUserFunc refreshToken

      let! _ = _bot.SendTextMessageAsync(ChatId(message.Chat.Id), "You've successfully logged in!")

      ()
    }

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
      let! userSpotifyClient = _spotifyClientProvider.GetClientAsync inlineQuery.From.Id

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