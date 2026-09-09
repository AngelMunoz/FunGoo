module GooRes.Widgets.MediaList

open Goo
open GooRes.Types
open FunGoo.Children
open Mibo.Adaptive

type MediaListProps = {
  songs: alist<Song>
  selected: int aval
  onSelect: Song -> unit
}

let inline row
  (song: Song)
  (index: int)
  (isSelected: bool)
  ([<InlineIfLambda>] onSelect: Song -> unit)
  : Blob =
  Container(
    Key = $"song-{index}",
    OnClick = (fun _ -> onSelect song),
    HitTestSelf = true,
    Padding = 8,
    BorderRadius = 6,
    BackgroundColor =
      (if isSelected then
         Color.Rgb(40, 52, 74)
       else
         Color.Rgb(24, 31, 43))
  )
    .Children(
      Text(Content = song.Name, FontSize = 15, Color = Color.Rgb(230, 235, 245))
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
