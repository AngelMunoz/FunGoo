namespace FunGoo.Metro

open System
open System.Globalization
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct>]
type MetroClockFormat =
  | TwelveHour
  | TwentyFourHour

[<Struct; NoComparison>]
type MetroDatePickerRollerInput = {
  Value: DateOnly
  MinYear: int
  MaxYear: int
  Label: string voption
  Disabled: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroDatePickerRollerCell() =
  inherit Cell<MetroDatePickerRollerInput>()

  abstract Value: DateOnly

  abstract MonthRoller: MetroLoopingSelectorCell voption

  abstract DayRoller: MetroLoopingSelectorCell voption

  abstract YearRoller: MetroLoopingSelectorCell voption

  // Gives one date change to the host cell through InvokeInputCallback.
  member this.Raise(callback: Action<DateOnly>, value: DateOnly) =
    this.InvokeInputCallback(callback, value)

[<Struct; NoComparison>]
type MetroTimePickerRollerInput = {
  Value: TimeOnly
  Format: MetroClockFormat
  MinuteIncrement: int
  Label: string voption
  Disabled: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroTimePickerRollerCell() =
  inherit Cell<MetroTimePickerRollerInput>()

  abstract Value: TimeOnly

  abstract HourRoller: MetroLoopingSelectorCell voption

  abstract MinuteRoller: MetroLoopingSelectorCell voption

  abstract PeriodRoller: MetroLoopingSelectorCell voption

  // Gives one time change to the host cell through InvokeInputCallback.
  member this.Raise(callback: Action<TimeOnly>, value: TimeOnly) =
    this.InvokeInputCallback(callback, value)

[<Struct; NoComparison>]
type MetroDatePickerInput = {
  Value: DateOnly voption
  Open: bool
  FallbackDate: DateOnly
  MinYear: int
  MaxYear: int
  Label: string voption
  Placeholder: string voption
  Disabled: bool
  Width: Length voption
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroDatePickerEvent =
  | DateCommitted of date: DateOnly
  | OpenChanged of isOpen: bool

[<AbstractClass>]
type MetroDatePickerCell() =
  inherit Cell<MetroDatePickerInput>()

  abstract IsOpen: bool

  abstract DraftDate: DateOnly

  abstract View: Blob

  abstract Handle: event: MetroDatePickerEvent -> unit

  // Gives one event to the host cell through InvokeInputCallback.
  member this.Raise
    (callback: Action<MetroDatePickerEvent>, value: MetroDatePickerEvent)
    =
    this.InvokeInputCallback(callback, value)

[<Struct; NoComparison>]
type MetroTimePickerInput = {
  Value: TimeOnly voption
  Open: bool
  FallbackTime: TimeOnly
  Format: MetroClockFormat
  MinuteIncrement: int
  Label: string voption
  Placeholder: string voption
  Disabled: bool
  Width: Length voption
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroTimePickerEvent =
  | TimeCommitted of time: TimeOnly
  | OpenChanged of isOpen: bool

[<AbstractClass>]
type MetroTimePickerCell() =
  inherit Cell<MetroTimePickerInput>()

  abstract IsOpen: bool

  abstract DraftTime: TimeOnly

  abstract View: Blob

  abstract Handle: event: MetroTimePickerEvent -> unit

  // Gives one event to the host cell through InvokeInputCallback.
  member this.Raise
    (callback: Action<MetroTimePickerEvent>, value: MetroTimePickerEvent)
    =
    this.InvokeInputCallback(callback, value)

[<Struct; NoComparison>]
type PickerModalArgs = {
  PortalKey: string
  Title: string
  Roller: Blob
}

module DateTimeInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)

  let monthNames = [|
    "january"
    "february"
    "march"
    "april"
    "may"
    "june"
    "july"
    "august"
    "september"
    "october"
    "november"
    "december"
  |]

  let dayOfWeekNames = [|
    "sunday"
    "monday"
    "tuesday"
    "wednesday"
    "thursday"
    "friday"
    "saturday"
  |]

  let monthItems: MetroLoopingItem array =
    Array.init 12 (fun i ->
      let m = i + 1

      MetroLoopingSelector.itemWithCaption(
        string m,
        m.ToString("00", CultureInfo.InvariantCulture),
        monthNames[i]
      ))

  let hours24Items: MetroLoopingItem array =
    Array.init 24 (fun h ->
      MetroLoopingSelector.itemWithCaption(
        string h,
        h.ToString("00", CultureInfo.InvariantCulture),
        "hour"
      ))

  let hours12Items: MetroLoopingItem array =
    Array.init 12 (fun i ->
      let display = if i = 0 then 12 else i

      MetroLoopingSelector.itemWithCaption(
        string i,
        display.ToString("00", CultureInfo.InvariantCulture),
        "hour"
      ))

  let periodItems: MetroLoopingItem array = [|
    MetroLoopingSelector.itemWithCaption("am", "AM", "morning")
    MetroLoopingSelector.itemWithCaption("pm", "PM", "evening")
  |]

  let resolveYearBounds(minYear: int, maxYear: int) : struct (int * int) =
    let lo = Math.Clamp(minYear, 1, 9999)
    let hi = Math.Clamp(maxYear, lo, 9999)
    struct (lo, hi)

  let clampDate(year: int, month: int, day: int) : DateOnly =
    let y = Math.Clamp(year, 1, 9999)
    let m = Math.Clamp(month, 1, 12)
    let maxDay = DateTime.DaysInMonth(y, m)
    let d = Math.Clamp(day, 1, maxDay)
    DateOnly(y, m, d)

  let validDate(minYear: int, maxYear: int, date: DateOnly) : DateOnly =
    let struct (lo, hi) = resolveYearBounds(minYear, maxYear)
    clampDate(Math.Clamp(date.Year, lo, hi), date.Month, date.Day)

  let buildDaysFor(year: int, month: int) : MetroLoopingItem array =
    let y = Math.Clamp(year, 1, 9999)
    let m = Math.Clamp(month, 1, 12)
    let count = DateTime.DaysInMonth(y, m)

    Array.init count (fun idx ->
      let d = idx + 1
      let dow = int(DateOnly(y, m, d).DayOfWeek)

      MetroLoopingSelector.itemWithCaption(
        string d,
        d.ToString("00", CultureInfo.InvariantCulture),
        dayOfWeekNames[dow]
      ))

  let buildYearsFor(minYear: int, maxYear: int) : MetroLoopingItem array =
    let struct (lo, hi) = resolveYearBounds(minYear, maxYear)

    Array.init (hi - lo + 1) (fun idx ->
      let y = lo + idx
      let text = y.ToString("0000", CultureInfo.InvariantCulture)
      MetroLoopingSelector.itemWithCaption(text, text, "year"))

  let resolveIncrement(step: int) : int = Math.Clamp(step, 1, 30)

  let buildMinutesFor(step: int) : MetroLoopingItem array =
    let inc = resolveIncrement step
    let count = (59 / inc) + 1

    Array.init count (fun idx ->
      let m = idx * inc

      MetroLoopingSelector.itemWithCaption(
        string m,
        m.ToString("00", CultureInfo.InvariantCulture),
        "min"
      ))

  let snapMinute(minute: int, step: int) : int =
    let inc = resolveIncrement step
    let snapped = (minute / inc) * inc
    Math.Clamp(snapped, 0, 59)

  let formatIso(date: DateOnly) : string =
    date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

  let formatTime(value: TimeOnly, format: MetroClockFormat) : string =
    match format with
    | MetroClockFormat.TwentyFourHour -> $"{value.Hour:D2}:{value.Minute:D2}"
    | MetroClockFormat.TwelveHour ->
      let h12 =
        let rem = value.Hour % 12
        if rem = 0 then 12 else rem

      let period = if value.Hour >= 12 then "PM" else "AM"
      $"{h12}:{value.Minute:D2} {period}"

  [<Struct; NoComparison>]
  type TriggerArgs = {
    Key: string
    DisplayText: string
    IsPlaceholder: bool
    IconName: string
    IsOpen: bool
    Disabled: bool
    AccessibilityName: string
  }

  let buildTriggerButton(args: TriggerArgs, onToggle: unit -> unit) : Button =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent

    let borderColor =
      if theme.HighContrast then p.Foreground
      elif args.IsOpen then a.Base
      else p.BorderStrong

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let btn =
      Button(
        Key = args.Key,
        Width = Length.Percent 100.0,
        MinHeight = Length.Pixels 38.0,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Length.Pixels 8.0,
        Padding = Edges(Length.Pixels 6.0, Length.Pixels 12.0),
        BorderRadius = zeroRadius,
        BorderWidth = metroBorder,
        BorderColor = Edges<Color>(borderColor),
        BoxShadows = emptyShadows,
        BackgroundColor = p.BackgroundAlt,
        HoverStyle = Style(BackgroundColor = p.Hover),
        ActiveStyle = Style(BackgroundColor = p.Active),
        FocusStyle = focusStyle,
        DisabledStyle =
          Style(Opacity = (if theme.HighContrast then 1.0 else 0.4)),
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if args.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not args.Disabled,
        TabStop = not args.Disabled,
        Disabled = args.Disabled,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Button,
            Name = args.AccessibilityName,
            Expanded = Nullable args.IsOpen
          )
      )

    if not args.Disabled then
      let toggle = Action(onToggle)
      btn.OnClick <- toggle

      btn.KeyBindings <- [|
        KeyBinding(Key.Enter, KeyModifiers.None, toggle)
        KeyBinding(Key.Space, KeyModifiers.None, toggle)
      |]

    btn.Children(
      Text(
        Content = args.DisplayText,
        Color =
          (if args.IsPlaceholder then
             p.ForegroundSecondary
           else
             p.Foreground),
        FontFamily = theme.FontFamily,
        FontSize =
          Length.Pixels(
            MetroTypography.size MetroTextRole.Body * theme.TextScaleFactor
          ),
        FontWeight = 400.0,
        TextWrap = TextWrap.NoWrap,
        TextTrimming = TextTrimming.Ellipsis
      ),
      MaterialIcons.Create(args.IconName, 18.0, Nullable p.ForegroundSecondary)
    )

  let buildSheet
    (args: PickerModalArgs, onAccept: unit -> unit, onCancel: unit -> unit)
    : struct (Blob * Blob) =
    let theme = Metro.read()
    let p = theme.Palette
    let acceptAction = Action(onAccept)
    let portalKey = args.PortalKey
    let sheetTitle = MetroText.subheader args.Title
    sheetTitle.Key <- $"{portalKey}-title"

    let acceptButton = MetroButton.accent("Accept", onAccept)
    acceptButton.Key <- $"{portalKey}-accept"

    let cancelButton = MetroButton.standard("Cancel", onCancel)
    cancelButton.Key <- $"{portalKey}-cancel"

    let commandRow =
      Container(
        Key = $"{portalKey}-commands",
        FlexDirection = FlexDirection.Row,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Length.Pixels 12.0,
        Width = Length.Percent 100.0
      )
        .Children(acceptButton, cancelButton)

    let sheet =
      Container(
        Key = $"{portalKey}-sheet",
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 20.0,
        Padding = Edges(Length.Pixels 24.0),
        BorderRadius = zeroRadius,
        BorderWidth = metroBorder,
        BorderColor =
          Edges<Color>(
            if theme.HighContrast then p.Foreground else p.BorderStrong
          ),
        BackgroundColor = p.Background,
        HitTestSelf = true,
        KeyBindings = [|
          KeyBinding(Key.Enter, KeyModifiers.None, acceptAction)
        |],
        Accessibility =
          Accessibility(Role = AccessibilityRole.Dialog, Name = args.Title)
      )
        .Children(sheetTitle, args.Roller, commandRow)

    let backdrop =
      Container(
        Key = $"{portalKey}-backdrop",
        Position = PositionType.Absolute,
        Inset = Length.Pixels 0.0,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        BackgroundColor = Color.Black.WithAlpha(0.7)
      )

    // Press dispatch walks the pressed node and its parents. The sheet is a
    // sibling of the backdrop, so a press inside the sheet never reaches this
    // cancel handler; a press on the dimmed area lands on the backdrop itself.
    backdrop.OnPointerDown <- Action<PointerEvent>(fun _ -> onCancel())

    struct (backdrop :> Blob, sheet :> Blob)

  let buildPickerModal
    (args: PickerModalArgs, onAccept: unit -> unit, onCancel: unit -> unit)
    : Portal =
    let struct (backdrop, sheet) = buildSheet(args, onAccept, onCancel)

    let portal =
      Portal(
        Key = args.PortalKey,
        FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
        OnDismiss = Action<PopupDismissReason>(fun _ -> onCancel()),
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.FlexEnd,
        AlignItems = AlignItems.Center,
        ZIndex = 50
      )

    portal.Add(backdrop)
    portal.Add(sheet)
    portal

[<RequireQualifiedAccess>]
module MetroDatePickerRoller =

  let defaults(value: DateOnly) : MetroDatePickerRollerInput = {
    Value = value
    MinYear = 1950
    MaxYear = 2050
    Label = ValueNone
    Disabled = false
    AccessibilityName = ValueNone
  }

  let clampDate(year: int, month: int, day: int) : DateOnly =
    DateTimeInternals.clampDate(year, month, day)

  type DateRollerState = {
    mutable Input: MetroDatePickerRollerInput
    mutable CachedDays: (int * int * MetroLoopingItem array) voption
    mutable CachedYears: (int * int * MetroLoopingItem array) voption
    mutable MonthRef: MetroLoopingSelectorCell voption
    mutable DayRef: MetroLoopingSelectorCell voption
    mutable YearRef: MetroLoopingSelectorCell voption
  }

  type DateRollerFactories = {
    Month: unit -> MetroLoopingSelectorCell
    Day: unit -> MetroLoopingSelectorCell
    Year: unit -> MetroLoopingSelectorCell
  }

  let daysFor
    (state: DateRollerState, year: int, month: int)
    : MetroLoopingItem array =
    match state.CachedDays with
    | ValueSome(cy, cm, items) when cy = year && cm = month -> items
    | _ ->
      let items = DateTimeInternals.buildDaysFor(year, month)
      state.CachedDays <- ValueSome(year, month, items)
      items

  let yearsFor
    (state: DateRollerState, minYear: int, maxYear: int)
    : MetroLoopingItem array =
    match state.CachedYears with
    | ValueSome(lo, hi, items) when lo = minYear && hi = maxYear -> items
    | _ ->
      let items = DateTimeInternals.buildYearsFor(minYear, maxYear)
      state.CachedYears <- ValueSome(minYear, maxYear, items)
      items

  let emitChanged
    (state: DateRollerState, raise: Action<DateOnly>, next: DateOnly)
    : unit =
    let struct (lo, hi) =
      DateTimeInternals.resolveYearBounds(
        state.Input.MinYear,
        state.Input.MaxYear
      )

    let clamped = clampDate(Math.Clamp(next.Year, lo, hi), next.Month, next.Day)

    if clamped <> state.Input.Value then
      raise.Invoke(clamped)

  let build
    (
      state: DateRollerState,
      factories: DateRollerFactories,
      input: MetroDatePickerRollerInput
    ) : Blob =
    state.Input <- input
    let theme = Metro.read()
    let p = theme.Palette

    let struct (lo, hi) =
      DateTimeInternals.resolveYearBounds(input.MinYear, input.MaxYear)

    let cur =
      DateTimeInternals.validDate(input.MinYear, input.MaxYear, input.Value)

    let monthInput = {
      MetroLoopingSelector.defaults(
        DateTimeInternals.monthItems,
        string cur.Month
      ) with
          ItemWidth = 96.0
          Disabled = input.Disabled
          AccessibilityName = ValueSome "Month"
    }

    let dayInput = {
      MetroLoopingSelector.defaults(
        daysFor(state, cur.Year, cur.Month),
        string cur.Day
      ) with
          ItemWidth = 84.0
          Disabled = input.Disabled
          AccessibilityName = ValueSome "Day"
    }

    let yearStr = cur.Year.ToString("0000", CultureInfo.InvariantCulture)

    let yearInput = {
      MetroLoopingSelector.defaults(yearsFor(state, lo, hi), yearStr) with
          Loop = false
          ItemWidth = 88.0
          Disabled = input.Disabled
          AccessibilityName = ValueSome "Year"
    }

    let columns =
      Container(
        Key = "date-roller-columns",
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 8.0,
        FlexShrink = 0.0
      )
        .Children(
          MetroLoopingSelector.mount(
            "date-roller-month",
            monthInput,
            factories.Month
          ),
          MetroLoopingSelector.mount("date-roller-day", dayInput, factories.Day),
          MetroLoopingSelector.mount(
            "date-roller-year",
            yearInput,
            factories.Year
          )
        )

    let accName =
      input.AccessibilityName
      |> ValueOption.orElse input.Label
      |> ValueOption.defaultValue "Date picker roller"

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 8.0,
        FlexShrink = 0.0,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Group, Name = accName)
      )

    input.Label
    |> ValueOption.iter(fun lbl ->
      if not(String.IsNullOrWhiteSpace lbl) then
        root.Add(
          Text(
            Key = "date-roller-label",
            Content = lbl,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Caption
                * theme.TextScaleFactor
              ),
            FontWeight = 600.0
          )
        ))

    root.Add(columns)
    root

  let create(onChange: DateOnly -> unit) : MetroDatePickerRollerCell =
    let state: DateRollerState = {
      Input = defaults(DateOnly(2026, 1, 1))
      CachedDays = ValueNone
      CachedYears = ValueNone
      MonthRef = ValueNone
      DayRef = ValueNone
      YearRef = ValueNone
    }

    let mutable self: MetroDatePickerRollerCell voption = ValueNone
    let onChangeAction = Action<DateOnly>(onChange)

    let raise =
      Action<DateOnly>(fun date ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onChangeAction, date)))

    let factories: DateRollerFactories = {
      Month =
        fun () ->
          let c =
            MetroLoopingSelector.create(fun id ->
              let mutable m = 1

              if Int32.TryParse(id, &m) then
                let cur = state.Input.Value
                emitChanged(state, raise, clampDate(cur.Year, m, cur.Day)))

          state.MonthRef <- ValueSome c
          c
      Day =
        fun () ->
          let c =
            MetroLoopingSelector.create(fun id ->
              let mutable d = 1

              if Int32.TryParse(id, &d) then
                let cur = state.Input.Value
                emitChanged(state, raise, clampDate(cur.Year, cur.Month, d)))

          state.DayRef <- ValueSome c
          c
      Year =
        fun () ->
          let c =
            MetroLoopingSelector.create(fun id ->
              let mutable y = 2026

              if Int32.TryParse(id, &y) then
                let cur = state.Input.Value
                emitChanged(state, raise, clampDate(y, cur.Month, cur.Day)))

          state.YearRef <- ValueSome c
          c
    }

    let cell = {
      new MetroDatePickerRollerCell() with
        override _.Value =
          DateTimeInternals.validDate(
            state.Input.MinYear,
            state.Input.MaxYear,
            state.Input.Value
          )

        override _.MonthRoller = state.MonthRef
        override _.DayRoller = state.DayRef
        override _.YearRoller = state.YearRef
        override _.InputChanged(input) = state.Input <- input
        override _.Build(input) : Blob = build(state, factories, input)
    }

    self <- ValueSome cell
    cell

  let createFactory
    (onChange: DateOnly -> unit)
    : unit -> MetroDatePickerRollerCell =
    fun () -> create onChange

  let mount
    (
      key: string,
      input: MetroDatePickerRollerInput,
      factory: unit -> MetroDatePickerRollerCell
    ) : Blob =
    Cell.Mount<MetroDatePickerRollerInput, MetroDatePickerRollerCell>(
      key,
      input,
      factory
    )

