module FunGoo.Metro.Tests.SelectionControlsTests

open System
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let widePivotItems =
  [| 1..14 |]
  |> Array.map(fun i -> {
    Id = $"sec-{i}"
    Header = $"section number {i}"
  })

let sampleGroups: MetroLongListGroup array = [|
  {
    Key = "a"
    Title = "a"
    Items = [|
      {
        Id = "ada"
        Label = "Ada Lovelace"
        Caption = ValueSome "Analytical Engine"
        Disabled = false
      }
      {
        Id = "alan"
        Label = "Alan Turing"
        Caption = ValueNone
        Disabled = false
      }
    |]
  }
  {
    Key = "b"
    Title = "b"
    Items = Array.empty
  }
  {
    Key = "g"
    Title = "g"
    Items = [|
      {
        Id = "grace"
        Label = "Grace Hopper"
        Caption = ValueSome "Compilers"
        Disabled = false
      }
    |]
  }
|]

[<Fact>]
let ``MetroPivot pans its header strip when off-screen tabs are selected and on wheel``
  ()
  =
  use host = createHost(420, 320)
  let mutable selected = "sec-1"
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let pivotFactory() =
    let cell =
      MetroPivot.create(
        (fun id -> selected <- id),
        fun id -> Text(Content = id) :> Blob
      )

    pivotRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      Container(Width = Length.Percent 100.0, Height = Length.Percent 100.0)
        .Children(
          MetroPivot.mount(
            "pan-pivot",
            MetroPivot.defaults(widePivotItems, selected),
            pivotFactory
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let pivot =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to mount."

  Assert.True(
    pivot.HeaderScrollHandle.ScrollRange.X > 0.0,
    $"Expected positive horizontal scroll range, got {pivot.HeaderScrollHandle.ScrollRange.X}."
  )

  Assert.Equal(0.0, pivot.HeaderScrollHandle.ScrollOffset.X, 3)

  // Selecting the last off-screen tab pans the header strip automatically.
  selected <- "sec-14"
  root.Rebuild()
  pumpFrames(host, 4)

  Assert.True(
    pivot.HeaderScrollHandle.ScrollOffset.X > 100.0,
    $"Expected header strip to pan toward sec-14, got {pivot.HeaderScrollHandle.ScrollOffset.X}."
  )

  // Wheel delta on the header strip pans horizontally without a visible scrollbar.
  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- pivot.View)
  let headerStrip = childAt(tree, 0) :?> Container
  let beforeWheel = pivot.HeaderScrollHandle.ScrollOffset.X
  headerStrip.OnWheel.Invoke(WheelEvent(Delta = Point(X = 0.0, Y = 2.0)))
  pumpFrames(host, 2)
  Assert.True(pivot.HeaderScrollHandle.ScrollOffset.X < beforeWheel)

[<Fact>]
let ``MetroLongListSelector flattens non-empty groups and disables empty jump tiles``
  ()
  =
  let input = {
    MetroLongListSelector.defaults sampleGroups with
        SelectedIds = [| "grace" |]
  }

  let rows = MetroLongListSelector.flattenRows input
  // Group "a" (1 header + 2 items) + Group "g" (1 header + 1 item) = 5 rows; empty "b" omitted from list.
  Assert.Equal(5, rows.Length)

  match rows[0] with
  | MetroLongListRow.Header(key, title, interactive) ->
    Assert.Equal("a", key)
    Assert.Equal("a", title)
    Assert.True(interactive)
  | _ -> failwith "Expected first row to be header 'a'."

  match rows[4] with
  | MetroLongListRow.Item(item, selected) ->
    Assert.Equal("grace", item.Id)
    Assert.True(selected)
  | _ -> failwith "Expected fifth row to be selected item 'grace'."

  // In the jump grid, empty group "b" is rendered disabled while "a" and "g" are active.
  use host = createHost(400, 300)
  let mutable jumped = ""
  let mutable cancelled = false
  let mutable gridTree: Blob = null

  let root =
    CaptureCell.create(fun () ->
      gridTree <-
        MetroLongListSelector.buildJumpGrid(
          input,
          (fun key -> jumped <- key),
          fun () -> cancelled <- true
        )

      gridTree)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tileA = requireBlobByKey(gridTree, "lls-jump-a") :?> Button
  let tileB = requireBlobByKey(gridTree, "lls-jump-b") :?> Button
  let tileG = requireBlobByKey(gridTree, "lls-jump-g") :?> Button

  Assert.False(tileA.Disabled)
  Assert.True(tileB.Disabled)
  Assert.False(tileG.Disabled)

  tileG.OnClick.Invoke()
  Assert.Equal("g", jumped)

  let esc = findKeyBinding((gridTree :?> Container).KeyBindings, Key.Escape)
  esc.OnPress.Invoke()
  Assert.True(cancelled)

[<Fact>]
let ``MetroLongListSelectorCell rebuilds host via owner rule and animates jump transitions``
  ()
  =
  use host = createHost(400, 360)
  let mutable jumpOpen = false
  let mutable lastJump = ""
  let mutable selectedIds = [| "ada" |]
  let mutable cellRef: MetroLongListSelectorCell voption = ValueNone

  let factory() =
    let cell =
      MetroLongListSelector.create(function
        | MetroLongListEvent.SelectionChanged ids -> selectedIds <- ids
        | MetroLongListEvent.ItemClick _ -> ()
        | MetroLongListEvent.JumpOpenChanged isOpen -> jumpOpen <- isOpen
        | MetroLongListEvent.GroupJumped key -> lastJump <- key)

    cellRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroLongListSelector.mount(
        "lls",
        {
          MetroLongListSelector.defaults sampleGroups with
              SelectedIds = selectedIds
              JumpOpen = jumpOpen
              Height = ValueSome(Length.Pixels 240.0)
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let cell =
    match cellRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroLongListSelectorCell to mount."

  let buildsBefore = root.Builds
  cell.Handle(MetroLongListEvent.JumpOpenChanged true)
  pumpFrames(host, 2)

  Assert.True(jumpOpen)
  Assert.True(cell.IsJumpOpen)
  Assert.Equal(buildsBefore + 1, root.Builds)
  Assert.True(cell.ScaleAnim.Running)
  Assert.True(cell.OpacityAnim.Running)

  cell.Handle(MetroLongListEvent.GroupJumped "g")
  pumpFrames(host, 2)

  Assert.False(jumpOpen)
  Assert.False(cell.IsJumpOpen)
  Assert.Equal("g", lastJump)

[<Fact>]
let ``MetroListPicker resolves Auto mode by threshold and rebuilds host via owner rule``
  ()
  =
  let threeItems: MetroListPickerItem array =
    [| 1..3 |]
    |> Array.map(fun i -> {
      Id = $"i-{i}"
      Label = $"Item {i}"
      IconName = ValueNone
      Disabled = false
    })

  let sixItems: MetroListPickerItem array =
    [| 1..6 |]
    |> Array.map(fun i -> {
      Id = $"i-{i}"
      Label = $"Item {i}"
      IconName = ValueNone
      Disabled = false
    })

  Assert.Equal(
    MetroListPickerMode.Inline,
    MetroListPicker.resolveMode(MetroListPicker.defaults(threeItems, "i-1"))
  )

  Assert.Equal(
    MetroListPickerMode.Overlay,
    MetroListPicker.resolveMode(MetroListPicker.defaults(sixItems, "i-1"))
  )

  use host = createHost(480, 400)
  let mutable isOpen = false
  let mutable chosen = "i-1"
  let mutable pickerRef: MetroListPickerCell voption = ValueNone

  let factory() =
    let cell =
      MetroListPicker.create(function
        | MetroListPickerEvent.Selected id -> chosen <- id
        | MetroListPickerEvent.OpenChanged next -> isOpen <- next)

    pickerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroListPicker.mount(
        "picker",
        {
          MetroListPicker.defaults(threeItems, chosen) with
              Open = isOpen
              Header = ValueSome "CHOOSE"
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let picker =
    match pickerRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroListPickerCell to mount."

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- picker.View)
  let buildsBefore = root.Builds
  let trigger = requireBlobByKey(tree, "list-picker-trigger") :?> Button
  trigger.OnClick.Invoke()
  pumpFrames(host, 2)

  Assert.True(isOpen)
  Assert.True(picker.IsOpen)
  Assert.Equal(buildsBefore + 1, root.Builds)

  root.DuringBuild(host, fun () -> tree <- picker.View)
  let option2 = requireBlobByKey(tree, "list-picker-item-i-2") :?> Button
  option2.OnClick.Invoke()
  pumpFrames(host, 2)

  Assert.Equal("i-2", chosen)
  Assert.False(isOpen)

[<Fact>]
let ``MetroTreeView caches mapped TreeNode arrays and callback delegates with zero churn``
  ()
  =
  let nodes = [|
    {
      MetroTreeView.branch(
        "root",
        "Root",
        [| MetroTreeView.leaf("child-1", "Child 1") |]
      ) with
          IconName = ValueSome "folder"
    }
  |]

  let mutable selected: string array = Array.empty
  let mutable expanded: string array = Array.empty

  let factory =
    MetroTreeView.createFactory(function
      | MetroTreeViewEvent.SelectionChanged ids -> selected <- ids
      | MetroTreeViewEvent.ExpandedChanged(id, isExp) ->
        expanded <- MetroTreeView.applyExpanded(expanded, id, isExp))

  let props = {
    MetroTreeView.defaults nodes with
        SelectedIds = [| "child-1" |]
        ExpandedIds = [| "root" |]
  }

  let input1 = factory.CreateInput props
  let input2 = factory.CreateInput props

  Assert.True(obj.ReferenceEquals(input1.Nodes, input2.Nodes))

  Assert.True(
    obj.ReferenceEquals(input1.OnSelectionChange, input2.OnSelectionChange)
  )

  Assert.True(
    obj.ReferenceEquals(input1.OnExpandedChange, input2.OnExpandedChange)
  )

  Assert.True(obj.ReferenceEquals(input1.ConfigureRow, input2.ConfigureRow))

  // Measure allocation of repeated CreateInput with unchanged node array.
  for _ = 1 to 10 do
    factory.CreateInput props |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
  let _ = factory.CreateInput props
  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes
  Assert.True(allocated <= 64L, $"Expected <= 64 B, allocated {allocated} B.")

  input1.OnExpandedChange.Invoke("root", true)
  Assert.Equal<string array>([| "root" |], expanded)
  input1.OnExpandedChange.Invoke("root", false)
  Assert.Empty(expanded)
  input1.OnSelectionChange.Invoke([| "child-1" |])
  Assert.Equal<string array>([| "child-1" |], selected)

[<Fact>]
let ``the pivot header wheel stays clamped at the two ends``() =
  use host = createHost(420, 320)
  let mutable selected = "sec-1"
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let pivotFactory() =
    let cell =
      MetroPivot.create(
        (fun id -> selected <- id),
        fun id -> Text(Content = id) :> Blob
      )

    pivotRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      Container(Width = Length.Percent 100.0, Height = Length.Percent 100.0)
        .Children(
          MetroPivot.mount(
            "pan-pivot",
            MetroPivot.defaults(widePivotItems, selected),
            pivotFactory
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let pivot =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to mount."

  Assert.Equal(0.0, pivot.HeaderScrollHandle.ScrollOffset.X, 3)

  // A wheel up at offset 0 must leave the offset at 0.
  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- pivot.View)
  let headerStrip = childAt(tree, 0) :?> Container
  headerStrip.OnWheel.Invoke(WheelEvent(Delta = Point(X = 0.0, Y = 2.0)))
  pumpFrames(host, 2)
  Assert.Equal(0.0, pivot.HeaderScrollHandle.ScrollOffset.X, 3)

  // A wheel down pans to the end of the strip range.
  headerStrip.OnWheel.Invoke(WheelEvent(Delta = Point(X = 0.0, Y = -100.0)))
  pumpFrames(host, 2)

  Assert.Equal(
    pivot.HeaderScrollHandle.ScrollRange.X,
    pivot.HeaderScrollHandle.ScrollOffset.X,
    3
  )

  // A further wheel down at the end must leave the offset at the end.
  headerStrip.OnWheel.Invoke(WheelEvent(Delta = Point(X = 0.0, Y = -100.0)))
  pumpFrames(host, 2)

  Assert.Equal(
    pivot.HeaderScrollHandle.ScrollRange.X,
    pivot.HeaderScrollHandle.ScrollOffset.X,
    3
  )
