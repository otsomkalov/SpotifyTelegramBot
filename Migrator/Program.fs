open System
open Bot
open Bot.Data
open Microsoft.AspNetCore.Builder
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
let configureDbContext (provider: IServiceProvider) (builder: DbContextOptionsBuilder) =
  let settings =
    provider
      .GetRequiredService<IOptions<Settings.Database.T>>()
      .Value

  builder.UseNpgsql(settings.ConnectionString) |> ignore

  ()

let builder = WebApplication.CreateBuilder()

builder.Services.Configure<Settings.Database.T>(builder.Configuration.GetSection(Settings.Database.SectionName))

builder.Services.AddDbContext<AppDbContext>(configureDbContext) |> ignore


let app = builder.Build()

