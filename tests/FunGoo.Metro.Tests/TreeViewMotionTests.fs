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

[<Fact>]
let ``Collapsing and expanding through the widget callback rebuilds the host``
  ()
  =
  let treeNodes = nodes()
  let mutable selected: string array = Array.empty
  let mutable expanded: string array = [| "root" |]
  let mutable lastExpanded: string array = Array.empty

  // The host holds the expanded state, like the gallery: each build reads it
  // and the event handler updates it.
  let factory =
    MetroTreeView.createFactory(function
      | MetroTreeViewEvent.SelectionChanged ids -> selected <- ids
      | MetroTreeViewEvent.ExpandedChanged(id, isExp) ->
        expanded <- MetroTreeView.applyExpanded(expanded, id, isExp))

  let root =
    CaptureCell.create(fun () ->
      lastExpanded <- expanded

      factory.Mount(
        "tree",
        {
          MetroTreeView.defaults treeNodes with
              SelectedIds = selected
              ExpandedIds = expanded
        }
      ))

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  Assert.Equal<string array>([| "root" |], lastExpanded)

  // The mounted widget holds the same cached delegates the factory hands
  // out, so invoking them is the widget callback path.
  let widgetInput() =
    factory.CreateInput {
      MetroTreeView.defaults treeNodes with
          SelectedIds = selected
          ExpandedIds = expanded
    }

  let buildsBefore = root.Builds
  (widgetInput()).OnExpandedChange.Invoke("root", false)
  pumpFrames(host, 2)

  Assert.Empty(expanded)
  Assert.True(root.Builds > buildsBefore)
  Assert.Empty(lastExpanded)

  let buildsAfterCollapse = root.Builds
  (widgetInput()).OnExpandedChange.Invoke("root", true)
  pumpFrames(host, 2)

  Assert.Equal<string array>([| "root" |], expanded)
  Assert.True(root.Builds > buildsAfterCollapse)
  Assert.Equal<string array>([| "root" |], lastExpanded)

  (widgetInput()).OnSelectionChange.Invoke([| "a" |])
  pumpFrames(host, 2)
  Assert.Equal<string array>([| "a" |], selected)
