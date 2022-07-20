﻿namespace Bot.Helpers

open System
open Microsoft.FSharp.Core
open SpotifyAPI.Web
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.InlineQueryResults

module String =
  let (|StartsWith|_|) (prefix: string) (str: string) =
    if str.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase) then
      Some()
    else
      None

  let (|CommandData|_|) (str: string) =
    match str.Split(" ") with
    | [| _; data |] -> Some(data)
    | _ -> None

module Telegram =
  module InlineQueryResult =
    let private getThumbUrl (images: Image seq) =
      match images |> Seq.tryHead with
      | Some image -> image.Url
      | None -> null

    let private getArtistsNames (artists: SimpleArtist seq) =
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
          likeSymbol,
          album.Name,
          getArtistsLinks album.Artists,
          album.ReleaseDate
        )

      InlineQueryResultArticle(
        album.Id,
        [ album.Name; likeSymbol ] |> String.concat " ",
        InputTextMessageContent(albumMarkdown, ParseMode = ParseMode.Html),
        ThumbUrl = getThumbUrl album.Images,
        Description = String.Format(Resources.InlineQueryResult.AlbumDescription, getArtistsNames album.Artists)
      )

    let FromAlbumForAnonymousUser album = FromAlbumForUser album false

    let FromArtist (artist: FullArtist) =
      let artistMarkdown =
        String.Format(Resources.InlineQueryResult.ArtistContent, artist.ExternalUrls["spotify"], artist.Name, getArtistGenres artist)

      InlineQueryResultArticle(
        artist.Id,
        artist.Name,
        InputTextMessageContent(artistMarkdown, ParseMode = ParseMode.Html),
        ThumbUrl = getThumbUrl artist.Images,
        Description = String.Format(Resources.InlineQueryResult.ArtistDescription, getArtistGenres artist)
      )

    let FromPlaylist (playlist: SimplePlaylist) =
      let playlistMarkdown =
        String.Format(
          Resources.InlineQueryResult.PlaylistContent,
          playlist.ExternalUrls["spotify"],
          playlist.Name,
          playlist.Owner.DisplayName
        )

      InlineQueryResultArticle(
        playlist.Id,
        playlist.Name,
        InputTextMessageContent(playlistMarkdown, ParseMode = ParseMode.Html),
        ThumbUrl = getThumbUrl playlist.Images,
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
        ThumbUrl = getThumbUrl track.Album.Images,
        Description = String.Format(Resources.InlineQueryResult.TrackDescription, getArtistsNames track.Artists)
      )

    let FromTrackFromAnonymousUser track = FromTrackForUser track false

  module Shared =
    let inline getUserId (item: ^a) = (^a: (member From: User) item).Id
