module GooRes.Layout

open System
open Goo
open FunGoo.Children
open FunGoo.Style
open GooRes.Widgets
open GooRes.Widgets.FilePicker

// The root cell: static chrome plus the stateful cells. The chrome carries
// no state, so a root rebuild only re-runs this build; the cells rebuild
// themselves through their own events.
let view(picker: FilePickerWidget, playlist: Cell, bottomBar: Cell) : Cell =
  // Mount factories created once, beside the cells they build: a factory
  // written inside `Build` would allocate a fresh closure every rebuild.
  let pickerFactory = fun () -> picker.cell
  let playlistFactory = fun () -> playlist
  let bottomBarFactory = fun () -> bottomBar

  let middleView =
    Container(
      Key = "middle-view",
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
          Width = percent 100.0,
          MaxWidth = 280,
          FlexShrink = 1.0,
          MinWidth = 0
        )
          .Children(Cell.Mount(playlistFactory, "playlist"))
      )

  let bottomBarView = Cell.Mount(bottomBarFactory, "bottom-bar")

  {
    new Cell() with
      override _.Build() : Blob =
        Container(
          Width = percent 100.0,
          Height = percent 100.0,
          Padding = 12,
          Gap = 12,
          FlexDirection = FlexDirection.Column,
          BackgroundColor = Color.Rgb(20, 27, 39)
        )
          .Children(
            MenuBar.create(
              (fun () -> picker.openPicker PickFiles),
              (fun () -> picker.openPicker PickFolder)
            ),
            Cell.Mount(pickerFactory, "picker"),
            middleView,
            bottomBarView
          )
  }
