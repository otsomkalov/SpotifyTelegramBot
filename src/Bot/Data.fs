namespace Bot.Data

open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Core

[<CLIMutable>]
  type User = { Id: int64; SpotifyId: string; SpotifyRefreshToken: string }

type AppDbContext(options: DbContextOptions) =
  inherit DbContext(options)

  member val Users = base.Set<User>()
