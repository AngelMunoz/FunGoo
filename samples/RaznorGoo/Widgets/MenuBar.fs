module GooRes.Widgets.MenuBar

open Goo
open FunGoo.Children

type MenuBarProps = {
  onSelectFiles: unit -> unit
  onSelectFolder: unit -> unit
}

let inline item (label: string) ([<InlineIfLambda>] onClick: unit -> unit) =
  Button(
    OnClick = (fun _ -> onClick()),
    Padding = 10,
    BorderRadius = 6,
    BackgroundColor = Color.Rgb(30, 38, 54)
  )
    .Children(
      Text(Content = label, FontSize = 14, Color = Color.Rgb(230, 235, 245))
    )

let inline create(p: MenuBarProps) : Blob =
  Container(
    FlexDirection = FlexDirection.Row,
    Gap = 8,
    Padding = 12,
    BackgroundColor = Color.Rgb(16, 21, 31)
  )
    .Children(
      item "Select Files" p.onSelectFiles,
      item "Select Folder" p.onSelectFolder
    )
