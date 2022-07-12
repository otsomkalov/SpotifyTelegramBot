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
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.DbContextHelpers

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
    Dictionary<int64, SpotifyClient>()

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

    SpotifyClient(spotifyClientConfig)

  let createClientAsync' (telegramId: int64) =
    task {
      let! user = _context.Users.FirstOrDefaultAsync(fun u -> u.Id = telegramId)

      let spotifyClient =
        createClient user.SpotifyRefreshToken


      return spotifyClient
    }

  let createClientAsync (telegramId: int64) =
    task {
      let! client = createClientAsync' telegramId

      _spotifyClientStore.AddClient telegramId client

      return client
    }

  member this.GetClientAsync(telegramId: int64) =
    match _spotifyClientStore.GetClient telegramId with
    | Some client -> Task.FromResult client
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
    let processMessageFunc =
      match message.Text with
      | StartsWith "/start" -> processStartCommandAsync
      | _ -> processUnknownCommandAsync

    processMessageFunc message

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
        |> Seq.filter (fun a -> a.Title |> String.IsNullOrEmpty |> not)
        |> Seq.map (fun a -> a :> InlineQueryResult)

      let! _ = _bot.AnswerInlineQueryAsync(inlineQuery.Id, results)

      return ()
    }

  member this.ProcessAsync(inlineQuery: InlineQuery) =
    let processInlineQueryFunc =
      if String.IsNullOrEmpty(inlineQuery.Query) |> not then
        processInlineQueryAsync
      else
        fun _ -> Task.FromResult()

    processInlineQueryFunc inlineQuery
