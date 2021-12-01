using System;
using System.Linq;
using System.Threading.Tasks;
using Bot.Helpers;
using Bot.Services.Interfaces;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bot.Services
{
    public class InlineQueryService : IInlineQueryService
    {
        private readonly ITelegramBotClient _bot;
        private readonly ISpotifyClient _spotifyClient;

        public InlineQueryService(ITelegramBotClient bot, ISpotifyClient spotifyClient)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _spotifyClient = spotifyClient ?? throw new ArgumentNullException(nameof(spotifyClient));
        }

        public async Task HandleAsync(InlineQuery inlineQuery)
        {
            if (string.IsNullOrEmpty(inlineQuery.Query))
            {
                return;
            }

            var response = await _spotifyClient.Search.Item(new(SearchRequest.Types.All, inlineQuery.Query)
            {
                Limit = 50
            });

            var tracks = response.Tracks.Items.Select(InlineQueryResultHelpers.GetTrackInlineQueryResult);
            var albums = response.Albums.Items.Select(InlineQueryResultHelpers.GetAlbumInlineQueryResult);
            var artists = response.Artists.Items.Select(InlineQueryResultHelpers.GetArtistInlineQueryResult);
            var playlists = response.Playlists.Items.Select(InlineQueryResultHelpers.GetPlaylistInlineQueryResult);

            var results = new[]
            {
                tracks,
                albums,
                artists,
                playlists
            }.SelectMany(markdowns => markdowns);

            await _bot.AnswerInlineQueryAsync(inlineQuery.Id, results);
        }
    }
}