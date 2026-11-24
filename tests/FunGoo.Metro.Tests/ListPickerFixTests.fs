module FunGoo.Metro.Tests.ListPickerFixTests

open System
open System.Collections
open System.Reflection
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private nodeFlags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

let private mountedChildCount(window: Window, key: string) : int =
  let windowNode = typeof<Window>.GetField("node", nodeFlags).GetValue(window)

  let found = ResizeArray<obj>()
  let pending = ResizeArray<obj>()
  pending.Add windowNode
  let mutable head = 0

  while head < pending.Count do
    let node = pending[head]
    head <- head + 1
    let nodeType = node.GetType()

    let nodeKey =
      nodeType.GetField("<Key>k__BackingField", nodeFlags).GetValue(node)
      :?> string

    let kind = nodeType.GetProperty("Kind", nodeFlags).GetValue(node).ToString()

    if nodeKey = key && kind = "Button" then
      found.Add node

    let children =
      nodeType.GetField("<Children>k__BackingField", nodeFlags).GetValue(node)
      :?> IList

    if not(isNull children) then
      for i = 0 to children.Count - 1 do
        pending.Add children[i]

  if found.Count = 0 then
    failwith $"Expected a Button node with key '{key}'."

  let children =
    found[0].GetType().GetProperty("Children", nodeFlags).GetValue(found[0])
    :?> IList

  children.Count

let pickerItems(count: int) : MetroListPickerItem array =
  [| 1..count |]
  |> Array.map(fun i -> {
    Id = $"i-{i}"
    Label = $"Item {i}"
    IconName = ValueNone
    Disabled = false
  })

// Ceiling in bytes for one view read of an open overlay with 1000 items. Two
// times the measured 22104 B in Release and 23072 B in Debug on 2026-11-24.
let overlayViewAllocationCeiling = 47104L

// Ceiling in bytes for one warm call of the inline view builder with five
// items. Two times the measured 26792 B in Release and 27960 B in Debug on
// 2026-11-24.
let inlineViewAllocationCeiling = 56320L

let mountPicker
  (
    host: EmbeddedWindowHost,
    input: MetroListPickerInput,
    onEvent: MetroListPickerEvent -> unit
  ) : CaptureCell * MetroListPickerCell =
  let mutable pickerRef: MetroListPickerCell voption = ValueNone

  let factory() =
    let cell = MetroListPicker.create onEvent
    pickerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroListPicker.mount("picker", input, factory))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 4)

  let picker =
    match pickerRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroListPickerCell to mount."

  (root, picker)

[<Fact>]
let ``the overlay sheet stays inside a small window``() =
  use host = createHost(600, 400)

  let _, picker =
    mountPicker(
      host,
      {
        MetroListPicker.defaults(pickerItems 1000, "i-1") with
            Mode = MetroListPickerMode.Overlay
            Open = true
      },
      fun _ -> ()
    )

  let box = picker.SheetHandle.BorderBox

  Assert.True(box.Y >= 0.0, $"Expected the sheet top at Y >= 0, got {box.Y}.")

  Assert.True(
    box.Y + box.Height <= 400.0,
    $"Expected the sheet bottom at Y + Height <= 400, got {box.Y + box.Height}."
  )

[<Fact>]
let ``overlay mounts with many items and reads as a list``() =
  use host = createHost(600, 400)

  let input = {
    MetroListPicker.defaults(pickerItems 1000, "i-1") with
        Mode = MetroListPickerMode.Overlay
        Open = true
  }

  let root, picker = mountPicker(host, input, fun _ -> ())

  Assert.True(picker.IsOpen)
  Assert.Equal(MetroListPickerMode.Overlay, picker.ResolvedMode)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- picker.View)

  requireBlobByKey(tree, "list-picker-portal") |> ignore

  // The overlay content behind the portal, through the public view seam.
  let state: MetroListPickerState = {
    Input = input
    ResolvedMode = MetroListPickerMode.Overlay
    PanelTranslate = root.Track
    PanelOpacity = root.Alpha
    SheetHandle = ElementHandle()
  }

  let mutable overlay: Blob = null

  root.DuringBuild(
    host,
    fun () -> overlay <- MetroListPicker.buildOverlay(state, fun _ -> ())
  )

  requireBlobByKey(overlay, "list-picker-backdrop") |> ignore
  let sheet = requireBlobByKey(overlay, "list-picker-overlay-sheet")
  requireBlobByKey(overlay, "list-picker-close") |> ignore

  // The option list inside the sheet reads as one list.
  let listChildren =
    [ 0 .. childCount(sheet) - 1 ]
    |> List.filter(fun i ->
      childAt(sheet, i).AccessibilityRole = AccessibilityRole.List)

  Assert.Equal(1, listChildren.Length)

