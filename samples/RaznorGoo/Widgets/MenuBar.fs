module GooRes.Widgets.MenuBar

open Goo
open Goo.Widgets.Actions
open FunGoo.Children
open FunGoo.Widgets

type MenuBarProps = {
  onSelectFiles: unit -> unit
  onSelectFolder: unit -> unit
}

let inline item
  (label: string)
  ([<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  ActionButton(Label = label, OnClick = fun _ -> onClick())
    .backgroundColor(Color.Rgb(30, 38, 54))
    .textColor(Color.Rgb(230, 235, 245))
    .hoverBackgroundColor(Color.Rgb(40, 52, 74))
    .Build()

let inline create(p: MenuBarProps) : Blob =
  Container(
    Key = "menu-bar",
    FlexDirection = FlexDirection.Row,
    Gap = 8,
    Padding = 12,
    BackgroundColor = Color.Rgb(16, 21, 31)
  )
    .Children(
      item "Select Files" p.onSelectFiles,
      item "Select Folder" p.onSelectFolder
    )