[<RequireQualifiedAccess>]
module MetroTimePickerRoller =

  let defaults(value: TimeOnly) : MetroTimePickerRollerInput = {
    Value = value
    Format = MetroClockFormat.TwentyFourHour
    MinuteIncrement = 1
    Label = ValueNone
    Disabled = false
    AccessibilityName = ValueNone
  }

  let formatTime(value: TimeOnly, format: MetroClockFormat) : string =
    DateTimeInternals.formatTime(value, format)

  type TimeRollerState = {
    mutable Input: MetroTimePickerRollerInput
    mutable CachedMinutes: (int * MetroLoopingItem array) voption
    mutable HourRef: MetroLoopingSelectorCell voption
    mutable MinuteRef: MetroLoopingSelectorCell voption
    mutable PeriodRef: MetroLoopingSelectorCell voption
  }

  type TimeRollerFactories = {
    Hour: unit -> MetroLoopingSelectorCell
    Minute: unit -> MetroLoopingSelectorCell
    Period: unit -> MetroLoopingSelectorCell
  }

  let minutesFor(state: TimeRollerState, step: int) : MetroLoopingItem array =
    let inc = DateTimeInternals.resolveIncrement step

    match state.CachedMinutes with
    | ValueSome(cs, items) when cs = inc -> items
    | _ ->
      let items = DateTimeInternals.buildMinutesFor inc
      state.CachedMinutes <- ValueSome(inc, items)
      items

  let emitChanged
    (state: TimeRollerState, raise: Action<TimeOnly>, next: TimeOnly)
    : unit =
    if next <> state.Input.Value then
      raise.Invoke(next)

  let build
    (
      state: TimeRollerState,
      factories: TimeRollerFactories,
      input: MetroTimePickerRollerInput
    ) : Blob =
    state.Input <- input
    let theme = Metro.read()
    let p = theme.Palette
    let cur = input.Value

    let snappedMin =
      DateTimeInternals.snapMinute(cur.Minute, input.MinuteIncrement)

    let hourItems, hourId =
      match input.Format with
      | MetroClockFormat.TwentyFourHour ->
        DateTimeInternals.hours24Items, string cur.Hour
      | MetroClockFormat.TwelveHour ->
        DateTimeInternals.hours12Items, string(cur.Hour % 12)

    let hourInput = {
      MetroLoopingSelector.defaults(hourItems, hourId) with
          ItemWidth = 80.0
          Disabled = input.Disabled
          AccessibilityName = ValueSome "Hour"
    }

    let minuteInput = {
      MetroLoopingSelector.defaults(
        minutesFor(state, input.MinuteIncrement),
        string snappedMin
      ) with
          ItemWidth = 80.0
          Disabled = input.Disabled
          AccessibilityName = ValueSome "Minute"
    }

    let columns =
      Container(
        Key = "time-roller-columns",
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels 8.0,
        FlexShrink = 0.0
      )
        .Children(
          MetroLoopingSelector.mount(
            "time-roller-hour",
            hourInput,
            factories.Hour
          ),
          MetroLoopingSelector.mount(
            "time-roller-minute",
            minuteInput,
            factories.Minute
          )
        )

    if input.Format = MetroClockFormat.TwelveHour then
      let periodId = if cur.Hour >= 12 then "pm" else "am"

      let periodInput = {
        MetroLoopingSelector.defaults(DateTimeInternals.periodItems, periodId) with
            Loop = false
            ItemWidth = 80.0
            Disabled = input.Disabled
            AccessibilityName = ValueSome "AM or PM"
      }

      columns.Add(
        MetroLoopingSelector.mount(
          "time-roller-period",
          periodInput,
          factories.Period
        )
      )
    else
      state.PeriodRef <- ValueNone

    let accName =
      input.AccessibilityName
      |> ValueOption.orElse input.Label
      |> ValueOption.defaultValue "Time picker roller"

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 8.0,
        FlexShrink = 0.0,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Group, Name = accName)
      )

    input.Label
    |> ValueOption.iter(fun lbl ->
      if not(String.IsNullOrWhiteSpace lbl) then
        root.Add(
          Text(
            Key = "time-roller-label",
            Content = lbl,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Caption
                * theme.TextScaleFactor
              ),
            FontWeight = 600.0
          )
        ))

    root.Add(columns)
    root

  let create(onChange: TimeOnly -> unit) : MetroTimePickerRollerCell =
    let state: TimeRollerState = {
      Input = defaults(TimeOnly(9, 0))
      CachedMinutes = ValueNone
      HourRef = ValueNone
      MinuteRef = ValueNone
      PeriodRef = ValueNone
    }

    let mutable self: MetroTimePickerRollerCell voption = ValueNone
    let onChangeAction = Action<TimeOnly>(onChange)

    let raise =
      Action<TimeOnly>(fun time ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onChangeAction, time)))

    let factories: TimeRollerFactories = {
      Hour =
        fun () ->
          let c =
            MetroLoopingSelector.create(fun id ->
              let mutable h = 0

              if Int32.TryParse(id, &h) then
                let cur = state.Input.Value

                let nextHour =
                  match state.Input.Format with
                  | MetroClockFormat.TwentyFourHour -> Math.Clamp(h, 0, 23)
                  | MetroClockFormat.TwelveHour ->
                    let base12 = Math.Clamp(h, 0, 11)
                    if cur.Hour >= 12 then base12 + 12 else base12

                emitChanged(
                  state,
                  raise,
                  TimeOnly(
                    nextHour,
                    DateTimeInternals.snapMinute(
                      cur.Minute,
                      state.Input.MinuteIncrement
                    )
                  )
                ))

          state.HourRef <- ValueSome c
          c
      Minute =
        fun () ->
          let c =
            MetroLoopingSelector.create(fun id ->
              let mutable m = 0

              if Int32.TryParse(id, &m) then
                let cur = state.Input.Value

                emitChanged(
                  state,
                  raise,
                  TimeOnly(cur.Hour, Math.Clamp(m, 0, 59))
                ))

          state.MinuteRef <- ValueSome c
          c
      Period =
        fun () ->
          let c =
            MetroLoopingSelector.create(fun id ->
              let cur = state.Input.Value
              let base12 = cur.Hour % 12
              let nextHour = if id = "pm" then base12 + 12 else base12

              emitChanged(
                state,
                raise,
                TimeOnly(
                  nextHour,
                  DateTimeInternals.snapMinute(
                    cur.Minute,
                    state.Input.MinuteIncrement
                  )
                )
              ))

          state.PeriodRef <- ValueSome c
          c
    }

    let cell = {
      new MetroTimePickerRollerCell() with
        override _.Value =
          let cur = state.Input.Value

          TimeOnly(
            cur.Hour,
            DateTimeInternals.snapMinute(
              cur.Minute,
              state.Input.MinuteIncrement
            )
          )

        override _.HourRoller = state.HourRef
        override _.MinuteRoller = state.MinuteRef
        override _.PeriodRoller = state.PeriodRef
        override _.InputChanged(input) = state.Input <- input
        override _.Build(input) : Blob = build(state, factories, input)
    }

    self <- ValueSome cell
    cell

  let createFactory
    (onChange: TimeOnly -> unit)
    : unit -> MetroTimePickerRollerCell =
    fun () -> create onChange

  let mount
    (
      key: string,
      input: MetroTimePickerRollerInput,
      factory: unit -> MetroTimePickerRollerCell
    ) : Blob =
    Cell.Mount<MetroTimePickerRollerInput, MetroTimePickerRollerCell>(
      key,
      input,
      factory
    )

