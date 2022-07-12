namespace Bot.Settings

open System
open Microsoft.FSharp.Core

module TelegramSettings =
  [<Literal>]
  let SectionName = "Telegram"

  [<CLIMutable>]
  type T = { Token: string; BotUrl: string }

module SpotifySettings =
  [<Literal>]
  let SectionName = "Spotify"

  [<CLIMutable>]
  type T =
    { ClientId: string
      ClientSecret: string
      CallbackUrl: Uri }

module DatabaseSettings =
  [<Literal>]
  let SectionName = "Database"

  [<CLIMutable>]
  type T = { ConnectionString: string }
