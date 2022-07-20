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
      .GetRequiredService<IOptions<Database.T>>()
      .Value

  builder.UseNpgsql(settings.ConnectionString) |> ignore

  ()

let builder = WebApplication.CreateBuilder()

builder.Services.Configure<Database.T>(builder.Configuration.GetSection(Database.SectionName))

builder.Services.AddDbContext<AppDbContext>(configureDbContext) |> ignore


let app = builder.Build()

