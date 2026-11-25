namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Inputs

/// Data-only properties for mounting a Metro month-grid calendar.
[<Struct; NoComparison>]
type MetroCalendarProps = {
  /// Currently selected date (or <c>ValueNone</c> when unselected).
  Value: DateOnly voption
  /// Controlled visible month (or <c>ValueNone</c> to let the widget manage
  /// month navigation internally).
  Month: DateOnly voption
  /// Optional earliest selectable date (inclusive).
  Minimum: DateOnly voption
  /// Optional latest selectable date (inclusive).
  Maximum: DateOnly voption
  /// Optional explicit "today" highlight override for deterministic rendering
  /// and tests.
  Today: DateOnly voption
  /// Optional first day of the week column override.
  FirstDayOfWeek: DayOfWeek voption
  /// Optional explicit calendar width (defaults to <c>280 px</c>).
  Width: Length voption
  /// Day cell height in logical pixels (defaults to <c>36.0</c>).
  DayHeight: float
  /// Disables date selection and month navigation when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding <c>"Calendar"</c>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="IMetroCalendarFactory"/>.
[<Struct; NoComparison>]
type MetroCalendarEvent =
  /// The user selected <c>date</c>.
  | DateSelected of date: DateOnly
  /// The visible month navigated to <c>firstOfMonth</c>.
  | MonthChanged of firstOfMonth: DateOnly

/// Factory for Metro month-grid calendars backed by
/// <see cref="Goo.Widgets.Inputs.Calendar"/>. Create one factory per calendar,
/// outside <c>Build</c>, so callback delegates stay cached across builds.
type IMetroCalendarFactory =

  /// Returns the <see cref="CalendarInput"/> for <paramref name="props"/> with
  /// cached callbacks and sharp Metro day-cell styling.
  abstract CreateInput: props: MetroCalendarProps -> CalendarInput

  /// Mounts a Metro calendar under stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroCalendarProps -> Blob

/// Pure mappers and factory constructors for Metro calendars.
[<RequireQualifiedAccess>]
module MetroCalendar =

  /// Default day cell height (<c>36.0</c> logical pixels).
  val defaultDayHeightPx: float

  /// Creates default <see cref="MetroCalendarProps"/> for
  /// <paramref name="value"/>.
  val defaults: value: DateOnly voption -> MetroCalendarProps

  /// Purely maps <paramref name="props"/> to <see cref="CalendarInput"/> data
  /// fields without callbacks.
  val toData: props: MetroCalendarProps -> CalendarInput

  /// Creates an <see cref="IMetroCalendarFactory"/> delivering events to
  /// <paramref name="onEvent"/>. Because <see cref="Calendar"/> invokes its
  /// callbacks through <c>InvokeInputCallback</c>, the owner rule rebuilds the
  /// host cell automatically.
  val createFactory:
    onEvent: (MetroCalendarEvent -> unit) -> IMetroCalendarFactory

/// Data-only properties for mounting an editable Metro calendar date picker
/// with an anchored dropdown calendar popup.
[<Struct; NoComparison>]
type MetroCalendarDatePickerProps = {
  /// Currently selected date (or <c>ValueNone</c> when empty).
  Value: DateOnly voption
  /// Controlled popup open state (or <c>ValueNone</c> to let the widget own
  /// opening and closing).
  Open: bool voption
  /// Permits clearing the text input to <c>ValueNone</c> when <c>true</c>.
  AllowEmpty: bool
  /// Optional earliest selectable date (inclusive).
  Minimum: DateOnly voption
  /// Optional latest selectable date (inclusive).
  Maximum: DateOnly voption
  /// Optional explicit "today" highlight override for the popup calendar.
  Today: DateOnly voption
  /// Optional first day of the week column override.
  FirstDayOfWeek: DayOfWeek voption
  /// Optional placeholder text shown when <see cref="Value"/> is <c>ValueNone</c>.
  Placeholder: string voption
  /// Optional explicit field width (defaults to <c>280 px</c>; fixed pixel
  /// widths must exceed <c>40 px</c>).
  Width: Length voption
  /// Popup calendar width in logical pixels (defaults to <c>300.0</c>).
  PopupWidth: float
  /// Disables text entry and popup opening when <c>true</c>.
  Disabled: bool
  /// Blocks edits and popup opening while keeping focus and copy when
  /// <c>true</c>.
  ReadOnly: bool
  /// Optional accessible name overriding <c>"Date"</c>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="IMetroCalendarDatePickerFactory"/>.
[<Struct; NoComparison>]
type MetroCalendarDatePickerEvent =
  /// The committed date changed (<c>ValueNone</c> when cleared).
  | ValueChanged of date: DateOnly voption
  /// The dropdown calendar popup requested opening or closing.
  | OpenChanged of isOpen: bool
  /// An unparseable text draft was submitted.
  | InvalidInput of rawText: string

/// Factory for Metro calendar date pickers backed by
/// <see cref="Goo.Widgets.Inputs.DatePicker"/>. Create one factory per control,
/// outside <c>Build</c>, so callback delegates stay cached across builds.
type IMetroCalendarDatePickerFactory =

  /// Returns the <see cref="DatePickerInput"/> for <paramref name="props"/>
  /// with cached callbacks and sharp Metro popup and calendar styling.
  abstract CreateInput: props: MetroCalendarDatePickerProps -> DatePickerInput

  /// Mounts a Metro calendar date picker under stable <paramref name="key"/>.
  abstract Mount: key: string * props: MetroCalendarDatePickerProps -> Blob

/// Pure mappers and factory constructors for Metro calendar date pickers.
[<RequireQualifiedAccess>]
module MetroCalendarDatePicker =

  /// Default popup calendar width (<c>300.0</c> logical pixels).
  val defaultPopupWidthPx: float

  /// Creates default <see cref="MetroCalendarDatePickerProps"/> for
  /// <paramref name="value"/>.
  val defaults: value: DateOnly voption -> MetroCalendarDatePickerProps

  /// Purely maps <paramref name="props"/> to <see cref="DatePickerInput"/> data
  /// fields without callbacks.
  val toData: props: MetroCalendarDatePickerProps -> DatePickerInput

  /// Creates an <see cref="IMetroCalendarDatePickerFactory"/> delivering events
  /// to <paramref name="onEvent"/>. Because <see cref="DatePicker"/> invokes
  /// its callbacks through <c>InvokeInputCallback</c>, the owner rule rebuilds
  /// the host cell automatically.
  val createFactory:
    onEvent: (MetroCalendarDatePickerEvent -> unit) ->
      IMetroCalendarDatePickerFactory
