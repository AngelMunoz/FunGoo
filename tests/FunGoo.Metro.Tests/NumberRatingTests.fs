module FunGoo.Metro.Tests.NumberRatingTests

open System
open System.Reflection
open Xunit
open Goo
open Goo.Widgets.Inputs
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private flags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

/// Reads the widget input of a mounted widget blob.
let private mountedInput<'input>(blob: Blob) : 'input =
  blob.GetType().GetField("<Input>k__BackingField", flags).GetValue(blob)
  :?> 'input

/// Mounts `build` in a headless window and returns the built tree.
let private mountInWindow(build: unit -> Blob) : Blob =
  let root = CaptureCell.create(build)

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)
  root.Captured

[<Fact>]
let ``MetroNumberBox maps props with spin buttons and mounts``() =
  let changes = ResizeArray<float>()
  let onChange = Action<float>(changes.Add)

  let props = {
    MetroNumberBox.defaults 12.5 with
        Minimum = ValueSome 0.0
        Maximum = ValueSome 100.0
        Step = ValueSome 0.5
        Label = ValueSome "Quantity"
        Width = ValueSome(Length.Pixels 160.0)
  }

  let input = MetroNumberBox.toInput(props, onChange)
  Assert.Equal(12.5, input.Value)
  Assert.Equal(Nullable 0.0, input.Minimum)
  Assert.Equal(Nullable 100.0, input.Maximum)
  Assert.Equal(Nullable 0.5, input.Step)
  Assert.Equal("Quantity", input.Label)
  Assert.Equal(Nullable true, input.ShowSpinButtons)
  Assert.Equal(Nullable(Length.Pixels 160.0), input.Width)
  Assert.False(input.Disabled)
  Assert.False(input.ReadOnly)

  input.OnChange.Invoke(13.0)
  assertListEqual([ 13.0 ], List.ofSeq changes)

  // Defaults leave the bounds and the step open, and always show the spin
  // buttons.
  let defaults = MetroNumberBox.toInput(MetroNumberBox.defaults 1.0, onChange)

  Assert.True(defaults.Minimum = Nullable())
  Assert.True(defaults.Maximum = Nullable())
  Assert.True(defaults.Step = Nullable())
  Assert.Null(defaults.Label)
  Assert.Equal(Nullable true, defaults.ShowSpinButtons)

  // Disabled and read-only pass through.
  let locked =
    MetroNumberBox.toInput(
      {
        props with
            Disabled = true
            ReadOnly = true
      },
      onChange
    )

  Assert.True(locked.Disabled)
  Assert.True(locked.ReadOnly)

  let numberCell = MetroNumberBox.create(changes.Add)
  let mutable numberSecond: Blob = null

  Assert.NotNull(
    mountInWindow(fun () ->
      numberSecond <- numberCell.Mount("number-again", props)
      numberCell.Mount("number", props))
  )

  // The cell owns one cached delegate: a second mount carries the same
  // callback instance, and it delivers value changes to the handler.
  let numberFirst =
    mountedInput<NumberInputInput>(numberCell.Mount("number", props))

  Assert.Same(
    (mountedInput<NumberInputInput> numberSecond).OnChange,
    numberFirst.OnChange
  )

  numberFirst.OnChange.Invoke(14.0)
  assertListEqual([ 13.0; 14.0 ], List.ofSeq changes)

[<Fact>]
let ``MetroRating maps props and mounts``() =
  let changes = ResizeArray<float>()
  let onChange = Action<float>(changes.Add)

  let props = {
    MetroRating.defaults 3.0 with
        Maximum = 4
        Step = 0.5
        AccessibilityName = ValueSome "Rate this app"
  }

  let input = MetroRating.toInput(props, onChange)
  Assert.Equal(3.0, input.Value)
  Assert.Equal(Nullable 4, input.Maximum)
  Assert.Equal(Nullable 0.5, input.Step)
  Assert.Equal("Rate this app", input.AccessibilityName)
  Assert.False(input.ReadOnly)
  Assert.False(input.Disabled)

  input.OnChange.Invoke(4.0)
  assertListEqual([ 4.0 ], List.ofSeq changes)

  // Defaults: five stars and a step of one.
  let defaults = MetroRating.toInput(MetroRating.defaults 0.0, onChange)
  Assert.Equal(0.0, defaults.Value)
  Assert.Equal(Nullable 5, defaults.Maximum)
  Assert.Equal(Nullable 1.0, defaults.Step)
  Assert.Null(defaults.AccessibilityName)

  // A highest rating below 1 and a step that is not positive use the widget
  // defaults.
  let invalid =
    MetroRating.toInput(
      {
        props with
            Maximum = 0
            Step = Double.NaN
      },
      onChange
    )

  Assert.True(invalid.Maximum = Nullable())
  Assert.True(invalid.Step = Nullable())

  let readOnly =
    MetroRating.toInput(
      {
        props with
            ReadOnly = true
            Disabled = true
      },
      onChange
    )

  Assert.True(readOnly.ReadOnly)
  Assert.True(readOnly.Disabled)

  let ratingCell = MetroRating.create(changes.Add)
  let mutable ratingSecond: Blob = null

  Assert.NotNull(
    mountInWindow(fun () ->
      ratingSecond <- ratingCell.Mount("rating-again", props)
      ratingCell.Mount("rating", props))
  )

  // The cell owns one cached delegate: a second mount carries the same
  // callback instance, and it delivers value changes to the handler.
  let ratingFirst =
    mountedInput<RatingControlInput>(ratingCell.Mount("rating", props))

  Assert.Same(
    (mountedInput<RatingControlInput> ratingSecond).OnChange,
    ratingFirst.OnChange
  )

  ratingFirst.OnChange.Invoke(5.0)
  assertListEqual([ 4.0; 5.0 ], List.ofSeq changes)
