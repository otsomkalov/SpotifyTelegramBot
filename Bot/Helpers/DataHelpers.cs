namespace Bot.Helpers;

public static class DataHelpers
{
    public static string GetAlbumArtistsLinks(SimpleAlbum album)
    {
        return string.Join(", ", album.Artists
            .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
    }

    public static string GetArtistGenres(FullArtist artist)
    {
        return $"Genres: {string.Join(", ", artist.Genres.Take(3))}";
    }

    public static string GetTrackArtistsLinks(FullTrack track)
    {
        return string.Join(", ", track.Artists
            .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
    }
}