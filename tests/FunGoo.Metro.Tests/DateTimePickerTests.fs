module FunGoo.Metro.Tests.DateTimePickerTests

open System
open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let digits =
  [| 0..9 |]
  |> Array.map(fun d -> MetroLoopingSelector.item($"d-{d}", string d))

[<Fact>]
let ``MetroLoopingSelector wraps when Loop is true and clamps when Loop is false``
  ()
  =
  Assert.Equal(
    9,
    MetroLoopingSelector.resolveOffsetIndex {
      Current = 0
      Delta = -1
      Count = 10
      Loop = true
    }
  )

  Assert.Equal(
    0,
    MetroLoopingSelector.resolveOffsetIndex {
      Current = 9
      Delta = 1
      Count = 10
      Loop = true
    }
  )

  Assert.Equal(
    -1,
    MetroLoopingSelector.resolveOffsetIndex {
      Current = 0
      Delta = -1
      Count = 10
      Loop = false
    }
  )

  Assert.Equal(
    -1,
    MetroLoopingSelector.resolveOffsetIndex {
      Current = 9
      Delta = 1
      Count = 10
      Loop = false
    }
  )

  use host = createHost(320, 300)
  let mutable selected = "d-0"
  let mutable loopMode = true
  let mutable rollerRef: MetroLoopingSelectorCell voption = ValueNone

  let factory() =
    let cell = MetroLoopingSelector.create(fun id -> selected <- id)
    rollerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroLoopingSelector.mount(
        "roller",
        {
          MetroLoopingSelector.defaults(digits, selected) with
              Loop = loopMode
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let roller =
    match rollerRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroLoopingSelectorCell to mount."

  let buildsBefore = root.Builds
  roller.Step(-1)
  pumpFrames(host, 2)

  Assert.Equal("d-9", selected)
  Assert.Equal(9, roller.SelectedIndex)
  Assert.Equal(buildsBefore + 1, root.Builds)
  Assert.True(roller.TrackTranslate.Running)

  // Non-looping mode clamps at the upper end.
  loopMode <- false
  root.Rebuild()
  pumpFrames(host, 2)

  let buildsAtEnd = root.Builds
  roller.Step(1)
  pumpFrames(host, 2)
  Assert.Equal("d-9", selected)
  Assert.Equal(buildsAtEnd, root.Builds)

[<Fact>]
let ``MetroDatePickerRoller clamps month-end days and propagates changes via owner rule``
  ()
  =
  Assert.Equal(
    DateOnly(2026, 2, 28),
    MetroDatePickerRoller.clampDate(2026, 2, 31)
  )

  Assert.Equal(
    DateOnly(2024, 2, 29),
    MetroDatePickerRoller.clampDate(2024, 2, 31)
  )

  use host = createHost(480, 320)
  let mutable currentDate = DateOnly(2026, 1, 31)
  let mutable rollerRef: MetroDatePickerRollerCell voption = ValueNone

  let factory() =
    let cell = MetroDatePickerRoller.create(fun d -> currentDate <- d)
    rollerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroDatePickerRoller.mount(
        "date-roller",
        MetroDatePickerRoller.defaults currentDate,
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dateRoller =
    match rollerRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerRollerCell to mount."

  let monthRoller =
    match dateRoller.MonthRoller with
    | ValueSome m -> m
    | ValueNone -> failwith "Expected month roller to mount."

  // Stepping month from January 31 (+1) -> February 28 in non-leap year 2026,
  // rebuilding the outer host cell once with zero manual Rebuild().
  let buildsBefore = root.Builds
  monthRoller.Step(1)
  pumpFrames(host, 2)

  Assert.Equal(DateOnly(2026, 2, 28), currentDate)
  Assert.Equal(buildsBefore + 1, root.Builds)

[<Fact>]
let ``MetroTimePickerRoller supports 12-hour and 24-hour formatting and stepping``
  ()
  =
  Assert.Equal(
    "14:05",
    MetroTimePickerRoller.formatTime(
      TimeOnly(14, 5),
      MetroClockFormat.TwentyFourHour
    )
  )

  Assert.Equal(
    "12:00 AM",
    MetroTimePickerRoller.formatTime(
      TimeOnly(0, 0),
      MetroClockFormat.TwelveHour
    )
  )

  Assert.Equal(
    "2:30 PM",
    MetroTimePickerRoller.formatTime(
      TimeOnly(14, 30),
      MetroClockFormat.TwelveHour
    )
  )

  use host = createHost(480, 320)
  let mutable currentTime = TimeOnly(9, 30)
  let mutable rollerRef: MetroTimePickerRollerCell voption = ValueNone

  let factory() =
    let cell = MetroTimePickerRoller.create(fun t -> currentTime <- t)
    rollerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroTimePickerRoller.mount(
        "time-roller",
        {
          MetroTimePickerRoller.defaults currentTime with
              Format = MetroClockFormat.TwelveHour
              MinuteIncrement = 15
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let timeRoller =
    match rollerRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTimePickerRollerCell to mount."

  let periodRoller =
    match timeRoller.PeriodRoller with
    | ValueSome p -> p
    | ValueNone -> failwith "Expected AM/PM period roller in 12-hour mode."

  periodRoller.Step(1)
  pumpFrames(host, 2)
  Assert.Equal(TimeOnly(21, 30), currentTime)

[<Fact>]
let ``MetroDatePicker and MetroTimePicker stage drafts and commit or cancel cleanly``
  ()
  =
  use host = createHost(600, 480)
  let mutable dateVal: DateOnly voption = ValueSome(DateOnly(2026, 5, 10))
  let mutable dateOpen = false
  let mutable timeVal: TimeOnly voption = ValueSome(TimeOnly(8, 15))
  let mutable timeOpen = false
  let mutable dateRef: MetroDatePickerCell voption = ValueNone
  let mutable timeRef: MetroTimePickerCell voption = ValueNone

  let dateFactory() =
    let c =
      MetroDatePicker.create(function
        | MetroDatePickerEvent.DateCommitted d -> dateVal <- ValueSome d
        | MetroDatePickerEvent.OpenChanged o -> dateOpen <- o)

    dateRef <- ValueSome c
    c

  let timeFactory() =
    let c =
      MetroTimePicker.create(function
        | MetroTimePickerEvent.TimeCommitted t -> timeVal <- ValueSome t
        | MetroTimePickerEvent.OpenChanged o -> timeOpen <- o)

    timeRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      Container()
      |> fun c ->
        c.Add(
          MetroDatePicker.mount(
            "dp",
            {
              MetroDatePicker.defaults dateVal with
                  Open = dateOpen
            },
            dateFactory
          )
        )

        c.Add(
          MetroTimePicker.mount(
            "tp",
            {
              MetroTimePicker.defaults timeVal with
                  Open = timeOpen
            },
            timeFactory
          )
        )

        c :> Blob)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dp =
    match dateRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerCell."

  let tp =
    match timeRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTimePickerCell."

  dp.Handle(MetroDatePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(dp.IsOpen)
  Assert.Equal(DateOnly(2026, 5, 10), dp.DraftDate)

  dp.Handle(MetroDatePickerEvent.DateCommitted(DateOnly(2026, 12, 25)))
  pumpFrames(host, 2)
  Assert.False(dp.IsOpen)
  Assert.Equal(ValueSome(DateOnly(2026, 12, 25)), dateVal)

  tp.Handle(MetroTimePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(tp.IsOpen)

  tp.Handle(MetroTimePickerEvent.TimeCommitted(TimeOnly(16, 45)))
  pumpFrames(host, 2)
  Assert.False(tp.IsOpen)
  Assert.Equal(ValueSome(TimeOnly(16, 45)), timeVal)

[<Fact>]
let ``a date past MaxYear shows and commits the last valid year``() =
  // The roller Value is the last valid date of the year range.
  let rollerScenario() =
    use host = createHost(480, 320)
    let mutable currentDate = DateOnly(2100, 1, 1)
    let mutable rollerRef: MetroDatePickerRollerCell voption = ValueNone

    let factory() =
      let cell = MetroDatePickerRoller.create(fun d -> currentDate <- d)
      rollerRef <- ValueSome cell
      cell

    let rollerInput = {
      MetroDatePickerRoller.defaults(DateOnly(2100, 1, 1)) with
          MinYear = 1950
          MaxYear = 2050
    }

    let root =
      CaptureCell.create(fun () ->
        MetroDatePickerRoller.mount("date-roller", rollerInput, factory))

    let window = Window(Root = root)
    Metro.attach(window, darkConfig)
    window.Attach(host) |> ignore
    pumpFrames(host, 2)

    let dateRoller =
      match rollerRef with
      | ValueSome c -> c
      | ValueNone -> failwith "Expected MetroDatePickerRollerCell to mount."

    Assert.Equal(DateOnly(2050, 1, 1), dateRoller.Value)

  // The compact field shows the valid date on the trigger and Accept commits
  // the valid draft.
  let fieldScenario
    (
      initialValue: DateOnly voption,
      fallback: DateOnly,
      triggerAfterAccept: bool
    ) =
    use host = createHost(600, 480)
    let mutable dateVal = initialValue
    let mutable dateOpen = false
    let mutable committed: DateOnly voption = ValueNone
    let mutable dateRef: MetroDatePickerCell voption = ValueNone
    let mutable tree: Blob = null
    let mutable sheet: Blob = null

    let raiseToHost =
      Action<MetroDatePickerEvent>(fun ev ->
        match ev with
        | MetroDatePickerEvent.DateCommitted d ->
          committed <- ValueSome d
          dateVal <- ValueSome d
        | MetroDatePickerEvent.OpenChanged o -> dateOpen <- o)

    let dateFactory() =
      let c = MetroDatePicker.create raiseToHost.Invoke
      dateRef <- ValueSome c
      c

    let root =
      CaptureCell.create(fun () ->
        MetroDatePicker.mount(
          "dp",
          {
            MetroDatePicker.defaults dateVal with
                Open = dateOpen
                FallbackDate = fallback
                MinYear = 1950
                MaxYear = 2050
          },
          dateFactory
        ))

    let window = Window(Root = root)
    Metro.attach(window, darkConfig)
    window.Attach(host) |> ignore
    pumpFrames(host, 2)

    let dp =
      match dateRef with
      | ValueSome c -> c
      | ValueNone -> failwith "Expected MetroDatePickerCell."

    dp.Handle(MetroDatePickerEvent.OpenChanged true)
    pumpFrames(host, 2)
    Assert.True(dp.IsOpen)

    root.DuringBuild(
      host,
      fun () ->
        tree <- dp.View

        let struct (_, s) =
          DateTimeInternals.buildSheet(
            {
              PortalKey = "date-picker-portal"
              Title = "CHOOSE DATE"
              Roller = (Container() :> Blob)
            },
            (fun () ->
              dp.Raise(
                raiseToHost,
                MetroDatePickerEvent.DateCommitted dp.DraftDate
              )

              dp.Raise(raiseToHost, MetroDatePickerEvent.OpenChanged false)),
            (fun () ->
              dp.Raise(raiseToHost, MetroDatePickerEvent.OpenChanged false))
          )

        sheet <- s
    )

    if not triggerAfterAccept then
      let trigger = requireBlobByKey(tree, "date-picker-trigger")

      Assert.Contains("2050", (childAt(trigger, 0) :?> Text).Content)

    let accept =
      findKeyBinding(
        (requireBlobByKey(sheet, "date-picker-portal-sheet")).KeyBindings,
        Key.Enter
      )

    accept.OnPress.Invoke()
    pumpFrames(host, 2)

    Assert.Equal(ValueSome(DateOnly(2050, 1, 1)), committed)
    Assert.False(dp.IsOpen)

    if triggerAfterAccept then
      root.DuringBuild(host, fun () -> tree <- dp.View)

      let trigger = requireBlobByKey(tree, "date-picker-trigger")

      Assert.Contains("2050", (childAt(trigger, 0) :?> Text).Content)

  rollerScenario()
  fieldScenario(ValueSome(DateOnly(2100, 1, 1)), DateOnly(2026, 1, 1), false)
  fieldScenario(ValueNone, DateOnly(2100, 1, 1), true)

[<Fact>]
let ``a minute off the step shows and commits the snapped minute``() =
  // The roller Value is the snapped minute; the hour step stays on the step.
  let rollerScenario() =
    use host = createHost(480, 320)
    let mutable currentTime = TimeOnly(10, 7)
    let mutable rollerRef: MetroTimePickerRollerCell voption = ValueNone

    let factory() =
      let cell = MetroTimePickerRoller.create(fun t -> currentTime <- t)
      rollerRef <- ValueSome cell
      cell

    let rollerInput = {
      MetroTimePickerRoller.defaults(TimeOnly(10, 7)) with
          MinuteIncrement = 15
    }

    let root =
      CaptureCell.create(fun () ->
        MetroTimePickerRoller.mount("time-roller", rollerInput, factory))

    let window = Window(Root = root)
    Metro.attach(window, darkConfig)
    window.Attach(host) |> ignore
    pumpFrames(host, 2)

    let timeRoller =
      match rollerRef with
      | ValueSome c -> c
      | ValueNone -> failwith "Expected MetroTimePickerRollerCell to mount."

    Assert.Equal(TimeOnly(10, 0), timeRoller.Value)

    let hourRoller =
      match timeRoller.HourRoller with
      | ValueSome h -> h
      | ValueNone -> failwith "Expected hour roller to mount."

    hourRoller.Step(1)
    pumpFrames(host, 2)
    Assert.Equal(TimeOnly(11, 0), currentTime)

  // The compact field commits the snapped draft.
  let fieldScenario() =
    use host = createHost(600, 480)
    let mutable timeVal: TimeOnly voption = ValueSome(TimeOnly(10, 7))
    let mutable timeOpen = false
    let mutable committed: TimeOnly voption = ValueNone
    let mutable timeRef: MetroTimePickerCell voption = ValueNone
    let mutable tree: Blob = null
    let mutable sheet: Blob = null

    let raiseToHost =
      Action<MetroTimePickerEvent>(fun ev ->
        match ev with
        | MetroTimePickerEvent.TimeCommitted t ->
          committed <- ValueSome t
          timeVal <- ValueSome t
        | MetroTimePickerEvent.OpenChanged o -> timeOpen <- o)

    let timeFactory() =
      let c = MetroTimePicker.create raiseToHost.Invoke
      timeRef <- ValueSome c
      c

    let root =
      CaptureCell.create(fun () ->
        MetroTimePicker.mount(
          "tp",
          {
            MetroTimePicker.defaults timeVal with
                Open = timeOpen
                MinuteIncrement = 15
          },
          timeFactory
        ))

    let window = Window(Root = root)
    Metro.attach(window, darkConfig)
    window.Attach(host) |> ignore
    pumpFrames(host, 2)

    let tp =
      match timeRef with
      | ValueSome c -> c
      | ValueNone -> failwith "Expected MetroTimePickerCell."

    // The trigger shows the snapped minute of the committed value.
    root.DuringBuild(host, fun () -> tree <- tp.View)

    let trigger = requireBlobByKey(tree, "time-picker-trigger")

    Assert.Contains("10:00", (childAt(trigger, 0) :?> Text).Content)

    tp.Handle(MetroTimePickerEvent.OpenChanged true)
    pumpFrames(host, 2)
    Assert.True(tp.IsOpen)

    root.DuringBuild(
      host,
      fun () ->
        let struct (_, s) =
          DateTimeInternals.buildSheet(
            {
              PortalKey = "time-picker-portal"
              Title = "CHOOSE TIME"
              Roller = (Container() :> Blob)
            },
            (fun () ->
              tp.Raise(
                raiseToHost,
                MetroTimePickerEvent.TimeCommitted tp.DraftTime
              )

              tp.Raise(raiseToHost, MetroTimePickerEvent.OpenChanged false)),
            (fun () ->
              tp.Raise(raiseToHost, MetroTimePickerEvent.OpenChanged false))
          )

        sheet <- s
    )

    let accept =
      findKeyBinding(
        (requireBlobByKey(sheet, "time-picker-portal-sheet")).KeyBindings,
        Key.Enter
      )

    accept.OnPress.Invoke()
    pumpFrames(host, 2)

    Assert.Equal(ValueSome(TimeOnly(10, 0)), committed)
    Assert.False(tp.IsOpen)

  rollerScenario()
  fieldScenario()

[<Fact>]
let ``cancel closes the date and time fields without a commit``() =
  use host = createHost(600, 480)
  let mutable dateVal: DateOnly voption = ValueSome(DateOnly(2026, 5, 10))
  let mutable dateOpen = false
  let mutable timeVal: TimeOnly voption = ValueSome(TimeOnly(8, 15))
  let mutable timeOpen = false
  let mutable dateCommits = 0
  let mutable timeCommits = 0
  let mutable dateRef: MetroDatePickerCell voption = ValueNone
  let mutable timeRef: MetroTimePickerCell voption = ValueNone
  let mutable dateSheet: Blob = null
  let mutable timeSheet: Blob = null

  let raiseDate =
    Action<MetroDatePickerEvent>(fun ev ->
      match ev with
      | MetroDatePickerEvent.DateCommitted d ->
        dateCommits <- dateCommits + 1
        dateVal <- ValueSome d
      | MetroDatePickerEvent.OpenChanged o -> dateOpen <- o)

  let raiseTime =
    Action<MetroTimePickerEvent>(fun ev ->
      match ev with
      | MetroTimePickerEvent.TimeCommitted t ->
        timeCommits <- timeCommits + 1
        timeVal <- ValueSome t
      | MetroTimePickerEvent.OpenChanged o -> timeOpen <- o)

  let dateFactory() =
    let c = MetroDatePicker.create raiseDate.Invoke
    dateRef <- ValueSome c
    c

  let timeFactory() =
    let c = MetroTimePicker.create raiseTime.Invoke
    timeRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      Container()
      |> fun c ->
        c.Add(
          MetroDatePicker.mount(
            "dp",
            {
              MetroDatePicker.defaults dateVal with
                  Open = dateOpen
            },
            dateFactory
          )
        )

        c.Add(
          MetroTimePicker.mount(
            "tp",
            {
              MetroTimePicker.defaults timeVal with
                  Open = timeOpen
            },
            timeFactory
          )
        )

        c :> Blob)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dp =
    match dateRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerCell."

  let tp =
    match timeRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTimePickerCell."

  dp.Handle(MetroDatePickerEvent.OpenChanged true)
  tp.Handle(MetroTimePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(dp.IsOpen)
  Assert.True(tp.IsOpen)

  // Cancel closes through the Cancel button and the backdrop press; the
  // Escape key closes through Portal.OnDismiss. The sheets are reachable
  // through the public buildSheet seam of DateTimeInternals.
  root.DuringBuild(
    host,
    fun () ->
      let struct (_, ds) =
        DateTimeInternals.buildSheet(
          {
            PortalKey = "date-picker-portal"
            Title = "CHOOSE DATE"
            Roller = (Container() :> Blob)
          },
          (fun () ->
            dp.Raise(
              raiseDate,
              MetroDatePickerEvent.DateCommitted dp.DraftDate
            )

            dp.Raise(raiseDate, MetroDatePickerEvent.OpenChanged false)),
          (fun () ->
            dp.Raise(raiseDate, MetroDatePickerEvent.OpenChanged false))
        )

      dateSheet <- ds

      let struct (_, ts) =
        DateTimeInternals.buildSheet(
          {
            PortalKey = "time-picker-portal"
            Title = "CHOOSE TIME"
            Roller = (Container() :> Blob)
          },
          (fun () ->
            tp.Raise(
              raiseTime,
              MetroTimePickerEvent.TimeCommitted tp.DraftTime
            )

            tp.Raise(raiseTime, MetroTimePickerEvent.OpenChanged false)),
          (fun () ->
            tp.Raise(raiseTime, MetroTimePickerEvent.OpenChanged false))
        )

      timeSheet <- ts
  )

  let dateCancel = requireBlobByKey(dateSheet, "date-picker-portal-cancel")
  (dateCancel :?> Button).OnClick.Invoke()
  pumpFrames(host, 2)
  Assert.False(dp.IsOpen)
  Assert.Equal(0, dateCommits)

  let timeCancel = requireBlobByKey(timeSheet, "time-picker-portal-cancel")
  (timeCancel :?> Button).OnClick.Invoke()
  pumpFrames(host, 2)
  Assert.False(tp.IsOpen)
  Assert.Equal(0, timeCommits)

[<Fact>]
let ``a press on the dimmed area cancels the modal sheet``() =
  use host = createHost(600, 480)
  let mutable dateVal: DateOnly voption = ValueSome(DateOnly(2026, 5, 10))
  let mutable dateOpen = false
  let mutable dateCommits = 0
  let mutable dateRef: MetroDatePickerCell voption = ValueNone
  let mutable backdrop: Blob = null

  let raiseDate =
    Action<MetroDatePickerEvent>(fun ev ->
      match ev with
      | MetroDatePickerEvent.DateCommitted d ->
        dateCommits <- dateCommits + 1
        dateVal <- ValueSome d
      | MetroDatePickerEvent.OpenChanged o -> dateOpen <- o)

  let dateFactory() =
    let c = MetroDatePicker.create raiseDate.Invoke
    dateRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroDatePicker.mount(
        "dp",
        {
          MetroDatePicker.defaults dateVal with
              Open = dateOpen
        },
        dateFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dp =
    match dateRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerCell."

  dp.Handle(MetroDatePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(dp.IsOpen)

  root.DuringBuild(
    host,
    fun () ->
      let struct (b, _) =
        DateTimeInternals.buildSheet(
          {
            PortalKey = "date-picker-portal"
            Title = "CHOOSE DATE"
            Roller = (Container() :> Blob)
          },
          (fun () ->
            dp.Raise(
              raiseDate,
              MetroDatePickerEvent.DateCommitted dp.DraftDate
            )

            dp.Raise(raiseDate, MetroDatePickerEvent.OpenChanged false)),
          (fun () ->
            dp.Raise(raiseDate, MetroDatePickerEvent.OpenChanged false))
        )

      backdrop <- b
  )

  Assert.NotNull(backdrop.OnPointerDown)
  backdrop.OnPointerDown.Invoke(PointerEvent(IsPrimary = true))
  pumpFrames(host, 2)
  Assert.False(dp.IsOpen)
  Assert.Equal(0, dateCommits)

[<Fact>]
let ``the sheet is not inside the dimmed backdrop``() =
  use host = createHost(600, 480)
  let mutable backdrop: Blob = null
  let mutable sheet: Blob = null

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      let struct (b, s) =
        DateTimeInternals.buildSheet(
          {
            PortalKey = "date-picker-portal"
            Title = "CHOOSE DATE"
            Roller = (Container() :> Blob)
          },
          ignore,
          ignore
        )

      backdrop <- b
      sheet <- s
  )

  Assert.Equal(0, childCount backdrop)
  Assert.NotNull(backdrop.OnPointerDown)
  Assert.True(childCount sheet > 0)

[<Fact>]
let ``the sheet hit-tests its own box so gaps do not reach the backdrop``() =
  use host = createHost(600, 480)
  let mutable sheet: Blob = null

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      let struct (_, s) =
        DateTimeInternals.buildSheet(
          {
            PortalKey = "date-picker-portal"
            Title = "CHOOSE DATE"
            Roller = (Container() :> Blob)
          },
          ignore,
          ignore
        )

      sheet <- s
  )

  Assert.True((sheet :?> Container).HitTestSelf)

[<Fact>]
let ``Accept in the sheet commits the date and closes the field``() =
  use host = createHost(600, 480)
  let mutable dateVal: DateOnly voption = ValueSome(DateOnly(2026, 5, 10))
  let mutable dateOpen = false
  let mutable dateCommits = 0
  let mutable falseOpens = 0
  let mutable dateRef: MetroDatePickerCell voption = ValueNone
  let mutable sheet: Blob = null

  let raiseDate =
    Action<MetroDatePickerEvent>(fun ev ->
      match ev with
      | MetroDatePickerEvent.DateCommitted d ->
        dateCommits <- dateCommits + 1
        dateVal <- ValueSome d
      | MetroDatePickerEvent.OpenChanged o ->
        dateOpen <- o

        if not o then
          falseOpens <- falseOpens + 1)

  let dateFactory() =
    let c = MetroDatePicker.create raiseDate.Invoke
    dateRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroDatePicker.mount(
        "dp",
        {
          MetroDatePicker.defaults dateVal with
              Open = dateOpen
        },
        dateFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dp =
    match dateRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerCell."

  dp.Handle(MetroDatePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(dp.IsOpen)

  root.DuringBuild(
    host,
    fun () ->
      let struct (_, s) =
        DateTimeInternals.buildSheet(
          {
            PortalKey = "date-picker-portal"
            Title = "CHOOSE DATE"
            Roller = (Container() :> Blob)
          },
          (fun () ->
            dp.Raise(
              raiseDate,
              MetroDatePickerEvent.DateCommitted dp.DraftDate
            )

            dp.Raise(raiseDate, MetroDatePickerEvent.OpenChanged false)),
          (fun () ->
            dp.Raise(raiseDate, MetroDatePickerEvent.OpenChanged false))
        )

      sheet <- s
  )

  let accept = requireBlobByKey(sheet, "date-picker-portal-accept")
  (accept :?> Button).OnClick.Invoke()
  pumpFrames(host, 2)

  Assert.Equal(1, dateCommits)
  Assert.Equal(1, falseOpens)
  Assert.Equal(ValueSome(DateOnly(2026, 5, 10)), dateVal)
  Assert.False(dp.IsOpen)

[<Fact>]
let ``Handle commits a valid date and a snapped time``() =
  use host = createHost(600, 480)
  let mutable dateVal: DateOnly voption = ValueNone
  let mutable dateOpen = false
  let mutable timeVal: TimeOnly voption = ValueNone
  let mutable timeOpen = false
  let mutable dateRef: MetroDatePickerCell voption = ValueNone
  let mutable timeRef: MetroTimePickerCell voption = ValueNone

  let dateFactory() =
    let c =
      MetroDatePicker.create(function
        | MetroDatePickerEvent.DateCommitted d -> dateVal <- ValueSome d
        | MetroDatePickerEvent.OpenChanged o -> dateOpen <- o)

    dateRef <- ValueSome c
    c

  let timeFactory() =
    let c =
      MetroTimePicker.create(function
        | MetroTimePickerEvent.TimeCommitted t -> timeVal <- ValueSome t
        | MetroTimePickerEvent.OpenChanged o -> timeOpen <- o)

    timeRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      Container()
      |> fun c ->
        c.Add(
          MetroDatePicker.mount(
            "dp",
            {
              MetroDatePicker.defaults dateVal with
                  MaxYear = 2050
            },
            dateFactory
          )
        )

        c.Add(
          MetroTimePicker.mount(
            "tp",
            {
              MetroTimePicker.defaults timeVal with
                  MinuteIncrement = 15
            },
            timeFactory
          )
        )

        c :> Blob)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dp =
    match dateRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerCell."

  let tp =
    match timeRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTimePickerCell."

  dp.Handle(MetroDatePickerEvent.DateCommitted(DateOnly(2100, 1, 1)))
  pumpFrames(host, 2)
  Assert.Equal(ValueSome(DateOnly(2050, 1, 1)), dateVal)

  tp.Handle(MetroTimePickerEvent.TimeCommitted(TimeOnly(10, 7)))
  pumpFrames(host, 2)
  Assert.Equal(ValueSome(TimeOnly(10, 0)), timeVal)

[<Fact>]
let ``PeriodRoller is empty in the 24-hour format``() =
  use host = createHost(480, 320)
  let mutable currentTime = TimeOnly(9, 30)
  let mutable clockFormat = MetroClockFormat.TwelveHour
  let mutable rollerRef: MetroTimePickerRollerCell voption = ValueNone

  let factory() =
    let cell = MetroTimePickerRoller.create(fun t -> currentTime <- t)
    rollerRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroTimePickerRoller.mount(
        "time-roller",
        {
          MetroTimePickerRoller.defaults currentTime with
              Format = clockFormat
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let timeRoller =
    match rollerRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTimePickerRollerCell to mount."

  Assert.True(timeRoller.PeriodRoller.IsSome)

  clockFormat <- MetroClockFormat.TwentyFourHour
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(timeRoller.PeriodRoller.IsNone)

// Measured warm buildSheet: Release 11288 B, Debug 11456 B.
let sheetAllocCeiling = 23552L

[<Fact>]
let ``a warm build of the modal sheet allocates below the ceiling``() =
  use host = createHost(600, 480)
  let mutable allocated = 0L

  let args = {
    PortalKey = "date-picker-portal"
    Title = "CHOOSE DATE"
    Roller = (Container() :> Blob)
  }

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      // Warm call first, then measure the second call with the same input.
      let struct (_, warm) = DateTimeInternals.buildSheet(args, ignore, ignore)
      ignore warm

      let before = GC.GetAllocatedBytesForCurrentThread()

      let struct (_, sheet) = DateTimeInternals.buildSheet(args, ignore, ignore)

      allocated <- GC.GetAllocatedBytesForCurrentThread() - before
      ignore sheet
  )

  Assert.True(
    allocated <= sheetAllocCeiling,
    $"Expected <= {sheetAllocCeiling} B, allocated {allocated} B."
  )

[<Fact>]
let ``an open date draft follows a change of year bounds``() =
  use host = createHost(600, 480)
  let mutable dateVal: DateOnly voption = ValueSome(DateOnly(2026, 5, 10))
  let mutable dateOpen = false
  let mutable maxYear = 2030
  let mutable dateRef: MetroDatePickerCell voption = ValueNone

  let dateFactory() =
    let c =
      MetroDatePicker.create(function
        | MetroDatePickerEvent.DateCommitted d -> dateVal <- ValueSome d
        | MetroDatePickerEvent.OpenChanged o -> dateOpen <- o)

    dateRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroDatePicker.mount(
        "dp",
        {
          MetroDatePicker.defaults dateVal with
              Open = dateOpen
              MaxYear = maxYear
        },
        dateFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dp =
    match dateRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroDatePickerCell."

  dp.Handle(MetroDatePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(dp.IsOpen)
  Assert.Equal(DateOnly(2026, 5, 10), dp.DraftDate)

  maxYear <- 2020
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(dp.IsOpen)
  Assert.Equal(DateOnly(2020, 5, 10), dp.DraftDate)

  dp.Handle(MetroDatePickerEvent.DateCommitted dp.DraftDate)
  pumpFrames(host, 2)
  Assert.Equal(ValueSome(DateOnly(2020, 5, 10)), dateVal)

[<Fact>]
let ``an open time draft follows a change of minute step``() =
  use host = createHost(600, 480)
  let mutable timeVal: TimeOnly voption = ValueSome(TimeOnly(10, 7))
  let mutable timeOpen = false
  let mutable step = 1
  let mutable timeRef: MetroTimePickerCell voption = ValueNone

  let timeFactory() =
    let c =
      MetroTimePicker.create(function
        | MetroTimePickerEvent.TimeCommitted t -> timeVal <- ValueSome t
        | MetroTimePickerEvent.OpenChanged o -> timeOpen <- o)

    timeRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTimePicker.mount(
        "tp",
        {
          MetroTimePicker.defaults timeVal with
              Open = timeOpen
              MinuteIncrement = step
        },
        timeFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tp =
    match timeRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTimePickerCell."

  tp.Handle(MetroTimePickerEvent.OpenChanged true)
  pumpFrames(host, 2)
  Assert.True(tp.IsOpen)
  Assert.Equal(TimeOnly(10, 7), tp.DraftTime)

  step <- 15
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(tp.IsOpen)
  Assert.Equal(TimeOnly(10, 0), tp.DraftTime)

  tp.Handle(MetroTimePickerEvent.TimeCommitted tp.DraftTime)
  pumpFrames(host, 2)
  Assert.Equal(ValueSome(TimeOnly(10, 0)), timeVal)
