module GooRes.Widgets.BottomBar

open Goo
open FunGoo.Children
open FunGoo.Style
open GooRes.Types
open GooRes.Player
open Goo.Widgets.Media

// The bottom bar is one mounted cell: it rebuilds on position and playback
// state events, and only then. The transport callbacks are created once
// here, so every input snapshot carries the same delegate instances and a
// clean diff can skip the rebuild.
let create
  (env: #IProvidePlayback, player: PlayerState, onTrackChanged: unit -> unit)
  : Cell =
  let onPlayPause() = playPause(env, player)

  let onNext() =
    skipNext(env, player)
    onTrackChanged()

  let onPrevious() =
    skipPrevious(env, player)
    onTrackChanged()

  let onSeekCommitted seconds = seek(env, seconds)
  let onVolumeChanged value = setVolume(env, value)

  {
    new Cell() with
      override _.Build() : Blob =
        let nowPlaying =
          player.Selected
          |> ValueOption.bind(fun index ->
            Array.tryItem index player.Songs |> ValueOption.ofOption)
          |> ValueOption.map(fun song -> $"Playing: {song.Name}")
          |> ValueOption.defaultValue "Nothing loaded"

        Container(
          FlexDirection = FlexDirection.Column,
          Gap = 8,
          Padding = 12,
          BackgroundColor = Color.Rgb(16, 21, 31),
          BorderRadius = 8,
          BorderWidth = topEdge 1.0,
          BorderColor = Color.Rgb(35, 44, 62)
        )
          .Children(
            Text(
              Key = "now-playing",
              Content = nowPlaying,
              FontSize = 15,
              Color = Color.White
            ),
            Cell.Mount<MediaTransportInput, MediaTransport>(
              "media-transport",
              MediaTransportInput(
                Playing = player.Playing,
                Position = float player.Position,
                Duration = float player.Length,
                Volume = env.Playback.Volume(),
                OnPlayPause = onPlayPause,
                OnNext = onNext,
                OnPrevious = onPrevious,
                OnPositionCommit = onSeekCommitted,
                OnVolumeChange = onVolumeChanged,
                OnVolumeCommit = onVolumeChanged
              )
            )
          )
  }
