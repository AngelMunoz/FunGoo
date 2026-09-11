module GooRes.Layout

open Goo
open FunGoo.Children
open GooRes.Types
open GooRes.Widgets
open GooRes.Widgets.FilePicker

// The root cell: static chrome plus the stateful cells. The chrome carries
// no state, so a root rebuild only re-runs this build; the cells rebuild
// themselves through their own events.
let view (picker: FilePickerWidget) (playlist: Cell) (bottomBar: Cell) : Cell =
  let middleView =
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
          .Children(Cell.Mount<Cell>((fun () -> playlist), "playlist"))
      )

  // Unkeyed mounts: the root children keep positional identity, and Goo
  // rejects child lists that mix keyed and unkeyed siblings.
  let bottomBarView = Cell.Mount<Cell>((fun () -> bottomBar), null)

  {
    new Cell() with
      override _.Build() : Blob =
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
            middleView,
            bottomBarView
          )
  }