[<RequireQualifiedAccess>]
module MetroDatePicker =

  let defaults(value: DateOnly voption) : MetroDatePickerInput = {
    Value = value
    Open = false
    FallbackDate = DateOnly(2026, 1, 1)
    MinYear = 1950
    MaxYear = 2050
    Label = ValueNone
    Placeholder = ValueNone
    Disabled = false
    Width = ValueNone
    AccessibilityName = ValueNone
  }

  let formatIso(date: DateOnly) : string = DateTimeInternals.formatIso date

  type DateFieldState = {
    mutable Input: MetroDatePickerInput
    mutable Draft: DateOnly
    mutable WasOpen: bool
  }

  let seedDraft(input: MetroDatePickerInput) : DateOnly =
    let raw = input.Value |> ValueOption.defaultValue input.FallbackDate
    DateTimeInternals.validDate(input.MinYear, input.MaxYear, raw)

  let syncDraft(state: DateFieldState, input: MetroDatePickerInput) : unit =
    let nowOpen = input.Open && not input.Disabled

    if nowOpen && not state.WasOpen then
      state.Draft <- seedDraft input
    elif not nowOpen then
      state.Draft <- seedDraft input
    else
      state.Draft <-
        DateTimeInternals.validDate(input.MinYear, input.MaxYear, state.Draft)

    state.WasOpen <- nowOpen
    state.Input <- input

  let buildView
    (
      state: DateFieldState,
      rollerFactory: unit -> MetroDatePickerRollerCell,
      raise: Action<MetroDatePickerEvent>
    ) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let input = state.Input
    let isOpen = input.Open && not input.Disabled

    let displayText =
      input.Value
      |> ValueOption.map(fun d ->
        formatIso(DateTimeInternals.validDate(input.MinYear, input.MaxYear, d)))
      |> ValueOption.orElse input.Placeholder
      |> ValueOption.defaultValue "Select date..."

    let accName =
      input.AccessibilityName
      |> ValueOption.orElse input.Label
      |> ValueOption.defaultValue "Date picker"

    let trigger =
      DateTimeInternals.buildTriggerButton(
        {
          Key = "date-picker-trigger"
          DisplayText = displayText
          IsPlaceholder = input.Value.IsNone
          IconName = "calendar_today"
          IsOpen = isOpen
          Disabled = input.Disabled
          AccessibilityName = accName
        },
        fun () -> raise.Invoke(MetroDatePickerEvent.OpenChanged(not isOpen))
      )

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 6.0,
        Width = (input.Width |> ValueOption.defaultValue(Length.Percent 100.0))
      )

    input.Label
    |> ValueOption.iter(fun lbl ->
      if not(String.IsNullOrWhiteSpace lbl) then
        root.Add(
          Text(
            Key = "date-picker-label",
            Content = lbl,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Caption
                * theme.TextScaleFactor
              ),
            FontWeight = 600.0
          )
        ))

    root.Add(trigger)

    if isOpen then
      let rollerInput = {
        MetroDatePickerRoller.defaults state.Draft with
            MinYear = input.MinYear
            MaxYear = input.MaxYear
      }

      let roller =
        MetroDatePickerRoller.mount(
          "date-picker-popup-roller",
          rollerInput,
          rollerFactory
        )

      let portal =
        DateTimeInternals.buildPickerModal(
          {
            PortalKey = "date-picker-portal"
            Title = "CHOOSE DATE"
            Roller = roller
          },
          (fun () ->
            raise.Invoke(
              MetroDatePickerEvent.DateCommitted(
                DateTimeInternals.validDate(
                  input.MinYear,
                  input.MaxYear,
                  state.Draft
                )
              )
            )

            raise.Invoke(MetroDatePickerEvent.OpenChanged false)),
          fun () -> raise.Invoke(MetroDatePickerEvent.OpenChanged false)
        )

      root.Add(portal)

    root

  let create(onEvent: MetroDatePickerEvent -> unit) : MetroDatePickerCell =
    let state: DateFieldState = {
      Input = defaults ValueNone
      Draft = DateOnly(2026, 1, 1)
      WasOpen = false
    }

    let mutable self: MetroDatePickerCell voption = ValueNone
    let onEventAction = Action<MetroDatePickerEvent>(onEvent)

    let raise =
      Action<MetroDatePickerEvent>(fun ev ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, ev)))

    // MetroDatePickerRollerCell delivers draft changes through InvokeInputCallback
    // so the owner rule rebuilds this cell automatically.
    let rollerFactory =
      MetroDatePickerRoller.createFactory(fun next -> state.Draft <- next)

    let cell = {
      new MetroDatePickerCell() with
        override _.IsOpen = state.Input.Open && not state.Input.Disabled
        override _.DraftDate = state.Draft
        override _.View = buildView(state, rollerFactory, raise)

        override _.Handle(event) =
          match event with
          | MetroDatePickerEvent.DateCommitted date ->
            let valid =
              DateTimeInternals.validDate(
                state.Input.MinYear,
                state.Input.MaxYear,
                date
              )

            state.Draft <- valid
            raise.Invoke(MetroDatePickerEvent.DateCommitted valid)
            raise.Invoke(MetroDatePickerEvent.OpenChanged false)
          | other -> raise.Invoke(other)

        override _.InputChanged(input) = syncDraft(state, input)

        override _.Build(input) : Blob =
          syncDraft(state, input)
          buildView(state, rollerFactory, raise)
    }

    self <- ValueSome cell
    cell

  let createFactory
    (onEvent: MetroDatePickerEvent -> unit)
    : unit -> MetroDatePickerCell =
    fun () -> create onEvent

  let mount
    (
      key: string,
      input: MetroDatePickerInput,
      factory: unit -> MetroDatePickerCell
    ) : Blob =
    Cell.Mount<MetroDatePickerInput, MetroDatePickerCell>(key, input, factory)

