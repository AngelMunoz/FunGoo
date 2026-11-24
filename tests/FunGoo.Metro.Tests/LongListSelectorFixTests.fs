module FunGoo.Metro.Tests.LongListSelectorFixTests

open System
open System.Collections
open System.Reflection
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let jumpGroups: MetroLongListGroup array = [|
  for letter in 'a' .. 'j' ->
    {
      Key = string letter
      Title = string letter
      Items =
        [| 1..50 |]
        |> Array.map(fun i -> {
          Id = $"{letter}-item-{i}"
          Label = $"Item {letter}{i}"
          Caption = ValueNone
          Disabled = false
        })
    }
|]

let tileGroups: MetroLongListGroup array = [|
  {
    Key = "a"
    Title = "a"
    Items = [|
      {
        Id = "ada"
        Label = "Ada Lovelace"
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
        Caption = ValueNone
        Disabled = false
      }
    |]
  }
|]

let flattenGroups: MetroLongListGroup array =
  [| 0..199 |]
  |> Array.map(fun g -> {
    Key = $"g{g}"
    Title = $"Group {g}"
    Items =
      [| 1..50 |]
      |> Array.map(fun i -> {
        Id = $"g{g}-item-{i}"
        Label = $"Item {g}/{i}"
        Caption = ValueNone
        Disabled = false
      })
  })

let flattenSelectedIds: string array =
  [| 0..199 |]
  |> Array.collect(fun g ->
    [| 2..2..50 |] |> Array.map(fun i -> $"g{g}-item-{i}"))

/// Ceiling in bytes for one flattenRows call. Two times the measured
/// allocation after the flatten change (769752 B), rounded up to a multiple
/// of 1024.
let flattenAllocationCeiling = 1540096L

