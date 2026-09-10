module GooRes.Shell

open Goo
open FunGoo.Children
open GooRes.Types
open GooRes.Widgets
open GooRes.Widgets.FilePicker
open Mibo.Adaptive
open Goo.Widgets.Media



let view
  (env: Env)
  (player: Player.PlayerState)
  (picker: FilePickerWidget)
  : Blob =

  Container(
    Width = Length.Percent 100,
    Height = Length.Percent 100,
    Padding = 12,
    Gap = 12,
    FlexDirection = FlexDirection.Column,
    BackgroundColor = Color.Rgb(20, 27, 39)
  )
    .Children(
      MenuBar.create {
        onSelectFiles = fun _ -> picker.openPicker PickFiles
        onSelectFolder = fun _ -> picker.openPicker PickFolder
      },
      picker.view(),
      Container(
        FlexDirection = FlexDirection.Row,
        Gap = 12,
        FlexGrow = 1.0,
        FlexShrink = 1.0,
        MinHeight = 0
      )
        .Children(
          Container(
            FlexGrow = 1.0,
            FlexShrink = 1.0,
            MinWidth = 0,
            BorderRadius = 8,
            BackgroundColor = Color.Rgb(16, 21, 31)
          ),
          Container(
            Width = Length.Percent 100,
            MaxWidth = 280,
            FlexShrink = 1.0,
            MinWidth = 0
          )
            .Children(
              MediaList.create {
                songs = player.songs
                selected = player.selected
                onSelect = fun song -> Player.playSong env player song
              }
            )
        ),
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
              Position = (player.position |> AVal.map float |> AVal.getValue),
              Duration = (player.length |> AVal.map float |> AVal.getValue),
              Volume = env.Playback.Volume(),
              OnPlayPause = (fun () -> Player.playPause env player),
              OnNext = (fun () -> Player.skipNext env player),
              OnPrevious = (fun () -> Player.skipPrevious env player),
              OnSeekCommitted = (fun seconds -> Player.seek env seconds),
              OnVolumeChanged = (fun value -> Player.setVolume env value),
              OnVolumeCommitted = (fun value -> Player.setVolume env value)
            )
          )
        )
    )
