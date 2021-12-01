using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace Bot.Helpers;

public static class InlineQueryResultHelpers
{
    public static InlineQueryResultArticle GetAlbumInlineQueryResult(SimpleAlbum album)
    {
        return new(album.Id, album.Name,
            new InputTextMessageContent(MarkdownHelpers.GetAlbumMarkdown(album))
            {
                ParseMode = ParseMode.Html
            })
        {
            ThumbUrl = album.Images.FirstOrDefault()?.Url,
            Description = $"{string.Join(", ", album.Artists.Select(artist => artist.Name))} \nAlbum"
        };
    }

    public static InlineQueryResultArticle GetArtistInlineQueryResult(FullArtist artist)
    {
        return new(artist.Id, artist.Name,
            new InputTextMessageContent(MarkdownHelpers.GetArtistMarkdown(artist))
            {
                ParseMode = ParseMode.Html
            })
        {
            ThumbUrl = artist.Images.FirstOrDefault()?.Url,
            Description = $"{DataHelpers.GetArtistGenres(artist)} \n Artist"
        };
    }

    public static InlineQueryResultArticle GetPlaylistInlineQueryResult(SimplePlaylist playlist)
    {
        return new(playlist.Id, playlist.Name,
            new InputTextMessageContent(MarkdownHelpers.GetPlaylistMarkdown(playlist))
            {
                ParseMode = ParseMode.Html
            })
        {
            ThumbUrl = playlist.Images.FirstOrDefault()?.Url,
            Description = $"Owner: {playlist.Owner.DisplayName} \nPlaylist"
        };
    }

    public static InlineQueryResultArticle GetTrackInlineQueryResult(FullTrack track)
    {
        return new(track.Id, track.Name,
            new InputTextMessageContent(MarkdownHelpers.GetTrackMarkdown(track))
            {
                ParseMode = ParseMode.Html
            })
        {
            ThumbUrl = track.Album.Images.FirstOrDefault()?.Url,
            Description = $"{string.Join(" ,", track.Artists.Select(artist => artist.Name))}\nTrack"
        };
    }
}