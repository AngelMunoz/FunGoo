module GooRes.Widgets.FilePicker

open System
open System.IO

open Goo
open FunGoo.Children
open Mibo.Adaptive

type PickerEntry = {
  Name: string
  Path: string
  IsFolder: bool
}

type PickerMode =
  | PickFolder
  | PickFiles

type FilePickerProps = {
  filters: string list
  postUI: (unit -> unit) -> unit

  onFilesSelected: string list -> unit
  onFolderSelected: string -> unit
}

// The handle a caller keeps: what to render, and how to open the picker.
type FilePickerWidget = {
  view: unit -> Blob
  openPicker: PickerMode -> unit
}

let listDir (filterPatterns: string list) (path: string) : PickerEntry list =
  try
    let dirs =
      Directory.GetDirectories path
      |> Array.sort
      |> Array.map(fun d -> {
        Name = Path.GetFileName d
        Path = d
        IsFolder = true
      })

    let files =
      Array.ofList filterPatterns
      |> Array.collect(fun pattern -> Directory.GetFiles(path, pattern))
      |> Array.distinct
      |> Array.sort
      |> Array.map(fun f -> {
        Name = Path.GetFileName f
        Path = f
        IsFolder = false
      })

    List.ofArray dirs @ List.ofArray files
  with _ -> []

let inline actionButton
  (label: string)
  ([<InlineIfLambda>] onClick: unit -> unit)
  =
  Button(
    OnClick = (fun _ -> onClick()),
    Padding = 8,
    BorderRadius = 6,
    BackgroundColor = Color.Rgb(30, 38, 54)
  )
    .Children(
      Text(Content = label, FontSize = 13, Color = Color.Rgb(230, 235, 245))
    )

let entryRow
  (m: PickerMode)
  (currentPicked: string list)
  (onOpenFolder: string -> unit)
  (onToggleFile: string -> unit)
  (e: PickerEntry)
  : Blob =
  let isPicked = List.contains e.Path currentPicked

  if e.IsFolder then
    Container(
      Key = e.Path,
      OnClick = (fun _ -> onOpenFolder e.Path),
      HitTestSelf = true,
      Padding = 8,
      BorderRadius = 6,
      BackgroundColor = Color.Rgb(24, 31, 43)
    )
      .Children(
        Text(
          Content = "/ " + e.Name,
          FontSize = 14,
          Color = Color.Rgb(140, 190, 255)
        )
      )
  elif m = PickFiles then
    Container(
      Key = e.Path,
      OnClick = (fun _ -> onToggleFile e.Path),
      HitTestSelf = true,
      Padding = 8,
      BorderRadius = 6,
      BackgroundColor =
        (if isPicked then
           Color.Rgb(40, 52, 74)
         else
           Color.Rgb(24, 31, 43))
    )
      .Children(
        Text(
          Content = (if isPicked then "[x] " else "[ ] ") + e.Name,
          FontSize = 14,
          Color = Color.Rgb(230, 235, 245)
        )
      )
  else
    Container(
      Key = e.Path,
      Padding = 8,
      BorderRadius = 6,
      BackgroundColor = Color.Rgb(20, 25, 36)
    )
      .Children(
        Text(Content = e.Name, FontSize = 14, Color = Color.Rgb(120, 130, 150))
      )

let inline create(p: FilePickerProps) : FilePickerWidget =
  let home = Environment.GetFolderPath Environment.SpecialFolder.UserProfile

  let isOpen = CVal.create false
  let mode = CVal.create PickFolder
  let directory = CVal.create home
  let entries = CVal.create List.empty<PickerEntry>
  let picked = CVal.create List.empty<string>

  let loadDirectory path =
    async {
      let found = listDir p.filters path

      p.postUI(fun () ->
        CVal.set found entries
        CVal.set path directory)
    }
    |> Async.Start

  let openPicker m =
    CVal.set m mode
    CVal.set List.empty<string> picked
    CVal.set true isOpen
    loadDirectory(AVal.getValue directory)

  let closePicker() = CVal.set false isOpen

  let view() : Blob =
    if not(AVal.getValue isOpen) then
      Container()
    else
      let currentMode = AVal.getValue mode
      let dir = AVal.getValue directory
      let currentEntries = AVal.getValue entries
      let currentPicked = AVal.getValue picked

      let confirmLabel =
        if currentMode = PickFolder then
          "Use Folder"
        else
          "Add Files"

      let goUp() =
        match Path.GetDirectoryName dir with
        | null -> ()
        | parent -> loadDirectory parent

      let toggle path =
        let next =
          if List.contains path currentPicked then
            List.filter ((<>) path) currentPicked
          else
            path :: currentPicked

        CVal.set next picked

      let confirm() =
        if currentMode = PickFolder then
          p.onFolderSelected dir
        else
          p.onFilesSelected(List.sort currentPicked)
          CVal.set List.empty<string> picked

        closePicker()

      Container(
        FlexDirection = FlexDirection.Column,
        Gap = 8,
        FlexGrow = 1.0,
        FlexShrink = 1.0,
        MinHeight = 0
      )
        .Children(
          Container(
            FlexDirection = FlexDirection.Row,
            Gap = 8,
            AlignItems = AlignItems.Center
          )
            .Children(
              Text(
                Content = dir,
                FontSize = 13,
                Color = Color.Rgb(150, 160, 180),
                FlexGrow = 1.0
              ),
              actionButton "Up" goUp,
              actionButton "Cancel" closePicker,
              actionButton confirmLabel confirm
            ),
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
            .Children(
              currentEntries
              |> List.map(
                entryRow currentMode currentPicked loadDirectory toggle
              )
            )
        )

  { view = view; openPicker = openPicker }
