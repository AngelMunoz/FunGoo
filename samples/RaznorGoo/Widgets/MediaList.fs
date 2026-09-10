module GooRes.Widgets.MediaList

open System
open Goo
open Goo.Widgets.Layout
open GooRes.Types
open FunGoo.Children
open FunGoo.Widgets
open Mibo.Adaptive

type MediaListProps = {
  songs: alist<Song>
  selected: int aval
  onSelect: Song -> unit
}

let inline clickable
  (id: string, child: Blob, [<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  Container(Key = id, OnClick = fun _ -> onClick()).Children child

let inline row
  (song: Song)
  (index: int)
  (isSelected: bool)
  ([<InlineIfLambda>] onSelect: Song -> unit)
  : Blob =
  clickable(
    $"song-{index}",
    ListRow(
      Title = song.Name,
      Selected = isSelected,
      MinHeight = 36.0,
      PaddingVertical = 8.0
    )
      .backgroundColor(Color.Rgb(24, 31, 43))
      .selectedBackgroundColor(Color.Rgb(40, 52, 74))
      .textColor(Color.Rgb(230, 235, 245))
      .Build(),
    fun () -> onSelect song
  )

let inline create(p: MediaListProps) : Blob =
  let songs = AList.toList p.songs
  let selected = AVal.getValue p.selected

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexShrink = 1.0,
    MinHeight = 0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    ScrollbarVisibility = ScrollbarVisibility.Auto,
    Gap = 2
  )
    .Children(songs |> List.mapi(fun i s -> row s i (i = selected) p.onSelect))
