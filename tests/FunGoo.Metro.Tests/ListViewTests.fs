module FunGoo.Metro.Tests.ListViewTests

open System
open Xunit
open Goo
open Goo.Widgets.Data
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private items = [|
  {
    Id = "mail"
    Label = "Mail"
    Caption = ValueSome "3 new"
    Count = ValueSome 3
    Disabled = false
  }
  {
    Id = "photos"
    Label = "Photos"
    Caption = ValueNone
    Count = ValueNone
    Disabled = false
  }
  {
    Id = "maps"
    Label = ""
    Caption = ValueNone
    Count = ValueNone
    Disabled = true
  }
|]

[<Fact>]
let ``MetroListView maps props to widget data without callbacks``() =
  let props = {
    MetroListView.defaults items with
        Mode = MetroListSelectionMode.Multiple
        SelectedIds = [| "photos"; "maps" |]
        RowHeight = 52.0
        Height = ValueSome(Length.Pixels 240.0)
        AccessibilityName = ValueSome "Apps"
  }

  let input = MetroListView.toInput props
  Assert.Equal(3, input.Items.Length)

  let first = input.Items[0]
  Assert.Equal("mail", first.Id)
  Assert.Equal("Mail", first.Label)
  Assert.Equal("3 new", first.Caption)
  Assert.Equal(Nullable 3, first.Count)
  Assert.False(first.Disabled)

  // An empty label, no caption, and no count pass as null. The widget shows
  // the identifier for a null label.
  let third = input.Items[2]
  Assert.Null(third.Label)
  Assert.Null(third.Caption)
  Assert.Null(third.Count)
  Assert.True(third.Disabled)

  Assert.True(input.Multiple)
  Assert.True([| "photos"; "maps" |] = input.SelectedIds)
  Assert.Equal(Nullable 52.0, input.RowHeight)
  Assert.Equal(Nullable(Length.Pixels 240.0), input.Height)
  Assert.Equal("Apps", input.AccessibilityName)

  // The pure mapping carries no callbacks.
  Assert.Null(input.OnSelectionChange)
  Assert.Null(input.OnClick)
  Assert.Null(input.ConfigureRow)
  Assert.Null(input.ConfigureRoot)

  // Defaults: single selection, no selection, the default row height.
  let defaults = MetroListView.toInput(MetroListView.defaults items)
  Assert.False(defaults.Multiple)
  Assert.Equal(0, defaults.SelectedIds.Length)
  Assert.Equal(Nullable MetroListView.defaultRowHeightPx, defaults.RowHeight)
  Assert.True(defaults.Height = Nullable())
  Assert.Null(defaults.AccessibilityName)

  // A row height that is not finite and positive selects the default.
  let invalidHeight =
    MetroListView.toInput { props with RowHeight = Double.NaN }

  Assert.Equal(
    Nullable MetroListView.defaultRowHeightPx,
    invalidHeight.RowHeight
  )

[<Fact>]
let ``MetroListView.createFactory routes widget callbacks and keeps one callback set``
  ()
  =
  let events = ResizeArray<MetroListEvent>()
  let factory = MetroListView.createFactory events.Add

  let props = {
    MetroListView.defaults items with
        SelectedIds = [| "mail" |]
  }

  let input = factory.CreateInput props
  Assert.NotNull(input.OnSelectionChange)
  Assert.NotNull(input.OnClick)
  Assert.NotNull(input.ConfigureRow)
  Assert.NotNull(input.ConfigureRoot)

  input.OnSelectionChange.Invoke([| "photos" |])
  input.OnClick.Invoke("maps")

  // The mapped rows are the same instance while the item array is the same
  // instance, so an unchanged input does not rebuild the list.
  let sameItems =
    factory.CreateInput {
      props with
          SelectedIds = [| "photos" |]
    }

  Assert.True(obj.ReferenceEquals(input.Items, sameItems.Items))

  // A factory maps an item array once: an input with the same item array
  // allocates nothing.
  let before = GC.GetAllocatedBytesForCurrentThread()
  let cached = factory.CreateInput props
  let allocated = GC.GetAllocatedBytesForCurrentThread() - before
  Assert.Equal(0L, allocated)
  Assert.True(obj.ReferenceEquals(input.Items, cached.Items))

  let changedItems = factory.CreateInput { props with Items = [| items[0] |] }

  Assert.False(obj.ReferenceEquals(input.Items, changedItems.Items))
  Assert.Equal(1, changedItems.Items.Length)

  assertListEqual(
    [
      MetroListEvent.SelectionChanged [| "photos" |]
      MetroListEvent.ItemClick "maps"
    ],
    List.ofSeq events
  )

  // Every input of one factory carries the same callback instances, so an
  // unchanged input does not rebuild the list.
  let again =
    factory.CreateInput {
      props with
          Mode = MetroListSelectionMode.Multiple
    }

  Assert.True(obj.ReferenceEquals(input.OnClick, again.OnClick))

  Assert.True(
    obj.ReferenceEquals(input.OnSelectionChange, again.OnSelectionChange)
  )

  Assert.True(obj.ReferenceEquals(input.ConfigureRow, again.ConfigureRow))
  Assert.True(obj.ReferenceEquals(input.ConfigureRoot, again.ConfigureRoot))

  // Activation mode: no selection callback and an empty selection.
  let activate =
    factory.CreateInput {
      props with
          Mode = MetroListSelectionMode.Activate
    }

  Assert.Null(activate.OnSelectionChange)
  Assert.Equal(0, activate.SelectedIds.Length)
  Assert.False(activate.Multiple)
  Assert.NotNull(activate.OnClick)

[<Fact>]
let ``MetroListView mounts and runs the row style hook inside a build``() =
  let events = ResizeArray<MetroListEvent>()
  let factory = MetroListView.createFactory events.Add

  let props = {
    MetroListView.defaults items with
        SelectedIds = [| "photos" |]
        Height = ValueSome(Length.Pixels 220.0)
  }

  let input = factory.CreateInput props
  let root = CaptureCell.create(fun () -> factory.Mount("list", props))

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  Assert.NotNull(root.Captured)

  // The row style hook reads the Metro theme, so it runs inside a build.
  let mutable styledRows = 0

  root.DuringBuild(
    host,
    fun () ->
      let row = Container()

      for item in input.Items do
        input.ConfigureRow.Invoke(input, item, row)
        styledRows <- styledRows + 1

      input.ConfigureRoot.Invoke(input, Container())
  )

  Assert.Equal(3, styledRows)
