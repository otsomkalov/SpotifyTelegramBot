namespace Bot.Helpers

open System
open Microsoft.Extensions.Primitives
open Microsoft.FSharp.Core
open SpotifyAPI.Web
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.InlineQueryResults

module String =

  let (|CommandData|_|) (str: string) =
    match str.Split(" ") with
    | [| _; data |] -> Some(data)
    | _ -> None

module Telegram =
  let (|CommandWithData|_|) (command: string) (input: string) =
    match input.Split(" ") with
    | [| inputCommand; data |] -> if inputCommand = command then Some(data) else None
    | _ -> None

  [<RequireQualifiedAccess>]
  module InlineQueryResult =
    let private getThumbUrl (images: Image seq) =
      match images
            |> Seq.sortBy (fun i -> i.Height)
            |> Seq.tryHead
        with
      | Some image -> image.Url
      | None -> null

    let private joinArtistsNames (artists: SimpleArtist seq) =
      artists
      |> Seq.map (fun a -> a.Name)
      |> String.concat ", "

    let private getArtistsLinks (artists: SimpleArtist seq) =
      artists
      |> Seq.map (fun artist -> $"""<a href="{artist.ExternalUrls["spotify"]}">{artist.Name}</a>""")
      |> String.concat ", "

    let private getArtistGenres (artist: FullArtist) =
      artist.Genres
      |> Seq.truncate 3
      |> String.concat ", "
      |> sprintf "Genres: %s"

    let FromAlbumForUser (album: SimpleAlbum) liked =
      let likeSymbol =
        if liked then "❤️" else String.Empty

      let albumMarkdown =
        String.Format(
          Resources.InlineQueryResult.AlbumContent,
          album.ExternalUrls["spotify"],
          album.Name,
          likeSymbol,
          getArtistsLinks album.Artists,
          album.ReleaseDate
        )

      InlineQueryResultArticle(
        album.Id,
        [ album.Name; likeSymbol ] |> String.concat " ",
        InputTextMessageContent(albumMarkdown, ParseMode = ParseMode.Html),
        ThumbnailUrl = getThumbUrl album.Images,
        Description = String.Format(Resources.InlineQueryResult.AlbumDescription, joinArtistsNames album.Artists)
      )

    let FromAlbumForAnonymousUser album = FromAlbumForUser album false

    let FromArtist (artist: FullArtist) =
      let artistMarkdown =
        String.Format(Resources.InlineQueryResult.ArtistContent, artist.ExternalUrls["spotify"], artist.Name, getArtistGenres artist)

      InlineQueryResultArticle(
        artist.Id,
        artist.Name,
        InputTextMessageContent(artistMarkdown, ParseMode = ParseMode.Html),
        ThumbnailUrl = getThumbUrl artist.Images,
        Description = String.Format(Resources.InlineQueryResult.ArtistDescription, getArtistGenres artist)
      )

    let FromPlaylist (playlist: FullPlaylist) =
      let playlistMarkdown =
        String.Format(
          Resources.InlineQueryResult.PlaylistContent,
          playlist.ExternalUrls["spotify"],
          playlist.Name,
          playlist.Owner.ExternalUrls["spotify"],
          playlist.Owner.DisplayName
        )

      InlineQueryResultArticle(
        playlist.Id,
        playlist.Name,
        InputTextMessageContent(playlistMarkdown, ParseMode = ParseMode.Html),
        ThumbnailUrl = getThumbUrl playlist.Images,
        Description = String.Format(Resources.InlineQueryResult.PlaylistDescription, playlist.Owner.DisplayName)
      )

    let FromTrackForUser (track: FullTrack) liked =
      let likeSymbol =
        if liked then "❤️" else String.Empty

      let trackMarkdown =
        String.Format(
          Resources.InlineQueryResult.TrackContent,
          track.ExternalUrls["spotify"],
          track.Name,
          likeSymbol,
          getArtistsLinks track.Artists,
          track.Album.ExternalUrls["spotify"],
          track.Album.Name,
          TimeSpan
            .FromMilliseconds(track.DurationMs)
            .ToString(@"mm\:ss")
        )

      InlineQueryResultArticle(
        track.Id,
        [ track.Name; likeSymbol ] |> String.concat " ",
        InputTextMessageContent(trackMarkdown, ParseMode = ParseMode.Html),
        ThumbnailUrl = getThumbUrl track.Album.Images,
        Description = String.Format(Resources.InlineQueryResult.TrackDescription, joinArtistsNames track.Artists)
      )

    let FromTrackFromAnonymousUser track = FromTrackForUser track false

module IQueryCollection =

  let (|QueryParam|_|) (stringValues: StringValues) =
    if stringValues = StringValues.Empty then
      None
    else
      Some(stringValues.ToString())
