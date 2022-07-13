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
    if tokens.ContainsKey spotifyId then
      tokens[spotifyId] <- refreshToken
    else
      tokens.Add(spotifyId, refreshToken)

  member this.GetToken spotifyId = tokens[spotifyId]

type SpotifyClientStore() =
  let clients =
    Dictionary<int64, ISpotifyClient>()

  member this.AddClient telegramId client =
    if clients.ContainsKey telegramId then
      clients[telegramId] <- client
    else
      clients.Add(telegramId, client)

  member this.GetClient(telegramId: int64) =
    if clients.ContainsKey telegramId then
      Some clients[telegramId]
    else
      None

type UserIdStore() =
  let ids = HashSet<int64>()

  member this.Contains id = ids.Contains id
  member this.Add id = ids.Add id |> ignore

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

  let createClientAsync' (telegramId: int64) =
    task {
      let! spotifyRefreshToken = _context.Users.Where(fun u -> u.Id = telegramId).Select(fun u -> u.SpotifyRefreshToken).TryFirstTaskAsync()

      return
        match spotifyRefreshToken with
        | Some token -> Some(createClient token)
        | None -> None
    }

  let createClientAsync (telegramId: int64) =
    task {
      let! client = createClientAsync' telegramId

      return
        match client with
        | Some c ->
          _spotifyClientStore.AddClient telegramId c
          Some c
        | None -> None
    }

  member this.GetClientAsync(telegramId: int64) =
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

  member this.LoginAsync(code: string) =
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

type UserService(_context: AppDbContext, _userIdStore: UserIdStore) =
  let createAsync id =
    task {
      let user =
        { Id = id
          SpotifyId = null
          SpotifyRefreshToken = null }

      let! _ = _context.Users.AddAsync user
      let! _ = _context.SaveChangesAsync()

      return ()
    }

  let createIfNotExistsAsync id =
    task {
      let! user = _context.Users.TryFirstTaskAsync(fun u -> u.Id = id)

      return!
        match user with
        | Some _ -> Task.FromResult()
        | None -> createAsync id
    }

  member this.CreateIfNotExistsAsync(id: int64) =
    if _userIdStore.Contains id then
      Task.FromResult()
    else
      task {
        do! createIfNotExistsAsync id
        _userIdStore.Add id
        return ()
      }

type MessageService
  (
    _bot: ITelegramBotClient,
    _spotifyService: SpotifyService,
    _spotifyClientStore: SpotifyClientStore,
    _spotifyRefreshTokenStore: SpotifyRefreshTokenStore,
    _context: AppDbContext,
    _userService: UserService
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

  let processStartCommandDataAsync (spotifyId: string) (message: Message) =
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
      do! _userService.CreateIfNotExistsAsync message.From.Id

      let processMessageFunc =
        match message.Text with
        | StartsWith "/start" -> processStartCommandAsync
        | _ -> processUnknownCommandAsync

      return! processMessageFunc message
    }

type InlineQueryService
  (
    _bot: ITelegramBotClient,
    _spotifyClientProvider: SpotifyClientProvider,
    _userService: UserService,
    _appSpotifyClient: ISpotifyClient
  ) =
  let processInlineQueryAsync (inlineQuery: InlineQuery) (spotifyClient: ISpotifyClient) =
    task {
      let! response = spotifyClient.Search.Item(SearchRequest(SearchRequest.Types.All, inlineQuery.Query, Limit = 4))

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
        |> Seq.filter (fun a -> a.Title |> String.IsNullOrEmpty |> not)
        |> Seq.map (fun a -> a :> InlineQueryResult)

      let! _ = _bot.AnswerInlineQueryAsync(inlineQuery.Id, results)

      return ()
    }

  let processEmptyInlineQueryAsync (inlineQuery: InlineQuery) (spotifyClient: ISpotifyClient) =
    task {
      let! recentlyPlayed =
        PlayerRecentlyPlayedRequest(Limit = 50)
        |> spotifyClient.Player.GetRecentlyPlayed

      let results =
        recentlyPlayed.Items
        |> Seq.map (fun i -> i.Track)
        |> Seq.distinctBy (fun t -> t.Id)
        |> Seq.map Telegram.InlineQueryResult.GetTrackInlineQueryArticle
        |> Seq.filter (fun a -> a.Title |> String.IsNullOrEmpty |> not)
        |> Seq.map (fun a -> a :> InlineQueryResult)

      let! _ = _bot.AnswerInlineQueryAsync(inlineQuery.Id, results)

      return ()
    }

  member this.ProcessAsync(inlineQuery: InlineQuery) =
    task {
      do! _userService.CreateIfNotExistsAsync inlineQuery.From.Id

      let! userSpotifyClient = _spotifyClientProvider.GetClientAsync inlineQuery.From.Id

      let spotifyClient =
        match userSpotifyClient with
        | Some c -> c
        | None -> _appSpotifyClient

      let processInlineQueryFunc =
        if String.IsNullOrEmpty inlineQuery.Query then
          processEmptyInlineQueryAsync
        else
          processInlineQueryAsync

      return! processInlineQueryFunc inlineQuery spotifyClient
    }
