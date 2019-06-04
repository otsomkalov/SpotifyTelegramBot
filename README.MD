# Spotify Telegram Bot

Telegram Bot to explore [Spotify](https://www.spotify.com/). 

## Getting Started
### Prerequisites

- [.NET Core 2.2](https://dotnet.microsoft.com/download) or higher

### Installing

**Spotify:**
1. Go to [Spotify Developer Portal](https://developer.spotify.com)
2. Register new application
3. Copy **Client Id** and **Client Secret**

**Telegram:**
1. Contact to [@BotFather](https://t.me/BotFather) in Telegram
2. Create new bot
3. Copy bot token

**Project:**
1. Clone project
2. Run in folder
```
docker-compose build
docker run -d spotifytelegrambot <telegram_bot_token> <spotify_app_id> <spotify_app_client_secret>
```
or go to **SpotifyTelegramBot** folder and run:
```
dotnet run <telegram_bot_token> <spotify_app_id> <spotify_app_client_secret>
```


## Usage

You can try this bot in [Telegram](https://t.me/ExploreSpotifyBot)


## Built With

* [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - .NET Client for Telegram Bot API
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - An API for the Spotify-Client and the Spotify Web API, written in C#/.NET


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.