[<RequireQualifiedAccess>]
module MetroTimePicker =

  let defaults(value: TimeOnly voption) : MetroTimePickerInput = {
    Value = value
    Open = false
    FallbackTime = TimeOnly(9, 0)
    Format = MetroClockFormat.TwentyFourHour
    MinuteIncrement = 1
    Label = ValueNone
    Placeholder = ValueNone
    Disabled = false
    Width = ValueNone
    AccessibilityName = ValueNone
  }

  type TimeFieldState = {
    mutable Input: MetroTimePickerInput
    mutable Draft: TimeOnly
    mutable WasOpen: bool
  }

  let seedDraft(input: MetroTimePickerInput) : TimeOnly =
    let raw = input.Value |> ValueOption.defaultValue input.FallbackTime

    TimeOnly(
      raw.Hour,
      DateTimeInternals.snapMinute(raw.Minute, input.MinuteIncrement)
    )

  let syncDraft(state: TimeFieldState, input: MetroTimePickerInput) : unit =
    let nowOpen = input.Open && not input.Disabled

    if nowOpen && not state.WasOpen then
      state.Draft <- seedDraft input
    elif not nowOpen then
      state.Draft <- seedDraft input
    else
      state.Draft <-
        TimeOnly(
          state.Draft.Hour,
          DateTimeInternals.snapMinute(
            state.Draft.Minute,
            input.MinuteIncrement
          )
        )

    state.WasOpen <- nowOpen
    state.Input <- input

  let buildView
    (
      state: TimeFieldState,
      rollerFactory: unit -> MetroTimePickerRollerCell,
      raise: Action<MetroTimePickerEvent>
    ) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let input = state.Input
    let isOpen = input.Open && not input.Disabled

    let displayText =
      input.Value
      |> ValueOption.map(fun t ->
        DateTimeInternals.formatTime(
          TimeOnly(
            t.Hour,
            DateTimeInternals.snapMinute(t.Minute, input.MinuteIncrement)
          ),
          input.Format
        ))
      |> ValueOption.orElse input.Placeholder
      |> ValueOption.defaultValue "Select time..."

    let accName =
      input.AccessibilityName
      |> ValueOption.orElse input.Label
      |> ValueOption.defaultValue "Time picker"

    let trigger =
      DateTimeInternals.buildTriggerButton(
        {
          Key = "time-picker-trigger"
          DisplayText = displayText
          IsPlaceholder = input.Value.IsNone
          IconName = "schedule"
          IsOpen = isOpen
          Disabled = input.Disabled
          AccessibilityName = accName
        },
        fun () -> raise.Invoke(MetroTimePickerEvent.OpenChanged(not isOpen))
      )

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 6.0,
        Width = (input.Width |> ValueOption.defaultValue(Length.Percent 100.0))
      )

    input.Label
    |> ValueOption.iter(fun lbl ->
      if not(String.IsNullOrWhiteSpace lbl) then
        root.Add(
          Text(
            Key = "time-picker-label",
            Content = lbl,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize =
              Length.Pixels(
                MetroTypography.size MetroTextRole.Caption
                * theme.TextScaleFactor
              ),
            FontWeight = 600.0
          )
        ))

    root.Add(trigger)

    if isOpen then
      let rollerInput = {
        MetroTimePickerRoller.defaults state.Draft with
            Format = input.Format
            MinuteIncrement = input.MinuteIncrement
      }

      let roller =
        MetroTimePickerRoller.mount(
          "time-picker-popup-roller",
          rollerInput,
          rollerFactory
        )

      let portal =
        DateTimeInternals.buildPickerModal(
          {
            PortalKey = "time-picker-portal"
            Title = "CHOOSE TIME"
            Roller = roller
          },
          (fun () ->
            let snapped =
              TimeOnly(
                state.Draft.Hour,
                DateTimeInternals.snapMinute(
                  state.Draft.Minute,
                  input.MinuteIncrement
                )
              )

            raise.Invoke(MetroTimePickerEvent.TimeCommitted snapped)
            raise.Invoke(MetroTimePickerEvent.OpenChanged false)),
          fun () -> raise.Invoke(MetroTimePickerEvent.OpenChanged false)
        )

      root.Add(portal)

    root

  let create(onEvent: MetroTimePickerEvent -> unit) : MetroTimePickerCell =
    let state: TimeFieldState = {
      Input = defaults ValueNone
      Draft = TimeOnly(9, 0)
      WasOpen = false
    }

    let mutable self: MetroTimePickerCell voption = ValueNone
    let onEventAction = Action<MetroTimePickerEvent>(onEvent)

    let raise =
      Action<MetroTimePickerEvent>(fun ev ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, ev)))

    let rollerFactory =
      MetroTimePickerRoller.createFactory(fun next -> state.Draft <- next)

    let cell = {
      new MetroTimePickerCell() with
        override _.IsOpen = state.Input.Open && not state.Input.Disabled
        override _.DraftTime = state.Draft
        override _.View = buildView(state, rollerFactory, raise)

        override _.Handle(event) =
          match event with
          | MetroTimePickerEvent.TimeCommitted time ->
            let snapped =
              TimeOnly(
                time.Hour,
                DateTimeInternals.snapMinute(
                  time.Minute,
                  state.Input.MinuteIncrement
                )
              )

            state.Draft <- snapped
            raise.Invoke(MetroTimePickerEvent.TimeCommitted snapped)
            raise.Invoke(MetroTimePickerEvent.OpenChanged false)
          | other -> raise.Invoke(other)

        override _.InputChanged(input) = syncDraft(state, input)

        override _.Build(input) : Blob =
          syncDraft(state, input)
          buildView(state, rollerFactory, raise)
    }

    self <- ValueSome cell
    cell

  let createFactory
    (onEvent: MetroTimePickerEvent -> unit)
    : unit -> MetroTimePickerCell =
    fun () -> create onEvent

  let mount
    (
      key: string,
      input: MetroTimePickerInput,
      factory: unit -> MetroTimePickerCell
    ) : Blob =
    Cell.Mount<MetroTimePickerInput, MetroTimePickerCell>(key, input, factory)
