# Spotify Telegram Bot

Telegram Bot to explore [Spotify](https://www.spotify.com/).

## Getting Started

### Prerequisites

- [.NET 6](https://dotnet.microsoft.com/download) or higher

### Installing

**Spotify:**

1. Go to [Spotify Developer Portal](https://developer.spotify.com)
2. Register new application
3. Copy **Client Id** and **Client Secret**
4. Add callback url of /api/spotify/callback function to allowed callback urls

**Telegram:**

1. Contact to [@BotFather](https://t.me/BotFather) in Telegram
2. Create new bot
3. Copy bot token

**Project:**

1. Clone project
2. [Publish](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#publish) to Azure Functions or
   run [locally](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)
3. Update Azure Functions app configuration according to table below

| Parameter                  | Required? | Description                                                                                         |
|----------------------------|-----------|-----------------------------------------------------------------------------------------------------|
| Spotify__ClientId          | true      | Spotify app Client Id                                                                               |
| Spotify__ClientSecret      | true      | Spotify app ClientSecret                                                                            |
| Spotify__CallbackUrl       | true      | Callback url of function to handle Spotify callback. Should be url of api/spotify/callback function |
| Telegram__Token            | true      | Telegram bot token                                                                                  |
| Telegram__BotUrl           | true      | Url of your telegram bot. E.g. https://t.me/sptfyqbot                                               |
| Database__ConnectionString | true      | Connection string to PostgreSQL DB to store users and their spotify refresh tokens                  |

## Usage

You can try this bot in [Telegram](https://t.me/sptfyqbot)

## Built With

* [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - .NET Client for Telegram Bot API
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - An API for the Spotify-Client and the Spotify Web API, written in
  C#/.NET
* [Azure Functions](https://azure.microsoft.com/en-us/services/functions/) - More than just event-driven serverless compute

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.