let mountCell
  (host: EmbeddedWindowHost, groups: MetroLongListGroup array)
  : CaptureCell * MetroLongListSelectorCell * (bool -> unit) =

  let mutable jumpOpen = false
  let mutable cellRef: MetroLongListSelectorCell voption = ValueNone

  let factory() =
    let cell =
      MetroLongListSelector.create(function
        | MetroLongListEvent.JumpOpenChanged isOpen -> jumpOpen <- isOpen
        | _ -> ())

    cellRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroLongListSelector.mount(
        "lls",
        {
          MetroLongListSelector.defaults groups with
              JumpOpen = jumpOpen
              Height = ValueSome(Length.Pixels 240.0)
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let cell =
    match cellRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroLongListSelectorCell to mount."

  root, cell, (fun value -> jumpOpen <- value)

[<Fact>]
let ``jump grid keeps the list mounted and its scroll offset``() =
  use host = createHost(600, 400)
  let root, cell, setJumpOpen = mountCell(host, jumpGroups)

  cell.ScrollHandle.JumpTo(y = Nullable 1200.0) |> ignore
  pumpFrames(host, 3)

  let before = cell.ScrollHandle.ScrollOffset.Y
  Assert.True(before > 0.0, $"Expected a positive scroll offset, got {before}.")

  setJumpOpen true
  root.Rebuild()
  pumpFrames(host, 3)

  Assert.True(cell.IsJumpOpen)

  Assert.Equal(before, cell.ScrollHandle.ScrollOffset.Y, 3)

  setJumpOpen false
  root.Rebuild()
  pumpFrames(host, 3)

  Assert.Equal(before, cell.ScrollHandle.ScrollOffset.Y, 3)

[<Fact>]
let ``a group tile click scrolls to the group at once and leaves no late jump``
  ()
  =
  use host = createHost(600, 400)
  let root, cell, setJumpOpen = mountCell(host, jumpGroups)

  setJumpOpen true
  root.Rebuild()
  pumpFrames(host, 3)
  Assert.True(cell.IsJumpOpen)

  cell.Handle(MetroLongListEvent.GroupJumped "g")

  let immediate = cell.ScrollHandle.ScrollOffset.Y

  Assert.True(
    immediate > 0.0,
    $"Expected an immediate scroll to group 'g', got {immediate}."
  )

  root.Rebuild()
  pumpFrames(host, 3)

  let jumped = cell.ScrollHandle.ScrollOffset.Y

  Assert.True(
    jumped > 0.0,
    $"Expected the offset to survive the grid close, got {jumped}."
  )

  cell.ScrollHandle.JumpTo(y = Nullable 0.0) |> ignore
  pumpFrames(host, 2)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- cell.View)
  pumpFrames(host, 3)

  Assert.Equal(0.0, cell.ScrollHandle.ScrollOffset.Y, 3)

[<Fact>]
let ``flattenRows marks the selected items and stays within its allocation ceiling``
  ()
  =
  let input = {
    MetroLongListSelector.defaults flattenGroups with
        SelectedIds = flattenSelectedIds
  }

  let rows = MetroLongListSelector.flattenRows input
  Assert.Equal(10200, rows.Length)

  let selectedCount =
    rows
    |> Array.sumBy(function
      | MetroLongListRow.Item(_, selected) -> if selected then 1 else 0
      | MetroLongListRow.Header _ -> 0)

  Assert.Equal(5000, selectedCount)

  [| 0..199 |]
  |> Array.iter(fun g ->
    match rows[g * 51] with
    | MetroLongListRow.Header(key, _, _) -> Assert.Equal($"g{g}", key)
    | _ -> failwith $"Expected row {g * 51} to be the header of group g{g}.")

  for _ = 1 to 10 do
    MetroLongListSelector.flattenRows input |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
  let _ = MetroLongListSelector.flattenRows input
  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes

  Assert.True(
    allocated <= flattenAllocationCeiling,
    $"Expected <= {flattenAllocationCeiling} B, allocated {allocated} B."
  )

[<Fact>]
let ``an empty jump grid takes the focus so that escape closes it``() =
  use host = createHost(400, 300)

  let emptyGroups =
    tileGroups |> Array.map(fun group -> { group with Items = Array.empty })

  let mutable emptyGrid: Blob = null

  let root =
    CaptureCell.create(fun () ->
      emptyGrid <-
        MetroLongListSelector.buildJumpGrid(
          {
            MetroLongListSelector.defaults emptyGroups with
                JumpOpen = true
          },
          (fun _ -> ()),
          fun () -> ()
        )

      emptyGrid)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  Assert.True(
    emptyGrid.Focusable,
    "Expected the empty grid container to be focusable."
  )

  Assert.True(
    emptyGrid.AutoFocus,
    "Expected the empty grid container to take the focus."
  )

  let singleGroup: MetroLongListGroup array = [| tileGroups[0] |]
  use host2 = createHost(400, 300)
  let mutable filledGrid: Blob = null

  let root2 =
    CaptureCell.create(fun () ->
      filledGrid <-
        MetroLongListSelector.buildJumpGrid(
          {
            MetroLongListSelector.defaults singleGroup with
                JumpOpen = true
          },
          (fun _ -> ()),
          fun () -> ()
        )

      filledGrid)

  let window2 = Window(Root = root2)
  Metro.attach(window2, darkConfig)
  window2.Attach(host2) |> ignore
  pumpFrames(host2, 2)

  Assert.False(
    filledGrid.Focusable,
    "Expected the grid container to leave the focus to its tiles."
  )

  Assert.False(
    filledGrid.AutoFocus,
    "Expected an enabled tile to take the focus, not the grid container."
  )

[<Fact>]
let ``the first enabled jump tile takes the focus when the grid opens``() =
  use host = createHost(400, 300)
  let mutable gridTree: Blob = null

  let root =
    CaptureCell.create(fun () ->
      gridTree <-
        MetroLongListSelector.buildJumpGrid(
          {
            MetroLongListSelector.defaults tileGroups with
                JumpOpen = true
          },
          (fun _ -> ()),
          fun () -> ()
        )

      gridTree)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tileA = requireBlobByKey(gridTree, "lls-jump-a") :?> Button
  let tileB = requireBlobByKey(gridTree, "lls-jump-b") :?> Button
  let tileG = requireBlobByKey(gridTree, "lls-jump-g") :?> Button

  Assert.True(tileA.AutoFocus)
  Assert.False(tileB.AutoFocus)
  Assert.False(tileG.AutoFocus)

  let emptyGroups =
    tileGroups |> Array.map(fun group -> { group with Items = Array.empty })

  use host2 = createHost(400, 300)
  let mutable emptyTree: Blob = null

  let root2 =
    CaptureCell.create(fun () ->
      emptyTree <-
        MetroLongListSelector.buildJumpGrid(
          {
            MetroLongListSelector.defaults emptyGroups with
                JumpOpen = true
          },
          (fun _ -> ()),
          fun () -> ()
        )

      emptyTree)

  let window2 = Window(Root = root2)
  Metro.attach(window2, darkConfig)
  window2.Attach(host2) |> ignore
  pumpFrames(host2, 2)

  [| "lls-jump-a"; "lls-jump-b"; "lls-jump-g" |]
  |> Array.iter(fun key ->
    let tile = requireBlobByKey(emptyTree, key) :?> Button
    Assert.False(tile.AutoFocus))

let scaledGeometryGroups: MetroLongListGroup array = [|
  {
    Key = "a"
    Title = "a"
    Items = [|
      {
        Id = "ada"
        Label = "Ada Lovelace"
        Caption = ValueSome "First programmer"
        Disabled = false
      }
    |]
  }
|]

let nodeFlags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

let windowNode(window: Window) : obj =
  typeof<Window>.GetField("node", nodeFlags).GetValue(window)

let nodeKind(node: obj) : string =
  node.GetType().GetProperty("Kind", nodeFlags).GetValue(node).ToString()

let rec collectNodesByKey
  (node: obj, key: string, found: ResizeArray<obj>)
  : unit =
  if not(isNull node) then
    let nt = node.GetType()

    let nodeKey =
      nt.GetField("<Key>k__BackingField", nodeFlags).GetValue(node) :?> string

    if nodeKey = key then
      found.Add node

    let children =
      nt.GetField("<Children>k__BackingField", nodeFlags).GetValue(node)
      :?> IList

    if not(isNull children) then
      for i = 0 to children.Count - 1 do
        collectNodesByKey(children[i], key, found)

let requireButtonNode(window: Window, key: string) : obj =
  let found = ResizeArray<obj>()
  collectNodesByKey(windowNode window, key, found)

  match found |> Seq.tryFind(fun node -> nodeKind node = "Button") with
  | Some node -> node
  | _ -> failwith $"Expected a Button node with key '{key}'."

// Reads a specified NodeLength property. Blob style values are write-only,
// so the geometry goes through the mounted node instead.
let nodeLength(node: obj, name: string) : float =
  let value = node.GetType().GetProperty(name, nodeFlags).GetValue(node)

  if isNull value then
    Double.NaN
  else
    let vt = value.GetType()
    let vf = vt.GetField("Value", nodeFlags)

    let raw =
      if isNull vf then
        vt.GetProperty("Value", nodeFlags).GetValue(value)
      else
        vf.GetValue(value)

    Convert.ToDouble(raw)

// Laid-out height of the mounted node.
let nodeBoxHeight(node: obj) : float =
  let rect = node.GetType().GetField("rect", nodeFlags).GetValue(node)
  Convert.ToDouble(rect.GetType().GetField("H", nodeFlags).GetValue(rect))

let nodeTextChildren(node: obj) : obj list =
  let children =
    node.GetType().GetProperty("Children", nodeFlags).GetValue(node) :?> IList

  [
    for i = 0 to children.Count - 1 do
      let child = children[i]

      if nodeKind child = "Text" then
        yield child
  ]

let nodeContent(node: obj) : string =
  node.GetType().GetProperty("Content", nodeFlags).GetValue(node) :?> string

let checkScaledRowGeometry(scale: float) : unit =
  use host = createHost(600, 400)

  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      TextScaleFactor = Nullable scale
    )
  )

  let mutable cellRef: MetroLongListSelectorCell voption = ValueNone

  let factory() =
    let cell = MetroLongListSelector.create(fun _ -> ())
    cellRef <- ValueSome cell
    cell

  let mutable jumpOpen = false

  let root =
    CaptureCell.create(fun () ->
      MetroLongListSelector.mount(
        "lls",
        {
          MetroLongListSelector.defaults scaledGeometryGroups with
              JumpOpen = jumpOpen
              Height = ValueSome(Length.Pixels 240.0)
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  match cellRef with
  | ValueNone -> failwith "Expected MetroLongListSelectorCell to mount."
  | ValueSome _ -> ()

  let row = requireButtonNode(window, "ada")
  let rowHeight = nodeLength(row, "Height")
  Assert.Equal(44.0 * scale, rowHeight, 3)
  Assert.Equal(44.0 * scale, nodeBoxHeight row, 2)

  let texts = nodeTextChildren row
  let label = texts |> List.find(fun node -> nodeContent node = "Ada Lovelace")

  let caption =
    texts |> List.find(fun node -> nodeContent node = "First programmer")

  let labelSize = nodeLength(label, "FontSize")
  let captionSize = nodeLength(caption, "FontSize")
  Assert.Equal(14.0 * scale, labelSize, 3)
  Assert.Equal(12.0 * scale, captionSize, 3)

  Assert.True(
    labelSize + captionSize + 2.0 <= rowHeight,
    $"Expected label ({labelSize}) + caption ({captionSize}) + gap to fit in row {rowHeight} at scale {scale}."
  )

  let header = requireButtonNode(window, "lls-group-a")
  let headerHeight = nodeLength(header, "Height")
  Assert.Equal(32.0 * scale, headerHeight, 3)
  Assert.Equal(32.0 * scale, nodeBoxHeight header, 2)

  let headerLabel = nodeTextChildren header |> List.head
  let headerSize = nodeLength(headerLabel, "FontSize")
  Assert.Equal(13.0 * scale, headerSize, 3)

  Assert.True(
    headerSize <= headerHeight,
    $"Expected header text ({headerSize}) to fit in header row {headerHeight} at scale {scale}."
  )

  jumpOpen <- true
  root.Rebuild()
  pumpFrames(host, 3)

  let tile = requireButtonNode(window, "lls-jump-a")
  Assert.Equal(64.0 * scale, nodeLength(tile, "Width"), 3)
  Assert.Equal(64.0 * scale, nodeLength(tile, "Height"), 3)
  Assert.Equal(64.0 * scale, nodeBoxHeight tile, 2)

  let tileLabel = nodeTextChildren tile |> List.head
  let tileSize = nodeLength(tileLabel, "FontSize")
  Assert.Equal(20.0 * scale, tileSize, 3)

  let tileHeight = nodeLength(tile, "Height")

  Assert.True(
    tileSize <= tileHeight,
    $"Expected tile text ({tileSize}) to fit in tile {tileHeight} at scale {scale}."
  )

[<Fact>]
let ``jump tiles render uppercase titles like the list headers``() =
  use host = createHost(400, 300)
  let mutable gridTree: Blob = null

  let groups: MetroLongListGroup array = [|
    {
      Key = "aBc"
      Title = "aBc"
      Items = [|
        {
          Id = "ada"
          Label = "Ada Lovelace"
          Caption = ValueNone
          Disabled = false
        }
      |]
    }
  |]

  let root =
    CaptureCell.create(fun () ->
      gridTree <-
        MetroLongListSelector.buildJumpGrid(
          {
            MetroLongListSelector.defaults groups with
                JumpOpen = true
          },
          (fun _ -> ()),
          fun () -> ()
        )

      gridTree)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tile = requireBlobByKey(gridTree, "lls-jump-aBc") :?> Button

  Assert.Equal("ABC", tile.Accessibility.Name)
  Assert.Equal("ABC", (childAt(tile, 0) :?> Text).Content)

[<Fact>]
let ``scaled item header and jump tile fit scaled text at 1 point 5``() =
  checkScaledRowGeometry 1.5

[<Fact>]
let ``scaled item header and jump tile fit scaled text at 2 point 0``() =
  checkScaledRowGeometry 2.0
