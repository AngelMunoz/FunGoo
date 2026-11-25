module FunGoo.Metro.Tests.TreeViewMotionTests

open System
open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private nodes() : MetroTreeNode array = [|
  MetroTreeView.branch(
    "root",
    "Root",
    [| MetroTreeView.leaf("a", "A"); MetroTreeView.leaf("b", "B") |]
  )
|]

type private TreeHarness = {
  Host: HeadlessWindowHost
  Root: CaptureCell
  Cell: unit -> MetroTreeViewCell
  SetExpanded: string array -> unit
  SetSelected: string array -> unit
}

let private setUpWith(initiallyExpanded: string array) : TreeHarness =
  let mutable props = {
    MetroTreeView.defaults(nodes()) with
        ExpandedIds = initiallyExpanded
  }

  let mutable cellRef: MetroTreeViewCell voption = ValueNone

  let factory() : MetroTreeViewCell =
    let cell = MetroTreeView.create ignore
    cellRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      Cell.Mount<MetroTreeViewProps, MetroTreeViewCell>("tree", props, factory))

  let host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  {
    Host = host
    Root = root
    Cell = fun () -> cellRef.Value
    SetExpanded =
      fun ids ->
        props <- { props with ExpandedIds = ids }
        root.Rebuild()
        pumpFrames(host, 1)
    SetSelected =
      fun ids ->
        props <- { props with SelectedIds = ids }
        root.Rebuild()
        pumpFrames(host, 1)
  }

let private setUp() : TreeHarness = setUpWith(Array.empty)

[<Fact>]
let ``Expanding a branch animates the tree surface``() =
  let harness = setUp()
  let cell = harness.Cell()

  Assert.False(cell.ContentOpacity.Running)
  Assert.False(cell.ContentTranslate.Running)

  harness.SetExpanded [| "root" |]

  Assert.True(cell.ContentOpacity.Running)
  Assert.True(cell.ContentTranslate.Running)

[<Fact>]
let ``Collapsing a branch animates the tree surface``() =
  let harness = setUpWith [| "root" |]
  let cell = harness.Cell()

  Assert.False(cell.ContentOpacity.Running)
  Assert.False(cell.ContentTranslate.Running)

  harness.SetExpanded [||]

  Assert.True(cell.ContentOpacity.Running)
  Assert.True(cell.ContentTranslate.Running)

[<Fact>]
let ``Expanding under reduced motion snaps instantly``() =
  let harness = setUp()
  let cell = harness.Cell()

  harness.Host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(harness.Host, 1)
  harness.SetExpanded [| "root" |]

  Assert.False(cell.ContentOpacity.Running)
  Assert.False(cell.ContentTranslate.Running)
  Assert.Equal(1.0, cell.ContentOpacity.Value, 3)
  Assert.Equal(0.0, cell.ContentTranslate.Value.X, 3)
  Assert.Equal(0.0, cell.ContentTranslate.Value.Y, 3)

[<Fact>]
let ``Selecting without expanding starts no animation``() =
  let harness = setUp()
  let cell = harness.Cell()

  harness.SetSelected [| "a" |]

  Assert.False(cell.ContentOpacity.Running)
  Assert.False(cell.ContentTranslate.Running)
  Assert.Equal(1.0, cell.ContentOpacity.Value, 3)
