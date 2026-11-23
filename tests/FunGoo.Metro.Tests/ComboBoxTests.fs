module FunGoo.Metro.Tests.ComboBoxTests

open System
open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private options = [|
  {
    Id = "small"
    Label = "Small"
    Disabled = false
  }
  {
    Id = "medium"
    Label = ""
    Disabled = false
  }
  {
    Id = "large"
    Label = "Large"
    Disabled = true
  }
|]

/// Mounts `build` in a headless window and returns the built tree.
let private mountInWindow(build: unit -> Blob) : Blob =
  let root = CaptureCell(build)

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)
  root.Captured

[<Fact>]
let ``MetroComboBox maps props for a combo box and for an auto-suggest box``() =
  let combo = {
    MetroComboBox.defaults(options, "medium") with
        Placeholder = ValueSome "size"
        Width = ValueSome(Length.Pixels 160.0)
        AccessibilityName = ValueSome "Size"
  }

  let data = MetroComboBox.toData combo
  Assert.Equal(3, data.Items.Length)
  Assert.Equal("medium", data.Items[1].Id)

  // An empty label passes as null. The widget shows the identifier.
  Assert.Null(data.Items[1].Label)
  Assert.True(data.Items[2].Disabled)
  Assert.Equal("medium", data.SelectedId)
  Assert.False(data.Searchable)

  // A combo box without the search entry leaves the query to the widget.
  Assert.Null(data.Query)
  Assert.Equal("size", data.Placeholder)
  Assert.Equal(Nullable(Length.Pixels 160.0), data.Width)
  Assert.Equal("Size", data.AccessibilityName)
  Assert.Null(data.OnSelect)
  Assert.Null(data.OnQueryChange)
  Assert.Null(data.OnOpenChange)

  let suggest = MetroComboBox.toData(MetroComboBox.autoSuggest(options, "me"))

  Assert.True(suggest.Searchable)
  Assert.Equal("me", suggest.Query)
  Assert.Null(suggest.SelectedId)

  // A null query becomes an empty controlled query.
  let empty = MetroComboBox.toData(MetroComboBox.autoSuggest(options, null))
  Assert.Equal("", empty.Query)

[<Fact>]
let ``MetroComboBoxFactory routes callbacks and keeps one callback set``() =
  let events = ResizeArray<MetroComboBoxEvent>()
  let factory = MetroComboBoxFactory(events.Add)

  let comboProps = MetroComboBox.defaults(options, "small")

  let combo = factory.CreateInput comboProps
  Assert.NotNull(combo.OnSelect)
  Assert.NotNull(combo.OnOpenChange)
  Assert.Null(combo.OnQueryChange)

  combo.OnSelect.Invoke("large")
  combo.OnOpenChange.Invoke(true)

  assertListEqual(
    [ MetroComboBoxEvent.Selected "large"; MetroComboBoxEvent.OpenChanged true ],
    List.ofSeq events
  )

  // Every input of one factory carries the same callback instances, so an
  // unchanged input does not rebuild the combo box.
  let again =
    factory.CreateInput {
      comboProps with
          SelectedId = "medium"
    }

  Assert.True(obj.ReferenceEquals(combo.OnSelect, again.OnSelect))

  Assert.True(obj.ReferenceEquals(combo.OnOpenChange, again.OnOpenChange))

  // The mapped options are the same instance while the option array is the
  // same instance.
  Assert.True(obj.ReferenceEquals(combo.Items, again.Items))

  let changed =
    factory.CreateInput {
      comboProps with
          Options = [| options[0] |]
    }

  Assert.False(obj.ReferenceEquals(combo.Items, changed.Items))
  Assert.Equal(1, changed.Items.Length)

  // An auto-suggest box also reports query changes.
  events.Clear()
  let suggest = factory.CreateInput(MetroComboBox.autoSuggest(options, "m"))
  Assert.NotNull(suggest.OnQueryChange)
  suggest.OnQueryChange.Invoke("ma")
  suggest.OnSelect.Invoke("small")

  assertListEqual(
    [
      MetroComboBoxEvent.QueryChanged "ma"
      MetroComboBoxEvent.Selected "small"
    ],
    List.ofSeq events
  )

  // Both variants mount in a window.
  Assert.NotNull(mountInWindow(fun () -> factory.Mount("combo", comboProps)))

  Assert.NotNull(
    mountInWindow(fun () ->
      factory.Mount("suggest", MetroComboBox.autoSuggest(options, "m")))
  )
