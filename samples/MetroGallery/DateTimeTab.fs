module MetroGallery.DateTimeTab

open System
open System.Globalization
open Goo
open Goo.Widgets.Inputs
open FunGoo.Children
open FunGoo.Metro

[<AbstractClass>]
type DateTimeTabCell() =
  inherit Cell()

  abstract DateRoller: MetroDatePickerRollerCell voption
  abstract TimeRoller: MetroTimePickerRollerCell voption
  abstract DatePicker: MetroDatePickerCell voption
  abstract TimePicker: MetroTimePickerCell voption
  abstract CalendarInput: CalendarInput
  abstract CalendarDatePickerInput: DatePickerInput
  abstract RollerDate: DateOnly
  abstract RollerTime: TimeOnly
  abstract PickedDate: DateOnly voption
  abstract PickedTime: TimeOnly voption
  abstract CalendarDate: DateOnly voption
  abstract DropdownDate: DateOnly voption
  abstract LastAction: string
  abstract Builds: int

type DateTimeState = {
  mutable RollerDate: DateOnly
  mutable RollerTime: TimeOnly
  mutable PickedDate: DateOnly voption
  mutable DateOpen: bool
  mutable PickedTime: TimeOnly voption
  mutable TimeOpen: bool
  mutable UseTwelveHour: bool
  mutable CalendarDate: DateOnly voption
  mutable CalendarMonth: DateOnly voption
  mutable DropdownDate: DateOnly voption
  mutable LastCalendarInput: CalendarInput
  mutable LastDropdownInput: DatePickerInput
  mutable LastAction: string
  mutable Builds: int
}

type DateTimeFactories = {
  DateRoller: unit -> MetroDatePickerRollerCell
  TimeRoller: unit -> MetroTimePickerRollerCell
  DatePicker: unit -> MetroDatePickerCell
  TimePicker: unit -> MetroTimePickerCell
  Calendar: IMetroCalendarFactory
  CalendarDatePicker: IMetroCalendarDatePickerFactory
}

let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 12.0
  )
    .Children(MetroText.subheader title, content)

let view(state: DateTimeState, factories: DateTimeFactories) : Blob =
  let clockFormat =
    if state.UseTwelveHour then
      MetroClockFormat.TwelveHour
    else
      MetroClockFormat.TwentyFourHour

  let dateRollerInput = {
    MetroDatePickerRoller.defaults state.RollerDate with
        Label = ValueSome "INLINE DATE ROLLER"
  }

  let timeRollerInput = {
    MetroTimePickerRoller.defaults state.RollerTime with
        Format = clockFormat
        MinuteIncrement = 5
        Label = ValueSome "INLINE TIME ROLLER (5-MIN STEP)"
  }

  let datePickerInput = {
    MetroDatePicker.defaults state.PickedDate with
        Open = state.DateOpen
        FallbackDate = state.RollerDate
        Label = ValueSome "ROLLER DATE PICKER"
  }

  let timePickerInput = {
    MetroTimePicker.defaults state.PickedTime with
        Open = state.TimeOpen
        FallbackTime = state.RollerTime
        Format = clockFormat
        MinuteIncrement = 5
        Label = ValueSome "ROLLER TIME PICKER"
  }

  let calendarProps = {
    MetroCalendar.defaults state.CalendarDate with
        Month = state.CalendarMonth
        Today = ValueSome(DateOnly(2026, 11, 24))
        Width = ValueSome(Length.Pixels 288.0)
        AccessibilityName = ValueSome "Project schedule calendar"
  }

  let dropdownProps = {
    MetroCalendarDatePicker.defaults state.DropdownDate with
        Today = ValueSome(DateOnly(2026, 11, 24))
        Placeholder =
          ValueSome(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern)
        Width = ValueSome(Length.Percent 100.0)
        AccessibilityName = ValueSome "Calendar date picker"
  }

  state.LastCalendarInput <- factories.Calendar.CreateInput calendarProps

  state.LastDropdownInput <-
    factories.CalendarDatePicker.CreateInput dropdownProps

  let rollersCol =
    column(
      "looping rollers",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 18.0)
        .Children(
          MetroDatePickerRoller.mount(
            "gallery-date-roller",
            dateRollerInput,
            factories.DateRoller
          ),
          MetroTimePickerRoller.mount(
            "gallery-time-roller",
            timeRollerInput,
            factories.TimeRoller
          )
        )
    )

  let fieldsCol =
    column(
      "popup pickers",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 14.0)
        .Children(
          MetroDatePicker.mount(
            "gallery-date-picker",
            datePickerInput,
            factories.DatePicker
          ),
          MetroTimePicker.mount(
            "gallery-time-picker",
            timePickerInput,
            factories.TimePicker
          ),
          Container(
            FlexDirection = FlexDirection.Column,
            Gap = Length.Pixels 6.0
          )
            .Children(
              MetroText.caption "CALENDAR DATE PICKER",
              factories.CalendarDatePicker.Mount(
                "gallery-calendar-date-picker",
                dropdownProps
              )
            ),
          MetroToggleSwitch.build(
            {
              MetroToggleSwitch.defaults state.UseTwelveHour with
                  Header = ValueSome "Clock format"
                  OnLabel = "12-hour (AM/PM)"
                  OffLabel = "24-hour"
            },
            fun enabled ->
              state.UseTwelveHour <- enabled

              state.LastAction <-
                (if enabled then "12-hour format" else "24-hour format")
          )
        )
    )

  let calendarCol =
    column(
      "month calendar",
      factories.Calendar.Mount("gallery-calendar", calendarProps)
    )

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 16.0
  )
    .Children(
      MetroText.secondary
        "Looping rollers, modal date and time pickers, an editable calendar \
         date picker, and a month-grid calendar with sharp 0 px cells and a \
         2 px accent border on today.",
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        Gap = Length.Pixels 24.0
      )
        .Children(rollersCol, fieldsCol, calendarCol),
      MetroText.caption state.LastAction
    )

