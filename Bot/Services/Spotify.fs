module Bot.Services.Spotify

open Bot
open Bot.Data
open System
open System.Collections.Generic
open Microsoft.Extensions.Options
open System.Threading.Tasks
open SpotifyAPI.Web
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


type SpotifyClientProvider(_context: AppDbContext, _spotifyClientStore: SpotifyClientStore, _spotifyOptions: IOptions<Settings.Spotify.T>) =
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
    _spotifyOptions: IOptions<Settings.Spotify.T>,
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