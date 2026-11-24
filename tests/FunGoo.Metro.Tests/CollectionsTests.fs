module FunGoo.Metro.Tests.CollectionsTests

open System
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private choices = [|
  {
    Id = "small"
    Label = "Small"
    Caption = ValueNone
    Count = ValueNone
    Disabled = false
  }
  {
    Id = "medium"
    Label = "Medium"
    Caption = ValueNone
    Count = ValueNone
    Disabled = false
  }
  {
    Id = "large"
    Label = "Large"
    Caption = ValueNone
    Count = ValueNone
    Disabled = false
  }
|]

let private tiles = [|
  {
    Id = "photos"
    Label = "Photos"
    IconName = ValueSome "photo"
  }
  {
    Id = "maps"
    Label = "Maps"
    IconName = ValueSome "map"
  }
  {
    Id = "music"
    Label = "Music"
    IconName = ValueNone
  }
|]

[<Fact>]
let ``MetroListBox builds one box that holds the Metro list``() =
  let events = ResizeArray<MetroListEvent>()
  let factory = MetroListView.createFactory events.Add

  let props = {
    MetroListView.defaults choices with
        SelectedIds = [| "medium" |]
        Height = ValueSome(Length.Pixels 200.0)
  }

  let root =
    CaptureCell.create(fun () -> MetroListBox.build("box-list", props, factory))

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let box = root.Captured :?> Container
  Assert.Equal(1, childCount box)

  // The box uses the list factory, so the list keeps its callbacks.
  let input = factory.CreateInput props
  input.OnSelectionChange.Invoke([| "large" |])

  assertListEqual(
    [ MetroListEvent.SelectionChanged [| "large" |] ],
    List.ofSeq events
  )

[<Fact>]
let ``MetroGridView.createFactory builds tiles and routes selection by the latest build``
  ()
  =
  let events = ResizeArray<MetroListEvent>()
  let factory = MetroGridView.createFactory events.Add

  let mutable gridProps = MetroGridView.defaults tiles

  let root =
    CaptureCell.create(fun () -> Container().Children(factory.Build gridProps))

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  // The grid is one Virtual element, its own scroll viewport, and reports the
  // grid role.
  let container = root.Captured :?> Container
  Assert.Equal(1, childCount container)
  let grid = childAt(container, 0)
  Assert.Equal(AccessibilityRole.Grid, grid.Accessibility.Role)
  Assert.Equal("Grid view", grid.Accessibility.Name)

  // Tile trees are built inside a build, because they read the Metro theme.
  let mutable selectedTile: Button = null
  let mutable plainTile: Button = null

  root.DuringBuild(
    host,
    fun () ->
      selectedTile <- factory.BuildItem(tiles[0], true, 120.0) :?> Button
      plainTile <- factory.BuildItem(tiles[2], false, 120.0) :?> Button
  )

  // A virtual item requires its content key to equal the item key.
  Assert.Equal("photos", selectedTile.Key)
  Assert.Equal(AccessibilityRole.GridCell, selectedTile.Accessibility.Role)
  Assert.Equal("Photos", selectedTile.Accessibility.Name)
  Assert.Equal(Nullable true, selectedTile.Accessibility.Selected)
  Assert.Equal(2, childCount selectedTile)
  Assert.Equal("music", plainTile.Key)
  Assert.Equal(Nullable false, plainTile.Accessibility.Selected)
  Assert.Equal(1, childCount plainTile)

  // Multiple selection is the default. An activation toggles one tile.
  let mutable photos: Button = null
  let mutable maps: Button = null

  let buildTiles() =
    root.DuringBuild(
      host,
      fun () ->
        photos <- factory.BuildItem(tiles[0], false, 120.0) :?> Button
        maps <- factory.BuildItem(tiles[1], true, 120.0) :?> Button
    )

  buildTiles()
  photos.OnClick.Invoke()
  maps.OnClick.Invoke()

  assertListEqual(
    [
      MetroListEvent.SelectionChanged [| "photos" |]
      MetroListEvent.SelectionChanged [| "maps" |]
    ],
    List.ofSeq events
  )

  // After a build with two selected tiles, an activation removes one tile.
  events.Clear()

  gridProps <- {
    gridProps with
        SelectedIds = [| "photos"; "maps" |]
  }

  root.Rebuild()
  pumpFrames(host, 1)
  buildTiles()
  photos.OnClick.Invoke()

  assertListEqual(
    [ MetroListEvent.SelectionChanged [| "maps" |] ],
    List.ofSeq events
  )

  // Single selection: the selected tile raises nothing, another tile replaces it.
  events.Clear()

  gridProps <- {
    gridProps with
        Mode = MetroListSelectionMode.Single
        SelectedIds = [| "maps" |]
  }

  root.Rebuild()
  pumpFrames(host, 1)
  buildTiles()
  maps.OnClick.Invoke()
  photos.OnClick.Invoke()

  assertListEqual(
    [ MetroListEvent.SelectionChanged [| "photos" |] ],
    List.ofSeq events
  )

  // Activation mode raises item activation only.
  events.Clear()

  gridProps <- {
    gridProps with
        Mode = MetroListSelectionMode.Activate
  }

  root.Rebuild()
  pumpFrames(host, 1)
  buildTiles()
  photos.OnClick.Invoke()

  assertListEqual([ MetroListEvent.ItemClick "photos" ], List.ofSeq events)

[<Fact>]
let ``MetroGridView defaults select multiple selection and the default tile size``
  ()
  =
  let props = MetroGridView.defaults tiles
  Assert.Equal(MetroListSelectionMode.Multiple, props.Mode)
  Assert.Equal(0, props.SelectedIds.Length)
  Assert.Equal(MetroGridView.defaultTilePx, props.TileSize)
  Assert.True(props.Height = ValueNone)
  Assert.True(props.AccessibilityName = ValueNone)