let create() : DateTimeTabCell =
  let state: DateTimeState = {
    RollerDate = DateOnly(2026, 11, 24)
    RollerTime = TimeOnly(14, 30)
    PickedDate = ValueSome(DateOnly(2026, 11, 24))
    DateOpen = false
    PickedTime = ValueSome(TimeOnly(14, 30))
    TimeOpen = false
    UseTwelveHour = false
    CalendarDate = ValueSome(DateOnly(2026, 11, 18))
    CalendarMonth = ValueSome(DateOnly(2026, 11, 1))
    DropdownDate = ValueSome(DateOnly(2026, 11, 24))
    LastCalendarInput = Unchecked.defaultof<CalendarInput>
    LastDropdownInput = Unchecked.defaultof<DatePickerInput>
    LastAction = "Ready"
    Builds = 0
  }

  let mutable dateRollerRef: MetroDatePickerRollerCell voption = ValueNone
  let mutable timeRollerRef: MetroTimePickerRollerCell voption = ValueNone
  let mutable datePickerRef: MetroDatePickerCell voption = ValueNone
  let mutable timePickerRef: MetroTimePickerCell voption = ValueNone

  let factories: DateTimeFactories = {
    DateRoller =
      fun () ->
        let cell =
          MetroDatePickerRoller.create(fun date ->
            state.RollerDate <- date

            state.LastAction <-
              $"Roller date: {MetroDatePicker.formatIso date}")

        dateRollerRef <- ValueSome cell
        cell
    TimeRoller =
      fun () ->
        let cell =
          MetroTimePickerRoller.create(fun time ->
            state.RollerTime <- time

            let fmt =
              if state.UseTwelveHour then
                MetroClockFormat.TwelveHour
              else
                MetroClockFormat.TwentyFourHour

            state.LastAction <-
              $"Roller time: {MetroTimePickerRoller.formatTime(time, fmt)}")

        timeRollerRef <- ValueSome cell
        cell
    DatePicker =
      fun () ->
        let cell =
          MetroDatePicker.create(function
            | MetroDatePickerEvent.DateCommitted date ->
              state.PickedDate <- ValueSome date

              state.LastAction <-
                $"Picked date: {MetroDatePicker.formatIso date}"
            | MetroDatePickerEvent.OpenChanged isOpen ->
              state.DateOpen <- isOpen)

        datePickerRef <- ValueSome cell
        cell
    TimePicker =
      fun () ->
        let cell =
          MetroTimePicker.create(function
            | MetroTimePickerEvent.TimeCommitted time ->
              state.PickedTime <- ValueSome time

              let fmt =
                if state.UseTwelveHour then
                  MetroClockFormat.TwelveHour
                else
                  MetroClockFormat.TwentyFourHour

              state.LastAction <-
                $"Picked time: {MetroTimePickerRoller.formatTime(time, fmt)}"
            | MetroTimePickerEvent.OpenChanged isOpen ->
              state.TimeOpen <- isOpen)

        timePickerRef <- ValueSome cell
        cell
    Calendar =
      MetroCalendar.createFactory(function
        | MetroCalendarEvent.DateSelected date ->
          state.CalendarDate <- ValueSome date

          state.LastAction <- $"Calendar date: {MetroDatePicker.formatIso date}"
        | MetroCalendarEvent.MonthChanged firstOfMonth ->
          state.CalendarMonth <- ValueSome firstOfMonth

          state.LastAction <-
            $"Calendar month: {MetroDatePicker.formatIso firstOfMonth}")
    CalendarDatePicker =
      MetroCalendarDatePicker.createFactory(function
        | MetroCalendarDatePickerEvent.ValueChanged maybeDate ->
          state.DropdownDate <- maybeDate

          let formatted =
            maybeDate
            |> ValueOption.map MetroDatePicker.formatIso
            |> ValueOption.defaultValue "cleared"

          state.LastAction <- $"Dropdown date: {formatted}"
        | MetroCalendarDatePickerEvent.OpenChanged _ -> ()
        | MetroCalendarDatePickerEvent.InvalidInput raw ->
          state.LastAction <- $"Invalid date: {raw}")
  }

  {
    new DateTimeTabCell() with
      override _.DateRoller = dateRollerRef
      override _.TimeRoller = timeRollerRef
      override _.DatePicker = datePickerRef
      override _.TimePicker = timePickerRef
      override _.CalendarInput = state.LastCalendarInput
      override _.CalendarDatePickerInput = state.LastDropdownInput
      override _.RollerDate = state.RollerDate
      override _.RollerTime = state.RollerTime
      override _.PickedDate = state.PickedDate
      override _.PickedTime = state.PickedTime
      override _.CalendarDate = state.CalendarDate
      override _.DropdownDate = state.DropdownDate
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, factories)
  }
