namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Inputs

[<Struct; NoComparison>]
type MetroCalendarProps = {
  Value: DateOnly voption
  Month: DateOnly voption
  Minimum: DateOnly voption
  Maximum: DateOnly voption
  Today: DateOnly voption
  FirstDayOfWeek: DayOfWeek voption
  Width: Length voption
  DayHeight: float
  Disabled: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroCalendarEvent =
  | DateSelected of date: DateOnly
  | MonthChanged of firstOfMonth: DateOnly

type IMetroCalendarFactory =

  abstract CreateInput: props: MetroCalendarProps -> CalendarInput

  abstract Mount: key: string * props: MetroCalendarProps -> Blob

[<Struct; NoComparison>]
type MetroCalendarDatePickerProps = {
  Value: DateOnly voption
  Open: bool voption
  AllowEmpty: bool
  Minimum: DateOnly voption
  Maximum: DateOnly voption
  Today: DateOnly voption
  FirstDayOfWeek: DayOfWeek voption
  Placeholder: string voption
  Width: Length voption
  PopupWidth: float
  Disabled: bool
  ReadOnly: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroCalendarDatePickerEvent =
  | ValueChanged of date: DateOnly voption
  | OpenChanged of isOpen: bool
  | InvalidInput of rawText: string

type IMetroCalendarDatePickerFactory =

  abstract CreateInput: props: MetroCalendarDatePickerProps -> DatePickerInput

  abstract Mount: key: string * props: MetroCalendarDatePickerProps -> Blob

module CalendarInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)

  [<Literal>]
  let defaultDayPx = 36.0

  [<Literal>]
  let defaultPopupPx = 300.0

  let toNullable(opt: 'T voption) : Nullable<'T> =
    opt |> ValueOption.map Nullable |> ValueOption.defaultValue(Nullable())

  let resolveDayHeight(value: float) : float =
    if Double.IsFinite value && value > 0.0 then
      value
    else
      defaultDayPx

  let resolvePopupWidth(value: float) : float =
    if Double.IsFinite value && value > 40.0 then
      value
    else
      defaultPopupPx

  // Calendar owns Accessibility, Disabled, Focusable, Handle, Key, KeyBindings,
  // OnBlur, OnClick, OnFocus, and TabStop on day buttons. Configure visual
  // properties only.
  let styleDay(_input: CalendarInput, day: CalendarDay, btn: Button) : unit =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    btn.BorderRadius <- zeroRadius
    btn.BoxShadows <- emptyShadows

    if day.Selected then
      btn.BackgroundColor <-
        (if theme.HighContrast then p.Highlight else a.Base)

      btn.BorderWidth <- metroBorder

      btn.BorderColor <-
        Edges<Color>(if theme.HighContrast then p.Foreground else a.Base)
    elif day.Today then
      btn.BorderWidth <- metroBorder

      btn.BorderColor <-
        Edges<Color>(if theme.HighContrast then p.Foreground else a.Base)

      btn.BackgroundColor <- Color.Transparent

    // The calendar paints the day number on its own Text child, so the color
    // must go on that child. A disabled day keeps the widget color.
    let children = btn.Children

    if not day.Disabled && children.Count > 0 then
      match children[0] with
      | :? Text as label ->
        label.Color <-
          if day.Selected then
            (if theme.HighContrast then p.Foreground else a.OnAccent)
          elif day.Today then
            p.Foreground
          elif not day.CurrentMonth then
            p.ForegroundSecondary
          else
            p.Foreground
      | _ -> ()

  let styleNav(_input: CalendarInput, _dir: int, btn: Button) : unit =
    btn.BorderRadius <- zeroRadius
    btn.BoxShadows <- emptyShadows

  let styleCalendarRoot(_input: CalendarInput, root: Container) : unit =
    let theme = Metro.read()
    let p = theme.Palette
    root.BorderRadius <- zeroRadius
    root.BoxShadows <- emptyShadows

    root.BorderWidth <-
      (if theme.HighContrast then
         metroBorder
       else
         Edges(Length.Pixels 0.0))

    root.BorderColor <-
      Edges<Color>(if theme.HighContrast then p.Foreground else p.BorderStrong)

    root.BackgroundColor <- p.BackgroundAlt

  let configureDayDelegate =
    Action<CalendarInput, CalendarDay, Button>(fun input day btn ->
      styleDay(input, day, btn))

  let configureNavDelegate =
    Action<CalendarInput, int, Button>(fun input dir btn ->
      styleNav(input, dir, btn))

  let configureCalendarRootDelegate =
    Action<CalendarInput, Container>(fun input root ->
      styleCalendarRoot(input, root))

[<RequireQualifiedAccess>]
module MetroCalendar =

  let defaultDayHeightPx = CalendarInternals.defaultDayPx

  let defaults(value: DateOnly voption) : MetroCalendarProps = {
    Value = value
    Month = ValueNone
    Minimum = ValueNone
    Maximum = ValueNone
    Today = ValueNone
    FirstDayOfWeek = ValueNone
    Width = ValueNone
    DayHeight = defaultDayHeightPx
    Disabled = false
    AccessibilityName = ValueNone
  }

  let toData(props: MetroCalendarProps) : CalendarInput =
    CalendarInput(
      Value = CalendarInternals.toNullable props.Value,
      Month = CalendarInternals.toNullable props.Month,
      Minimum = CalendarInternals.toNullable props.Minimum,
      Maximum = CalendarInternals.toNullable props.Maximum,
      Today = CalendarInternals.toNullable props.Today,
      FirstDayOfWeek = CalendarInternals.toNullable props.FirstDayOfWeek,
      Width = CalendarInternals.toNullable props.Width,
      DayHeight = Nullable(CalendarInternals.resolveDayHeight props.DayHeight),
      Disabled = props.Disabled,
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName
    )

  type CalendarHandlers = {
    OnChange: Action<DateOnly>
    OnMonthChange: Action<DateOnly>
  }

  let createInput
    (handlers: CalendarHandlers, props: MetroCalendarProps)
    : CalendarInput =
    let data = toData props

    CalendarInput(
      Value = data.Value,
      OnChange = handlers.OnChange,
      Month = data.Month,
      OnMonthChange = handlers.OnMonthChange,
      Minimum = data.Minimum,
      Maximum = data.Maximum,
      Today = data.Today,
      FirstDayOfWeek = data.FirstDayOfWeek,
      Width = data.Width,
      DayHeight = data.DayHeight,
      Disabled = data.Disabled,
      AccessibilityName = data.AccessibilityName,
      ConfigureDay = CalendarInternals.configureDayDelegate,
      ConfigureNavigation = CalendarInternals.configureNavDelegate,
      ConfigureRoot = CalendarInternals.configureCalendarRootDelegate
    )

  let mount
    (handlers: CalendarHandlers, key: string, props: MetroCalendarProps)
    : Blob =
    Cell.Mount<CalendarInput, Calendar>(key, createInput(handlers, props))

  let createFactory
    (onEvent: MetroCalendarEvent -> unit)
    : IMetroCalendarFactory =
    let handlers: CalendarHandlers = {
      OnChange =
        Action<DateOnly>(fun date ->
          onEvent(MetroCalendarEvent.DateSelected date))
      OnMonthChange =
        Action<DateOnly>(fun month ->
          onEvent(MetroCalendarEvent.MonthChanged month))
    }

    {
      new IMetroCalendarFactory with
        member _.CreateInput(props) = createInput(handlers, props)
        member _.Mount(key, props) = mount(handlers, key, props)
    }

[<RequireQualifiedAccess>]
module MetroCalendarDatePicker =

  let defaultPopupWidthPx = CalendarInternals.defaultPopupPx

  let defaults(value: DateOnly voption) : MetroCalendarDatePickerProps = {
    Value = value
    Open = ValueNone
    AllowEmpty = true
    Minimum = ValueNone
    Maximum = ValueNone
    Today = ValueNone
    FirstDayOfWeek = ValueNone
    Placeholder = ValueNone
    Width = ValueNone
    PopupWidth = defaultPopupWidthPx
    Disabled = false
    ReadOnly = false
    AccessibilityName = ValueNone
  }

  let toData(props: MetroCalendarDatePickerProps) : DatePickerInput =
    DatePickerInput(
      Value = CalendarInternals.toNullable props.Value,
      Open = CalendarInternals.toNullable props.Open,
      AllowEmpty = Nullable props.AllowEmpty,
      Minimum = CalendarInternals.toNullable props.Minimum,
      Maximum = CalendarInternals.toNullable props.Maximum,
      Today = CalendarInternals.toNullable props.Today,
      FirstDayOfWeek = CalendarInternals.toNullable props.FirstDayOfWeek,
      Placeholder = ValueOption.defaultValue null props.Placeholder,
      Width = CalendarInternals.toNullable props.Width,
      PopupWidth =
        Nullable(CalendarInternals.resolvePopupWidth props.PopupWidth),
      Disabled = props.Disabled,
      ReadOnly = props.ReadOnly,
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName
    )

  let stylePopup(_input: DatePickerInput, popup: Container) : unit =
    let theme = Metro.read()
    let p = theme.Palette
    popup.BorderRadius <- CalendarInternals.zeroRadius
    popup.BoxShadows <- CalendarInternals.emptyShadows
    popup.BorderWidth <- CalendarInternals.metroBorder

    popup.BorderColor <-
      Edges<Color>(if theme.HighContrast then p.Foreground else p.BorderStrong)

  let styleButton(_input: DatePickerInput, btn: Button) : unit =
    btn.BorderRadius <- CalendarInternals.zeroRadius
    btn.BoxShadows <- CalendarInternals.emptyShadows

  let styleRoot(_input: DatePickerInput, root: Container) : unit =
    root.BorderRadius <- CalendarInternals.zeroRadius
    root.BoxShadows <- CalendarInternals.emptyShadows

  let configureCalendarRootNoBorderDelegate =
    Action<CalendarInput, Container>(fun input root ->
      CalendarInternals.styleCalendarRoot(input, root)

      root.BorderWidth <- Edges(Length.Pixels 0.0))

  let customizeCalendar
    (_dpInput: DatePickerInput, cal: CalendarInput)
    : CalendarInput =
    let mutable next = cal
    next.ConfigureDay <- CalendarInternals.configureDayDelegate
    next.ConfigureNavigation <- CalendarInternals.configureNavDelegate
    next.ConfigureRoot <- configureCalendarRootNoBorderDelegate
    next

  let configurePopupDelegate =
    Action<DatePickerInput, Container>(fun input popup ->
      stylePopup(input, popup))

  let configureButtonDelegate =
    Action<DatePickerInput, Button>(fun input btn -> styleButton(input, btn))

  let configureRootDelegate =
    Action<DatePickerInput, Container>(fun input root -> styleRoot(input, root))

  let createCalendarDelegate =
    Func<DatePickerInput, CalendarInput, CalendarInput>(fun dp cal ->
      customizeCalendar(dp, cal))

  type DatePickerHandlers = {
    OnChange: Action<Nullable<DateOnly>>
    OnOpenChange: Action<bool>
    OnInvalid: Action<string>
  }

  let createInput
    (handlers: DatePickerHandlers, props: MetroCalendarDatePickerProps)
    : DatePickerInput =
    let data = toData props

    DatePickerInput(
      Value = data.Value,
      OnChange = handlers.OnChange,
      Open = data.Open,
      OnOpenChange = handlers.OnOpenChange,
      OnInvalid = handlers.OnInvalid,
      AllowEmpty = data.AllowEmpty,
      Minimum = data.Minimum,
      Maximum = data.Maximum,
      Today = data.Today,
      FirstDayOfWeek = data.FirstDayOfWeek,
      Placeholder = data.Placeholder,
      Width = data.Width,
      PopupWidth = data.PopupWidth,
      Disabled = data.Disabled,
      ReadOnly = data.ReadOnly,
      AccessibilityName = data.AccessibilityName,
      CreateCalendar = createCalendarDelegate,
      ConfigureButton = configureButtonDelegate,
      ConfigurePopup = configurePopupDelegate,
      ConfigureRoot = configureRootDelegate
    )

  let mount
    (
      handlers: DatePickerHandlers,
      key: string,
      props: MetroCalendarDatePickerProps
    ) : Blob =
    Cell.Mount<DatePickerInput, DatePicker>(key, createInput(handlers, props))

  let createFactory
    (onEvent: MetroCalendarDatePickerEvent -> unit)
    : IMetroCalendarDatePickerFactory =
    let handlers: DatePickerHandlers = {
      OnChange =
        Action<Nullable<DateOnly>>(fun nullableDate ->
          onEvent(
            MetroCalendarDatePickerEvent.ValueChanged(
              ValueOption.ofNullable nullableDate
            )
          ))
      OnOpenChange =
        Action<bool>(fun isOpen ->
          onEvent(MetroCalendarDatePickerEvent.OpenChanged isOpen))
      OnInvalid =
        Action<string>(fun raw ->
          onEvent(MetroCalendarDatePickerEvent.InvalidInput raw))
    }

    {
      new IMetroCalendarDatePickerFactory with
        member _.CreateInput(props) = createInput(handlers, props)
        member _.Mount(key, props) = mount(handlers, key, props)
    }
