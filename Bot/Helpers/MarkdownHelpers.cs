using System;
using System.Text;
using SpotifyAPI.Web.Models;

namespace Bot.Helpers
{
    public static class MarkdownHelpers
    {
        public static string GetAlbumMarkdown(SimpleAlbum album)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{album.ExternalUrls["spotify"]}\">{album.Name}</a>")
                .AppendLine($"Artists: {DataHelpers.GetAlbumArtistsLinks(album)}")
                .AppendLine($"Release date: {album.ReleaseDate}")
                .ToString();
        }

        public static string GetArtistMarkdown(FullArtist artist)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>")
                .AppendLine(DataHelpers.GetArtistGenres(artist))
                .ToString();
        }

        public static string GetPlaylistMarkdown(SimplePlaylist playlist)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{playlist.ExternalUrls["spotify"]}\">{playlist.Name}</a>")
                .AppendLine($"Owner: {playlist.Owner.DisplayName}")
                .ToString();
        }

        public static string GetTrackMarkdown(FullTrack track)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{track.ExternUrls["spotify"]}\">{track.Name}</a>")
                .AppendLine($"Artists: {DataHelpers.GetTrackArtistsLinks(track)}")
                .AppendLine($"Album: <a href=\"{track.Album.ExternalUrls["spotify"]}\">{track.Album.Name}</a>")
                .AppendLine($"Duration: {TimeSpan.FromMilliseconds(track.DurationMs):m\\:ss}")
                .ToString();
        }
    }
}
