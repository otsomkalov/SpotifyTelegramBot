using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpotifyTelegramBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace SpotifyTelegramBot
{
    internal static class Program
    {
        private static TelegramBotClient _bot;
        private static CredentialsAuth _authData;
        private static Token _token;
        private static SpotifyWebAPI _spotifyApi;
        private static ILogger _logger;

        private static async Task Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("You should provide Telegram bot token and Spotify client id with client secret");

                return;
            }

            _logger = Configuration.ConfigureLogger();

            _bot = new TelegramBotClient(args[0]);

            _bot.OnInlineQuery += OnInlineQueryAsync;
            _bot.OnMessage += OnOnMessageAsync;
            _bot.StartReceiving();

            _authData = new CredentialsAuth(args[1], args[2]);

            _spotifyApi = new SpotifyWebAPI();

            await GetAccessTokenAsync();

            _logger.Information("Bot started");

            await Task.Delay(-1);
        }

        private static async void OnOnMessageAsync(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;

                _logger.Information("Got message: {@Message}", message);

                if (message.Text.StartsWith("/start"))
                    await _bot.SendTextMessageAsync(new ChatId(message.From.Id),
                        "This bot allows you search & share songs, albums and artists from Spotify. It works on every " +
                        "dialog, just type @ExploreSpotifyBot in message input",
                        replyMarkup: InlineKeyboardMarkupHelpers.GetStartKeyboardMarkup());
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during processing message");
            }
        }

        private static async void OnInlineQueryAsync(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            try
            {
                var inlineQuery = inlineQueryEventArgs.InlineQuery;

                if (string.IsNullOrEmpty(inlineQuery.Query)) return;

                if (_token.IsExpired()) await GetAccessTokenAsync();

                var response = await _spotifyApi.SearchItemsAsync(inlineQuery.Query, SearchType.All, 3);

                var tracksInlineQueryResults =
                    response.Tracks.Items.Select(InlineQueryResultHelpers.GetTrackInlineQueryResult);

                var albumsInlineQueryResults =
                    response.Albums.Items.Select(InlineQueryResultHelpers.GetAlbumInlineQueryResult);

                var artistsInlineQueryResults =
                    response.Artists.Items.Select(InlineQueryResultHelpers.GetArtistInlineQueryResult);

                var playlistsInlineQueryResults =
                    response.Playlists.Items.Select(InlineQueryResultHelpers.GetPlaylistInlineQueryResult);

                var inlineQueriesResults = new[]
                    {
                        tracksInlineQueryResults,
                        albumsInlineQueryResults,
                        artistsInlineQueryResults,
                        playlistsInlineQueryResults
                    }
                    .SelectMany(markdowns => markdowns);

                await _bot.AnswerInlineQueryAsync(inlineQuery.Id, inlineQueriesResults);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during processing inline query");
            }
        }

        private static async Task GetAccessTokenAsync()
        {
            _token = await _authData.GetToken();
            _spotifyApi.AccessToken = $"Bearer {_token.AccessToken}";
        }
    }
}
