module GooRes.Widgets.BottomBar

open Goo
open FunGoo.Children
open Mibo.Adaptive
open GooRes.Types
open GooRes.Player
open Goo.Widgets.Media

// The bottom bar is one mounted cell: it rebuilds on position and playback
// state events, and only then. The transport callbacks are created once
// here, so every input snapshot carries the same delegate instances and a
// clean diff can skip the rebuild.
let inline create
  (env: Env)
  (player: PlayerState)
  ([<InlineIfLambda>] onTrackChanged: unit -> unit)
  : Cell =
  let inline onPlayPause() = playPause env player

  let inline onNext() =
    skipNext env player
    onTrackChanged()

  let inline onPrevious() =
    skipPrevious env player
    onTrackChanged()

  let inline onSeekCommitted seconds = seek env seconds
  let inline onVolumeChanged value = setVolume env value

  {
    new Cell() with
      override _.Build() : Blob =
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = 8,
          Padding = 12,
          BackgroundColor = Color.Rgb(16, 21, 31),
          BorderRadius = 8,
          BorderTopWidth = 1,
          BorderColor = Color.Rgb(35, 44, 62)
        )
          .Children(
            Text(
              Key = "now-playing",
              Content = (player.nowPlaying |> AVal.getValue),
              FontSize = 15,
              Color = Color.White
            ),
            Cell.Mount<MediaTransportInput, MediaTransport>(
              "media-transport",
              MediaTransportInput(
                Playing = AVal.getValue player.playing,
                Position = float(AVal.getValue player.position),
                Duration = float(AVal.getValue player.length),
                Volume = env.Playback.Volume(),
                OnPlayPause = onPlayPause,
                OnNext = onNext,
                OnPrevious = onPrevious,
                OnSeekCommitted = onSeekCommitted,
                OnVolumeChanged = onVolumeChanged,
                OnVolumeCommitted = onVolumeChanged
              )
            )
          )
  }