[<Fact>]
let ``one view of an open overlay with 1000 items allocates below its ceiling``
  ()
  =
  use host = createHost(600, 400)

  let root, picker =
    mountPicker(
      host,
      {
        MetroListPicker.defaults(pickerItems 1000, "i-1") with
            Mode = MetroListPickerMode.Overlay
            Open = true
      },
      fun _ -> ()
    )

  let mutable allocated = 0L

  root.DuringBuild(
    host,
    fun () ->
      let before = GC.GetAllocatedBytesForCurrentThread()
      let _ = picker.View
      allocated <- GC.GetAllocatedBytesForCurrentThread() - before
  )

  Assert.True(
    allocated <= overlayViewAllocationCeiling,
    $"Expected <= {overlayViewAllocationCeiling} B for one overlay view, allocated {allocated} B."
  )

[<Fact>]
let ``an overlay option reports the selection and closes the picker``() =
  use host = createHost(600, 400)
  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 1)

  let item: MetroListPickerItem = {
    Id = "b"
    Label = "Beta"
    IconName = ValueNone
    Disabled = false
  }

  let mutable received: MetroListPickerEvent list = []

  let mutable row: Blob = null

  root.DuringBuild(
    host,
    fun () ->
      row <-
        MetroListPicker.buildOverlayRow(
          item,
          "",
          fun ev -> received <- received @ [ ev ]
        )
  )

  let button = row :?> Button
  button.OnClick.Invoke()

  assertListEqual(
    [
      MetroListPickerEvent.Selected "b"
      MetroListPickerEvent.OpenChanged false
    ],
    received
  )

[<Fact>]
let ``the trigger closes the inline list on escape``() =
  use host = createHost(480, 400)
  let mutable received: MetroListPickerEvent voption = ValueNone

  let root, picker =
    mountPicker(
      host,
      {
        MetroListPicker.defaults(pickerItems 5, "i-1") with
            Mode = MetroListPickerMode.Inline
            Open = true
      },
      fun ev -> received <- ValueSome ev
    )

  Assert.True(picker.IsOpen)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- picker.View)

  let trigger = requireBlobByKey(tree, "list-picker-trigger") :?> Button
  let esc = findKeyBinding(trigger.KeyBindings, Key.Escape)
  esc.OnPress.Invoke()

  Assert.Equal(ValueSome(MetroListPickerEvent.OpenChanged false), received)

  // A closed picker gives the trigger no Escape binding.
  use host2 = createHost(480, 400)

  let root2, picker2 =
    mountPicker(
      host2,
      {
        MetroListPicker.defaults(pickerItems 5, "i-1") with
            Mode = MetroListPickerMode.Inline
            Open = false
      },
      fun _ -> ()
    )

  let mutable tree2: Blob = null
  root2.DuringBuild(host2, fun () -> tree2 <- picker2.View)

  let trigger2 = requireBlobByKey(tree2, "list-picker-trigger") :?> Button

  Assert.True(
    trigger2.KeyBindings
    |> Array.forall(fun binding -> binding.Key <> Key.Escape),
    "Expected no Escape binding on the trigger while the picker is closed."
  )

[<Fact>]
let ``changing SelectedId while the overlay is open updates the row visuals``
  ()
  =
  use host = createHost(600, 400)
  let items = pickerItems 10
  let mutable selectedId = "i-1"
  let mutable pickerRef: MetroListPickerCell voption = ValueNone

  let factory() =
    let cell = MetroListPicker.create(fun _ -> ())
    pickerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroListPicker.mount(
        "picker",
        {
          MetroListPicker.defaults(items, selectedId) with
              Mode = MetroListPickerMode.Overlay
              Open = true
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 4)

  match pickerRef with
  | ValueNone -> failwith "Expected MetroListPickerCell to mount."
  | ValueSome _ -> ()

  // A selected row shows the check mark next to its label; an unselected
  // row shows the label alone.
  Assert.Equal(2, mountedChildCount(window, "i-1"))
  Assert.Equal(1, mountedChildCount(window, "i-2"))

  // The same Items array stays mounted; only the selection changes.
  selectedId <- "i-2"
  root.Rebuild()
  pumpFrames(host, 4)

  Assert.Equal(1, mountedChildCount(window, "i-1"))
  Assert.Equal(2, mountedChildCount(window, "i-2"))

[<Fact>]
let ``an inline view with five items allocates below its ceiling``() =
  use host = createHost(480, 400)
  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 1)

  let state: MetroListPickerState = {
    Input = {
      MetroListPicker.defaults(pickerItems 5, "i-1") with
          Mode = MetroListPickerMode.Inline
          Open = true
    }
    ResolvedMode = MetroListPickerMode.Inline
    PanelTranslate = root.Track
    PanelOpacity = root.Alpha
    SheetHandle = ElementHandle()
  }

  let onEvent(_event: MetroListPickerEvent) : unit = ()
  let mutable allocated = 0L

  root.DuringBuild(
    host,
    fun () ->
      for _ = 1 to 10 do
        MetroListPicker.buildView(state, onEvent) |> ignore

      let before = GC.GetAllocatedBytesForCurrentThread()
      let _ = MetroListPicker.buildView(state, onEvent)
      allocated <- GC.GetAllocatedBytesForCurrentThread() - before
  )

  Assert.True(
    allocated <= inlineViewAllocationCeiling,
    $"Expected <= {inlineViewAllocationCeiling} B for one inline view, allocated {allocated} B."
  )
