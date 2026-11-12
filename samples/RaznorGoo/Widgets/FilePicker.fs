module GooRes.Widgets.FilePicker

open System

open Goo
open Goo.Widgets.Actions
open Goo.Widgets.Feedback
open Goo.Widgets.Layout
open GooRes.Types
open FunGoo.Children
open FunGoo.Interop
open FunGoo.Widgets
open GooRes

type PickerMode =
  | PickFolder
  | PickFiles

// Data only: the callbacks travel as parameters of `create`.
type FilePickerProps = {
  filters: string list
  startIn: string
}

// The handle a caller keeps: the mounted cell, and how to open the picker.
type FilePickerWidget = {
  cell: Cell
  openPicker: PickerMode -> unit
}

let inline actionButton
  (label: string, [<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  ActionButton(Content = label, OnClick = fun _ -> onClick())
    .backgroundColor(Color.Rgb(30, 38, 54))
    .textColor(Color.Rgb(230, 235, 245))
    .hoverBackgroundColor(Color.Rgb(40, 52, 74))
    .Build()

let inline clickable
  (id: string, child: Blob, [<InlineIfLambda>] onClick: unit -> unit)
  : Blob =
  Container(Key = id, OnClick = fun _ -> onClick()).Children child

let inline entryRow
  (
    m: PickerMode,
    currentPicked: FsEntry[],
    e: FsEntry,
    [<InlineIfLambda>] onOpenFolder: string -> unit,
    [<InlineIfLambda>] onToggleFile: FsEntry -> unit
  ) : Blob =
  let isPicked = Array.exists (fun x -> x.Path = e.Path) currentPicked

  if e.IsFolder then

    clickable(
      $"folder-{e.Path}",
      ListRow(
        Title = e.Name,
        Leading = Icons.folder,
        MinHeight = Nullable(lengthFromFloat 36.0),
        PaddingVertical = Nullable 8.0
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
        MinHeight = Nullable(lengthFromFloat 36.0),
        PaddingVertical = Nullable 8.0
      )
        .backgroundColor(Color.Rgb(24, 31, 43))
        .selectedBackgroundColor(Color.Rgb(40, 52, 74))
        .textColor(Color.Rgb(230, 235, 245))
        .Build(),
      fun () -> onToggleFile e
    )
  else
    ListRow(
      Title = e.Name,
      MinHeight = Nullable(lengthFromFloat 36.0),
      PaddingVertical = Nullable 8.0
    )
      .backgroundColor(Color.Rgb(20, 25, 36))
      .textColor(Color.Rgb(120, 130, 150))
      .opacity(0.7)
      .Build()

// The picker keeps its view state at birth (class form): the loads and the
// open command sit beside the state they write. `Open` runs from the root's
// menu callbacks, so the owner rule rebuilds the root, not this cell —
// `Open` rebuilds it explicitly. Row clicks are authored inside `Build`, so
// the owner rule covers them and they need no rebuild call.
type PickerCell<'env when 'env :> IProvideFileSystem and 'env :> IProvidePostUI>
  (
    env: 'env,
    props: FilePickerProps,
    onFilesSelected: FsEntry[] -> unit,
    onFolderSelected: string -> unit
  ) as this =
  inherit Cell()

  let mutable isOpen = false
  let mutable mode = PickFolder

  // `ValueNone` is the drives view: the list shows one row per ready drive.
  let mutable directory: string voption = ValueSome props.startIn
  let mutable entries = Array.empty<FsEntry>
  let mutable picked = Array.empty<FsEntry>

  // Loads run on background threads; the results return through PostUI,
  // write the state, and rebuild this cell explicitly.
  let loadDirectory path =
    async {
      let found = env.FileSystem.List(path, props.filters)

      env.PostUI.Post(fun () ->
        entries <- found
        directory <- ValueSome path
        this.Rebuild())
    }
    |> Async.Start

  let loadDrives() =
    async {
      let found = env.FileSystem.Drives()

      env.PostUI.Post(fun () ->
        entries <- found
        directory <- ValueNone
        this.Rebuild())
    }
    |> Async.Start

  member _.Open(m: PickerMode) =
    mode <- m
    picked <- Array.empty<FsEntry>
    isOpen <- true

    // The root's menu callback rebuilds the root through the owner rule;
    // this cell needs the explicit rebuild.
    this.Rebuild()

    match directory with
    | ValueSome dir -> loadDirectory dir
    | ValueNone -> loadDrives()

  override _.Build() : Blob =
    if not isOpen then
      Container()
    else
      let confirmLabel = if mode = PickFolder then "Use Folder" else "Add Files"

      let goUp() =
        directory
        |> ValueOption.bind(fun dir -> env.FileSystem.Parent dir)
        |> function
          | ValueSome parent -> loadDirectory parent
          | ValueNone -> loadDrives()

      // Author rule: the click that toggles a row rebuilds this cell.
      let toggle(e: FsEntry) =
        if Array.exists (fun x -> x.Path = e.Path) picked then
          picked <- Array.filter (fun x -> x.Path <> e.Path) picked
        else
          picked <- Array.append [| e |] picked

      let closePicker() = isOpen <- false

      let confirm() =
        directory
        |> ValueOption.iter(fun dir ->
          if mode = PickFolder then
            onFolderSelected dir
          else
            onFilesSelected(Array.sortBy (fun e -> e.Path) picked)
            picked <- Array.empty<FsEntry>

          closePicker())

      let locationText = directory |> ValueOption.defaultValue "This PC"

      let filterText = String.Join(", ", props.filters)
      let filterMessage = $"No {filterText} files in this folder"

      let emptyTitle, emptyDescription =
        if directory.IsNone then
          "No drives found", "No ready drives on this system"
        else
          "No media here", filterMessage

      let rows =
        if Array.isEmpty entries then
          EmptyState(
            Title = emptyTitle,
            Description = emptyDescription,
            MinHeight = Nullable(lengthFromFloat 160.0)
          )
            .Build()
          |> Array.singleton
        else
          entries
          |> Array.map(fun e ->
            entryRow(mode, picked, e, loadDirectory, toggle))

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
              actionButton("Up", goUp),
              actionButton("Cancel", closePicker),
              actionButton(confirmLabel, confirm)
            ),
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
            .Children(rows)
        )

// Infrastructure comes in through the environment; data and events arrive
// as separate parameters.
let create<'env when 'env :> IProvideFileSystem and 'env :> IProvidePostUI>
  (
    env: 'env,
    props: FilePickerProps,
    onFilesSelected: FsEntry[] -> unit,
    onFolderSelected: string -> unit
  ) : FilePickerWidget =
  let picker = PickerCell(env, props, onFilesSelected, onFolderSelected)

  {
    cell = picker :> Cell
    openPicker = fun mode -> picker.Open mode
  }
