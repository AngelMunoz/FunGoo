module GooRes.Widgets.FilePicker

open System

open Goo
open Goo.Widgets.Actions
open Goo.Widgets.Feedback
open Goo.Widgets.Layout
open GooRes.Types
open FunGoo.Children
open FunGoo.Widgets
open Mibo.Adaptive
open GooRes

type PickerMode =
  | PickFolder
  | PickFiles

type FilePickerProps = {
  filters: string list
  startIn: string
  onFilesSelected: FsEntry list -> unit
  onFolderSelected: string -> unit
}

// The handle a caller keeps: what to render, and how to open the picker.
type FilePickerWidget = {
  view: unit -> Blob
  openPicker: PickerMode -> unit
}

let inline actionButton
  (label: string)
  ([<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  ActionButton(Label = label, OnClick = fun _ -> onClick())
    .backgroundColor(Color.Rgb(30, 38, 54))
    .textColor(Color.Rgb(230, 235, 245))
    .hoverBackgroundColor(Color.Rgb(40, 52, 74))
    .Build()

let inline clickable
  (id: string, child: Blob, [<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  Container(Key = id, OnClick = fun _ -> onClick()).Children child


let inline entryRow
  (m: PickerMode)
  (currentPicked: FsEntry list)
  (onOpenFolder: string -> unit)
  (onToggleFile: FsEntry -> unit)
  (e: FsEntry)
  : Blob =
  let isPicked = List.exists (fun x -> x.Path = e.Path) currentPicked

  if e.IsFolder then

    clickable(
      $"folder-{e.Path}",
      ListRow(
        Title = e.Name,
        Leading = Icons.folder,
        MinHeight = 36.0,
        PaddingVertical = 8.0
      )
        .backgroundColor(Color.Rgb(24, 31, 43))
        .textColor(Color.Rgb(140, 190, 255))
        .Build(),
      fun () -> onOpenFolder e.Path
    )
  elif m = PickFiles then
    clickable(
      $"file-{e.Path}",
      ListRow(
        Title = e.Name,
        Selected = isPicked,
        MinHeight = 36.0,
        PaddingVertical = 8.0
      )
        .backgroundColor(Color.Rgb(24, 31, 43))
        .selectedBackgroundColor(Color.Rgb(40, 52, 74))
        .textColor(Color.Rgb(230, 235, 245))
        .Build(),
      fun () -> onToggleFile e
    )
  else
    ListRow(Title = e.Name, MinHeight = 36.0, PaddingVertical = 8.0)
      .backgroundColor(Color.Rgb(20, 25, 36))
      .textColor(Color.Rgb(120, 130, 150))
      .opacity(0.7)
      .Build()

// Infrastructure comes in through the environment; props carry data and
// events only.
let inline create (env: Env) (p: FilePickerProps) : FilePickerWidget =
  let isOpen = CVal.create false
  let mode = CVal.create PickFolder

  // `ValueNone` is the drives view: the list shows one row per ready drive.
  let directory = CVal.create(ValueSome p.startIn)
  let entries = CVal.create List.empty<FsEntry>
  let picked = CVal.create List.empty<FsEntry>

  let loadDirectory path =
    async {
      let found = env.FileSystem.List(path, p.filters)

      env.PostUI(fun () ->
        CVal.set found entries
        CVal.set (ValueSome path) directory)
    }
    |> Async.Start

  let loadDrives() =
    async {
      let found = env.FileSystem.Drives()

      env.PostUI(fun () ->
        CVal.set found entries
        CVal.set ValueNone directory)
    }
    |> Async.Start

  let openPicker m =
    CVal.set m mode
    CVal.set List.empty<FsEntry> picked
    CVal.set true isOpen

    match AVal.getValue directory with
    | ValueSome dir -> loadDirectory dir
    | ValueNone -> loadDrives()

  let closePicker() = CVal.set false isOpen

  let view() : Blob =
    if not(AVal.getValue isOpen) then
      Container()
    else
      let currentMode = AVal.getValue mode
      let currentDir = AVal.getValue directory
      let currentEntries = AVal.getValue entries
      let currentPicked = AVal.getValue picked

      let confirmLabel =
        if currentMode = PickFolder then
          "Use Folder"
        else
          "Add Files"

      let goUp() =
        match currentDir with
        | ValueNone -> ()
        | ValueSome dir ->
          match env.FileSystem.Parent dir with
          | ValueSome parent -> loadDirectory parent
          | ValueNone -> loadDrives()

      let toggle(e: FsEntry) =
        let next =
          if List.exists (fun x -> x.Path = e.Path) currentPicked then
            List.filter (fun x -> x.Path <> e.Path) currentPicked
          else
            e :: currentPicked

        CVal.set next picked

      let confirm() =
        match currentDir with
        | ValueNone -> ()
        | ValueSome dir ->
          if currentMode = PickFolder then
            p.onFolderSelected dir
          else
            p.onFilesSelected(List.sortBy (fun e -> e.Path) currentPicked)
            CVal.set List.empty<FsEntry> picked

          closePicker()

      let locationText = currentDir |> ValueOption.defaultValue "This PC"

      let filterText = String.Join(", ", p.filters)
      let filterMessage = $"No {filterText} files in this folder"

      let emptyTitle, emptyDescription =
        if currentDir.IsNone then
          ("No drives found", "No ready drives on this system")
        else
          ("No media here", filterMessage)

      let rows =
        if List.isEmpty currentEntries then
          EmptyState(
            Title = emptyTitle,
            Description = emptyDescription,
            MinHeight = Nullable 160.0
          )
            .Build()
          |> List.singleton
        else
          currentEntries
          |> List.map(entryRow currentMode currentPicked loadDirectory toggle)

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
                Content = locationText,
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
            .Children(rows)
        )

  { view = view; openPicker = openPicker }
