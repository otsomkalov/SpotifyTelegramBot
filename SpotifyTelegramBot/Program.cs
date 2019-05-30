using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace SpotifyTelegramBot
{
    internal static class Program
    {
        private static TelegramBotClient _bot;
        private static CredentialsAuth _authData;
        private static Token _token;
        private static SpotifyWebAPI _spotifyApi;

        private static async Task Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("You should provide Telegram bot token and Spotify client id with client secret");

                return;
            }

            _bot = new TelegramBotClient(args[0]);

            _bot.OnInlineQuery += OnInlineQueryAsync;
            _bot.OnMessage += OnOnMessageAsync;
            _bot.StartReceiving();

            _authData = new CredentialsAuth(args[1], args[2]);

            _spotifyApi = new SpotifyWebAPI();

            await GetAccessTokenAsync();

            Console.WriteLine("Bot started");

            while (true) await Task.Delay(int.MaxValue);
        }

        private static async void OnOnMessageAsync(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message.Text.StartsWith("/start"))
                await _bot.SendTextMessageAsync(new ChatId(message.From.Id),
                    "This bot allows you search & share songs, albums and artists from Spotify. It works on every " +
                    "dialog, just type @ExploreSpotifyBot in message input",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(
                            "🔍 Search songs, albums, artists and playlists"),
                        InlineKeyboardButton.WithSwitchInlineQuery(
                            "🔗 Find and share songs, albums, artists and playlists")
                    }));
        }

        private static async void OnInlineQueryAsync(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            var inlineQuery = inlineQueryEventArgs.InlineQuery;

            if (string.IsNullOrEmpty(inlineQuery.Query)) return;

            if (_token.IsExpired()) await GetAccessTokenAsync();

            var response = await _spotifyApi.SearchItemsAsync(inlineQuery.Query, SearchType.All, 3);

            var tracksInlineQueryResults = response.Tracks.Items.Select(GetTrackInlineQueryResult);
            var albumsInlineQueryResults = response.Albums.Items.Select(GetAlbumInlineQueryResult);
            var artistsInlineQueryResults = response.Artists.Items.Select(GetArtistInlineQueryResult);
            var playlistsInlineQueryResults = response.Playlists.Items.Select(GetPlaylistInlineQueryResult);

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

        private static async Task GetAccessTokenAsync()
        {
            _token = await _authData.GetToken();
            _spotifyApi.AccessToken = $"Bearer {_token.AccessToken}";
        }

        private static InlineQueryResultArticle GetTrackInlineQueryResult(FullTrack track)
        {
            return new InlineQueryResultArticle(track.Id, track.Name,
                new InputTextMessageContent(GetTrackMarkdown(track))
                {
                    ParseMode = ParseMode.Html
                })
            {
                ThumbUrl = track.Album.Images.FirstOrDefault()?.Url,
                Description = $"{string.Join(" ,", track.Artists.Select(artist => artist.Name))}\nTrack"
            };
        }

        private static InlineQueryResultArticle GetAlbumInlineQueryResult(SimpleAlbum album)
        {
            return new InlineQueryResultArticle(album.Id, album.Name,
                new InputTextMessageContent(GetAlbumMarkdown(album))
                {
                    ParseMode = ParseMode.Html
                })
            {
                ThumbUrl = album.Images.FirstOrDefault()?.Url,
                Description = $"{string.Join(" ,", album.Artists.Select(artist => artist.Name))} \nAlbum"
            };
        }

        private static InlineQueryResultArticle GetArtistInlineQueryResult(FullArtist artist)
        {
            return new InlineQueryResultArticle(artist.Id, artist.Name,
                new InputTextMessageContent(GetArtistMarkdown(artist))
                {
                    ParseMode = ParseMode.Html
                })
            {
                ThumbUrl = artist.Images.FirstOrDefault()?.Url,
                Description = $"{GetArtistGenres(artist)} \n Artist"
            };
        }

        private static InlineQueryResultArticle GetPlaylistInlineQueryResult(SimplePlaylist playlist)
        {
            return new InlineQueryResultArticle(playlist.Id, playlist.Name,
                new InputTextMessageContent(GetPlaylistMarkdown(playlist))
                {
                    ParseMode = ParseMode.Html
                })
            {
                ThumbUrl = playlist.Images.FirstOrDefault()?.Url,
                Description = $"Owner: {playlist.Owner.DisplayName} \nPlaylist"
            };
        }

        private static string GetTrackMarkdown(FullTrack track)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{track.ExternUrls["spotify"]}\">{track.Name}</a>")
                .AppendLine($"Artists: {GetTrackArtistsLinks(track)}")
                .AppendLine($"Album: {track.Album.Name}")
                .AppendLine($"Duration: {TimeSpan.FromMilliseconds(track.DurationMs):m\\:ss}")
                .ToString();
        }

        private static string GetAlbumMarkdown(SimpleAlbum album)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{album.ExternalUrls["spotify"]}\">{album.Name}</a>")
                .AppendLine($"Artists: {GetAlbumArtistsLinks(album)}")
                .AppendLine($"Release date: {album.ReleaseDate}")
                .ToString();
        }

        private static string GetArtistMarkdown(FullArtist artist)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>")
                .AppendLine(GetArtistGenres(artist))
                .ToString();
        }

        private static string GetPlaylistMarkdown(SimplePlaylist playlist)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{playlist.ExternalUrls["spotify"]}\">{playlist.Name}</a>")
                .AppendLine($"Owner: {playlist.Owner.DisplayName}")
                .ToString();
        }

        private static string GetTrackArtistsLinks(FullTrack track)
        {
            return string.Join(" ,",
                track.Artists
                    .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
        }

        private static string GetAlbumArtistsLinks(SimpleAlbum album)
        {
            return string.Join(" ,",
                album.Artists
                    .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
        }

        private static string GetArtistGenres(FullArtist artist)
        {
            return $"Genres: {string.Join(" ,", artist.Genres.Take(3))}";
        }
    }
}