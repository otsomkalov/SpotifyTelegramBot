FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
COPY ["SpotifyTelegramBot/SpotifyTelegramBot.csproj", "SpotifyTelegramBot/"]
RUN dotnet restore "SpotifyTelegramBot/SpotifyTelegramBot.csproj"
COPY . .
WORKDIR "/src/SpotifyTelegramBot"
RUN dotnet build "SpotifyTelegramBot.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SpotifyTelegramBot.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SpotifyTelegramBot.dll"]
