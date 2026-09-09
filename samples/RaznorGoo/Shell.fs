module GooRes.Shell

open Goo
open FunGoo.Children
open GooRes.Types
open GooRes.Player
open GooRes.Widgets
open GooRes.Widgets.FilePicker
open Mibo.Adaptive

let view (env: Env) (player: PlayerState) (picker: FilePickerWidget) : Blob =
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
                selected = CVal.value player.selected
                onSelect = fun song -> playSong env player song
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
          Text(Content = nowPlaying player, FontSize = 15, Color = Color.White),
          Progress.create {
            value = CVal.value player.position
            maxValue = AVal.constant 100.0f
            color = Color.Rgb(74, 125, 255)
            trackColor = Color.Rgb(35, 44, 62)
            height = 8.0
          },
          MediaMenu.create {
            isPlaying = CVal.value player.playing
            loop = CVal.value player.loop
            onPlayPause = fun _ -> playPause env player
            onNext = fun _ -> skipNext env player
            onPrevious = fun _ -> skipPrevious env player
            onShuffle = fun _ -> shuffle player
            onLoop = fun _ -> cycleLoop player
          }
        )
    )
