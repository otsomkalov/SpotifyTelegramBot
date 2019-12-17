using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyTelegramBot.Helpers;
using SpotifyTelegramBot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SpotifyTelegramBot.Services
{
    public class InlineQueryService : IInlineQueryService
    {
        private readonly ITelegramBotClient _bot;
        private readonly SpotifyWebAPI _spotifyApi;
        private readonly ISpotifyAuthService _spotifyAuthService;

        public InlineQueryService(ITelegramBotClient bot, SpotifyWebAPI spotifyApi,
            ISpotifyAuthService spotifyAuthService)
        {
            _bot = bot;
            _spotifyApi = spotifyApi;
            _spotifyAuthService = spotifyAuthService;
        }

        public async Task HandleAsync(InlineQuery inlineQuery)
        {
            if (string.IsNullOrEmpty(inlineQuery.Query)) return;

            _spotifyApi.AccessToken = $"Bearer {await _spotifyAuthService.GetAccessTokenAsync()}";

            var response = await _spotifyApi.SearchItemsAsync(inlineQuery.Query, SearchType.All, 5);

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
    }
}
