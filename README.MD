# Spotify Telegram Bot

Telegram Bot to explore [Spotify](https://www.spotify.com/). 

## Getting Started
### Prerequisites

- [.NET Core 3.1](https://dotnet.microsoft.com/download) or higher

### Installing

**Spotify:**
1. Go to [Spotify Developer Portal](https://developer.spotify.com)
2. Register new application
3. Copy **Client Id** and **Client Secret**

**Telegram:**
1. Contact [@BotFather](https://t.me/BotFather) in Telegram
2. Create new bot
3. Copy bot token

**Project:**
1. Clone project
2. Update values in **appsettings.json**
3. Update values in **aws-lambda-tools-defaults.json**
4. Update default welcome string in MessageService.cs
5. Run locally or publish via `dotnet lambda deploy-serverless`

## Usage

You can try this bot in [Telegram](https://t.me/sptfyqbot)

## Built With

* [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - .NET Client for Telegram Bot API
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - An API for the Spotify-Client and the Spotify Web API, written in C#/.NET
* [Amazon Lambda](https://aws.amazon.com/lambda/) - Run code without thinking about servers. Pay only for the compute time you consume.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.