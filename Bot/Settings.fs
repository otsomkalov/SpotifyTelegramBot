module Bot.Settings

open Microsoft.FSharp.Core

module TelegramSettings =
  [<Literal>]
  let SectionName = "Telegram"

  [<CLIMutable>]
  type T = { Token: string }

module SpotifySettings =
  [<Literal>]
  let SectionName = "Spotify"

  [<CLIMutable>]
  type T =
    { ClientId: string
      ClientSecret: string }
