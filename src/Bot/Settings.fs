[<RequireQualifiedAccess>]
module Bot.Settings

open System.ComponentModel.DataAnnotations
open Microsoft.FSharp.Core

module Telegram =
  [<Literal>]
  let SectionName = "Telegram"

  [<CLIMutable>]
  type T =
    { [<Required>]
      Token: string
      [<Required>]
      BotUrl: string }

[<CLIMutable>]
type DatabaseSettings =
  { ConnectionString: string
    Name: string }

  static member SectionName = "Database"
