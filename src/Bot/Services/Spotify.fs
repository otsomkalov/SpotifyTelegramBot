module Bot.Services.Spotify

open System
open System.Collections.Generic
open Microsoft.Extensions.Options
open System.Threading.Tasks
open SpotifyAPI.Web
open otsom.fs.Telegram.Bot.Auth.Spotify.Settings
open otsom.fs.Telegram.Bot.Auth.Spotify.Workflows
open otsom.fs.Telegram.Bot.Core

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


type SpotifyClientProvider(loadCompletedAuth: Completed.Load, _spotifyOptions: IOptions<SpotifySettings>) =
  let settings = _spotifyOptions.Value
  let createClientFromTokenResponse =
    fun response ->
      let authenticator =
        AuthorizationCodeAuthenticator(settings.ClientId, settings.ClientSecret, response)

      let retryHandler =
        SimpleRetryHandler(RetryAfter = TimeSpan.FromSeconds(30), RetryTimes = 3, TooManyRequestsConsumesARetry = true)

      let config =
        SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator(authenticator)
          .WithRetryHandler(retryHandler)

      config |> SpotifyClient :> ISpotifyClient

  let _clientsByTelegramId =
    Dictionary<int64, ISpotifyClient>()

  member this.GetAsync userId : Task<ISpotifyClient option> =
    let userId' = userId |> UserId.value

    if _clientsByTelegramId.ContainsKey(userId') then
      _clientsByTelegramId[userId'] |> Some |> Task.FromResult
    else
      task {
        let! auth = loadCompletedAuth userId

        return!
          match auth with
          | None -> Task.FromResult None
          | Some auth ->
            let client =
              AuthorizationCodeTokenResponse(RefreshToken = auth.Token)
              |> createClientFromTokenResponse

            this.SetClient(userId', client)

            client |> Some |> Task.FromResult
      }

  member this.SetClient(telegramId: int64, client: ISpotifyClient) =
    if _clientsByTelegramId.ContainsKey(telegramId) then
      ()
    else
      (telegramId, client) |> _clientsByTelegramId.Add