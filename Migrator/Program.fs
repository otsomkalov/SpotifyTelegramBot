open System
open Bot.Data
open Bot.Settings
open Microsoft.AspNetCore.Builder
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
let configureDbContext (provider: IServiceProvider) (builder: DbContextOptionsBuilder) =
  let settings =
    provider
      .GetRequiredService<IOptions<DatabaseSettings.T>>()
      .Value

  builder.UseNpgsql(settings.ConnectionString) |> ignore

  ()

let builder = WebApplication.CreateBuilder()

builder.Services.Configure<DatabaseSettings.T>(builder.Configuration.GetSection(DatabaseSettings.SectionName))

builder.Services.AddDbContext<AppDbContext>(configureDbContext) |> ignore


let app = builder.Build()

