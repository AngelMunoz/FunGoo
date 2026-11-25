module FunGoo.Metro.Tests.CalendarTests

open System
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

[<Fact>]
let ``MetroCalendar toData maps bounds and factory caches delegates with zero churn``
  ()
  =
  let props = {
    MetroCalendar.defaults(ValueSome(DateOnly(2026, 11, 24))) with
        Month = ValueSome(DateOnly(2026, 11, 1))
        Minimum = ValueSome(DateOnly(2026, 1, 1))
        Maximum = ValueSome(DateOnly(2026, 12, 31))
        Today = ValueSome(DateOnly(2026, 11, 24))
        FirstDayOfWeek = ValueSome DayOfWeek.Monday
        DayHeight = 40.0
  }

  let data = MetroCalendar.toData props
  Assert.Equal(Nullable(DateOnly(2026, 11, 24)), data.Value)
  Assert.Equal(Nullable(DateOnly(2026, 11, 1)), data.Month)
  Assert.Equal(Nullable DayOfWeek.Monday, data.FirstDayOfWeek)
  Assert.Equal(Nullable 40.0, data.DayHeight)

  let mutable selected: DateOnly voption = ValueNone
  let mutable visibleMonth: DateOnly voption = ValueNone

  let factory =
    MetroCalendar.createFactory(function
      | MetroCalendarEvent.DateSelected d -> selected <- ValueSome d
      | MetroCalendarEvent.MonthChanged m -> visibleMonth <- ValueSome m)

  let input1 = factory.CreateInput props
  let input2 = factory.CreateInput props

  Assert.True(obj.ReferenceEquals(input1.OnChange, input2.OnChange))
  Assert.True(obj.ReferenceEquals(input1.OnMonthChange, input2.OnMonthChange))
  Assert.True(obj.ReferenceEquals(input1.ConfigureDay, input2.ConfigureDay))

  for _ = 1 to 10 do
    factory.CreateInput props |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
  let _ = factory.CreateInput props
  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes
  Assert.Equal(0L, allocated)

  input1.OnChange.Invoke(DateOnly(2026, 11, 28))
  input1.OnMonthChange.Invoke(DateOnly(2026, 12, 1))
  Assert.Equal(ValueSome(DateOnly(2026, 11, 28)), selected)
  Assert.Equal(ValueSome(DateOnly(2026, 12, 1)), visibleMonth)

[<Fact>]
let ``MetroCalendarDatePicker caches delegates, handles clear and invalid events, and mounts``
  ()
  =
  let mutable picked: DateOnly voption = ValueSome(DateOnly(2026, 11, 24))
  let mutable isOpen = false
  let mutable invalidRaw = ""

  let calFactory = MetroCalendar.createFactory ignore

  let dpFactory =
    MetroCalendarDatePicker.createFactory(function
      | MetroCalendarDatePickerEvent.ValueChanged d -> picked <- d
      | MetroCalendarDatePickerEvent.OpenChanged o -> isOpen <- o
      | MetroCalendarDatePickerEvent.InvalidInput raw -> invalidRaw <- raw)

  let props = {
    MetroCalendarDatePicker.defaults picked with
        Open = ValueSome isOpen
        Placeholder = ValueSome "YYYY-MM-DD"
        Today = ValueSome(DateOnly(2026, 11, 24))
  }

  let in1 = dpFactory.CreateInput props
  let in2 = dpFactory.CreateInput props

  Assert.True(obj.ReferenceEquals(in1.OnChange, in2.OnChange))
  Assert.True(obj.ReferenceEquals(in1.OnOpenChange, in2.OnOpenChange))
  Assert.True(obj.ReferenceEquals(in1.CreateCalendar, in2.CreateCalendar))
  Assert.True(obj.ReferenceEquals(in1.ConfigurePopup, in2.ConfigurePopup))

  for _ = 1 to 10 do
    dpFactory.CreateInput props |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
  let _ = dpFactory.CreateInput props
  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes
  Assert.Equal(0L, allocated)

  in1.OnChange.Invoke(Nullable())
  Assert.True(picked.IsNone)

  in1.OnChange.Invoke(Nullable(DateOnly(2027, 1, 15)))
  Assert.Equal(ValueSome(DateOnly(2027, 1, 15)), picked)

  in1.OnOpenChange.Invoke(true)
  Assert.True(isOpen)

  in1.OnInvalid.Invoke("not-a-date")
  Assert.Equal("not-a-date", invalidRaw)

  // Verify both controls mount cleanly in a headless window under Metro theme.
  use host = createHost(800, 500)

  let root =
    CaptureCell.create(fun () ->
      Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 16.0)
        .Children(
          calFactory.Mount(
            "cal",
            MetroCalendar.defaults(ValueSome(DateOnly(2026, 11, 24)))
          ),
          dpFactory.Mount("cal-dp", props)
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)
  Assert.True(root.Builds >= 1)

[<Fact>]
let ``the calendar date picker builds its open popup``() =
  let dpFactory =
    MetroCalendarDatePicker.createFactory(function
      | MetroCalendarDatePickerEvent.ValueChanged _ -> ()
      | MetroCalendarDatePickerEvent.OpenChanged _ -> ()
      | MetroCalendarDatePickerEvent.InvalidInput _ -> ())

  let props = {
    MetroCalendarDatePicker.defaults(ValueSome(DateOnly(2026, 11, 24))) with
        Open = ValueSome true
        Today = ValueSome(DateOnly(2026, 11, 24))
  }

  // The mounted cell reports the controlled open state through its input.
  let input = dpFactory.CreateInput props
  Assert.Equal(Nullable true, input.Open)

  // The open popup is the only path that builds the nested calendar and runs
  // ConfigurePopup and CreateCalendar. Mount open and pump frames: any build
  // exception fails the test.
  use host = createHost(800, 500)

  let root =
    CaptureCell.create(fun () ->
      Container().Children(dpFactory.Mount("cal-dp-open", props)))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 4)

  Assert.Equal(Nullable true, input.Open)
  Assert.True(root.Builds >= 1)
