module GooRes.Widgets.MediaList

open System
open Goo
open Goo.Widgets.Layout
open GooRes.Types
open GooRes.Player
open FunGoo.Children
open FunGoo.Interop
open FunGoo.Widgets

let inline clickable
  (id: string, child: Blob, [<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  Container(Key = id, OnClick = fun _ -> onClick()).Children child

let inline row
  (
    song: Song,
    index: int,
    isSelected: bool,
    [<InlineIfLambda>] onSelect: Song -> unit
  ) : Blob =
  clickable(
    $"song-{index}",
    ListRow(
      Title = song.Name,
      Selected = isSelected,
      MinHeight = Nullable(lengthFromFloat 36.0),
      PaddingVertical = Nullable 8.0
    )
      .backgroundColor(Color.Rgb(24, 31, 43))
      .selectedBackgroundColor(Color.Rgb(40, 52, 74))
      .textColor(Color.Rgb(230, 235, 245))
      .Build(),
    fun () -> onSelect song
  )

// The playlist is one mounted cell: its build reads the player store, and a
// row click rebuilds it through the owner rule. Selecting a row only raises
// the event; the parent decides what it means.
let create(player: PlayerState, onSelect: Song -> unit) : Cell = {
  new Cell() with
    override _.Build() : Blob =
      let selected = player.Selected |> ValueOption.defaultValue -1

      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        FlexShrink = 1.0,
        MinHeight = 0,
        OverflowY = Overflow.Scroll,
        OverflowX = Overflow.Hidden,
        // Goo 0.6.4 dropped the built-in scrollbar thumb look: a scrollbar
        // only paints when the axis owns authored Track and Thumb parts.
        ScrollbarY =
          Scrollbar(
            Thickness = 10.0,
            Track = Container(BackgroundColor = Color.Rgb(24, 31, 43)),
            Thumb = Container(BackgroundColor = Color.Rgb(90, 105, 135))
          ),
        Gap = 2
      )
        .Children(
          player.Songs
          |> Array.mapi(fun i s -> row(s, i, i = selected, onSelect))
        )
}
