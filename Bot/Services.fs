namespace Bot.Services

open Bot.Data
open Resources
open System
open System.Collections.Generic
open Bot.Settings
open Microsoft.Extensions.Options
open System.Threading.Tasks
open SpotifyAPI.Web
open Telegram.Bot
open Telegram.Bot.Types
open Bot.Helpers
open Bot.Helpers.String
open Telegram.Bot.Types.InlineQueryResults
open Telegram.Bot.Types.ReplyMarkups
open EntityFrameworkCore.FSharp.DbContextHelpers
open System.Linq

type SpotifyRefreshTokenStore() =
  let tokens = Dictionary<string, string>()

  member this.AddToken spotifyId refreshToken =
    match tokens.ContainsKey spotifyId with
    | true -> tokens[spotifyId] <- refreshToken
    | false -> tokens.Add(spotifyId, refreshToken)

  member this.GetToken spotifyId = tokens[spotifyId]

type SpotifyClientStore() =
  let clients =
    Dictionary<int64, ISpotifyClient>()

  member this.AddClient telegramId client =
    match clients.ContainsKey telegramId with
    | true -> clients[telegramId] <- client
    | false -> clients.Add(telegramId, client)

  member this.GetClient telegramId =
    match clients.ContainsKey telegramId with
    | true -> Some clients[telegramId]
    | false -> None

type SpotifyClientProvider(_context: AppDbContext, _spotifyClientStore: SpotifyClientStore, _spotifyOptions: IOptions<SpotifySettings.T>) =
  let _spotifySettings = _spotifyOptions.Value

  let createClient refreshToken =
    let tokenResponse =
      AuthorizationCodeTokenResponse(CreatedAt = DateTime.UtcNow.AddSeconds(-86400), RefreshToken = refreshToken)

    let spotifyClientConfig =
      AuthorizationCodeAuthenticator(_spotifySettings.ClientId, _spotifySettings.ClientSecret, tokenResponse)
      |> SpotifyClientConfig
        .CreateDefault()
        .WithAuthenticator

    SpotifyClient(spotifyClientConfig) :> ISpotifyClient

  let createClientAsync' telegramId =
    task {
      let! spotifyRefreshToken =
        _context
          .Users
          .Where(fun u -> u.Id = telegramId)
          .Select(fun u -> u.SpotifyRefreshToken)
          .TryFirstTaskAsync()

      return
        match spotifyRefreshToken with
        | Some token -> Some(createClient token)
        | None -> None
    }

  let createClientAsync telegramId =
    task {
      let! client = createClientAsync' telegramId

      return
        match client with
        | Some c ->
          _spotifyClientStore.AddClient telegramId c
          Some c
        | None -> None
    }

  member this.GetClientAsync telegramId =
    match _spotifyClientStore.GetClient telegramId with
    | Some client -> Task.FromResult(Some client)
    | None -> createClientAsync telegramId

type SpotifyService
  (
    _spotifyOptions: IOptions<SpotifySettings.T>,
    _context: AppDbContext,
    _spotifyRefreshTokenStore: SpotifyRefreshTokenStore
  ) =
  let _spotifySettings = _spotifyOptions.Value

  member this.GetLoginUrl() =
    let scopes =
      [ Scopes.UserReadRecentlyPlayed
        Scopes.UserLibraryRead ]
      |> List<string>

    let loginRequest =
      LoginRequest(_spotifySettings.CallbackUrl, _spotifySettings.ClientId, LoginRequest.ResponseType.Code, Scope = scopes)

    loginRequest.ToUri().ToString()

  member this.LoginAsync code =
    task {
      let! tokenResponse =
        (_spotifySettings.ClientId, _spotifySettings.ClientSecret, code, _spotifySettings.CallbackUrl)
        |> AuthorizationCodeTokenRequest
        |> OAuthClient().RequestToken

      let spotifyClient =
        (_spotifySettings.ClientId, _spotifySettings.ClientSecret, tokenResponse)
        |> AuthorizationCodeAuthenticator
        |> SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator
        |> SpotifyClient

      let! spotifyUserProfile = spotifyClient.UserProfile.Current()

      _spotifyRefreshTokenStore.AddToken spotifyUserProfile.Id tokenResponse.RefreshToken

      return spotifyUserProfile.Id
    }

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
