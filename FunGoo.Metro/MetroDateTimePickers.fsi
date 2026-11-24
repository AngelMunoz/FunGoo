namespace FunGoo.Metro

open System
open Goo

/// Hour display mode for <see cref="MetroTimePickerRollerCell"/> and
/// <see cref="MetroTimePickerCell"/>.
[<Struct>]
type MetroClockFormat =
  /// 12-hour clock with an AM / PM roller column.
  | TwelveHour
  /// 24-hour clock (<c>00..23</c>) with two roller columns.
  | TwentyFourHour

/// Data-only input snapshot for <see cref="MetroDatePickerRollerCell"/>.
[<Struct; NoComparison>]
type MetroDatePickerRollerInput = {
  /// Selected calendar date.
  Value: DateOnly
  /// Earliest selectable year in the year column (defaults to <c>1950</c>).
  MinYear: int
  /// Latest selectable year in the year column (defaults to <c>2050</c>).
  MaxYear: int
  /// Optional field label rendered above the three columns.
  Label: string voption
  /// Disables all columns when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding <see cref="Label"/>.
  AccessibilityName: string voption
}

/// Stateful three-column Metro date roller cell (Month, Day, Year) built on
/// <see cref="MetroLoopingSelectorCell"/>. Clamps days automatically when
/// switching between months of different lengths.
/// <c>MetroDatePickerRoller.create</c> makes the cell.
[<AbstractClass>]
type MetroDatePickerRollerCell =
  inherit Cell<MetroDatePickerRollerInput>

  /// Latest date value of the roller.
  abstract Value: DateOnly

  /// Mounted month roller cell, for tests.
  abstract MonthRoller: MetroLoopingSelectorCell voption

  /// Mounted day roller cell, for tests.
  abstract DayRoller: MetroLoopingSelectorCell voption

  /// Mounted year roller cell, for tests.
  abstract YearRoller: MetroLoopingSelectorCell voption

  /// Gives one date change to the host cell through <c>InvokeInputCallback</c>,
  /// so the owner rule rebuilds the host cell.
  member Raise: callback: Action<DateOnly> * value: DateOnly -> unit

/// Pure date clamping and cell constructors for
/// <see cref="MetroDatePickerRollerCell"/>.
[<RequireQualifiedAccess>]
module MetroDatePickerRoller =

  /// Creates default <see cref="MetroDatePickerRollerInput"/> for
  /// <paramref name="value"/> spanning years <c>1950..2050</c>.
  val defaults: value: DateOnly -> MetroDatePickerRollerInput

  /// Clamps <paramref name="year"/> to <c>1..9999</c>, <paramref name="month"/>
  /// to <c>1..12</c>, and <paramref name="day"/> to the last day of
  /// <paramref name="month"/>.
  val clampDate: year: int * month: int * day: int -> DateOnly

  /// Creates a <see cref="MetroDatePickerRollerCell"/> delivering date changes
  /// to <paramref name="onChange"/> through <c>InvokeInputCallback</c>.
  val create: onChange: (DateOnly -> unit) -> MetroDatePickerRollerCell

  /// Creates a reusable cell factory for <see cref="MetroDatePickerRollerCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onChange: (DateOnly -> unit) -> (unit -> MetroDatePickerRollerCell)

  /// Mounts a <see cref="MetroDatePickerRollerCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroDatePickerRollerInput *
    factory: (unit -> MetroDatePickerRollerCell) ->
      Blob

/// Data-only input snapshot for <see cref="MetroTimePickerRollerCell"/>.
[<Struct; NoComparison>]
type MetroTimePickerRollerInput = {
  /// Selected time of day.
  Value: TimeOnly
  /// 12-hour or 24-hour presentation (<see cref="MetroClockFormat.TwentyFourHour"/>
  /// by default).
  Format: MetroClockFormat
  /// Step between selectable minutes (<c>1..30</c>; defaults to <c>1</c>).
  MinuteIncrement: int
  /// Optional field label rendered above the columns.
  Label: string voption
  /// Disables all columns when <c>true</c>.
  Disabled: bool
  /// Optional accessible name overriding <see cref="Label"/>.
  AccessibilityName: string voption
}

/// Stateful Metro time roller cell (Hour, Minute, and optional AM / PM) built
/// on <see cref="MetroLoopingSelectorCell"/>.
/// <c>MetroTimePickerRoller.create</c> makes the cell.
[<AbstractClass>]
type MetroTimePickerRollerCell =
  inherit Cell<MetroTimePickerRollerInput>

  /// Latest time value of the roller.
  abstract Value: TimeOnly

  /// Mounted hour roller cell, for tests.
  abstract HourRoller: MetroLoopingSelectorCell voption

  /// Mounted minute roller cell, for tests.
  abstract MinuteRoller: MetroLoopingSelectorCell voption

  /// Mounted AM / PM period roller cell when in 12-hour mode, for tests.
  abstract PeriodRoller: MetroLoopingSelectorCell voption

  /// Gives one time change to the host cell through <c>InvokeInputCallback</c>,
  /// so the owner rule rebuilds the host cell.
  member Raise: callback: Action<TimeOnly> * value: TimeOnly -> unit

/// Pure time formatting and cell constructors for
/// <see cref="MetroTimePickerRollerCell"/>.
[<RequireQualifiedAccess>]
module MetroTimePickerRoller =

  /// Creates default <see cref="MetroTimePickerRollerInput"/> for
  /// <paramref name="value"/> in 24-hour format with 1-minute steps.
  val defaults: value: TimeOnly -> MetroTimePickerRollerInput

  /// Formats <paramref name="value"/> according to <paramref name="format"/>
  /// (for example <c>"14:30"</c> or <c>"2:30 PM"</c>).
  val formatTime: value: TimeOnly * format: MetroClockFormat -> string

  /// Creates a <see cref="MetroTimePickerRollerCell"/> delivering time changes
  /// to <paramref name="onChange"/> through <c>InvokeInputCallback</c>.
  val create: onChange: (TimeOnly -> unit) -> MetroTimePickerRollerCell

  /// Creates a reusable cell factory for <see cref="MetroTimePickerRollerCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onChange: (TimeOnly -> unit) -> (unit -> MetroTimePickerRollerCell)

  /// Mounts a <see cref="MetroTimePickerRollerCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroTimePickerRollerInput *
    factory: (unit -> MetroTimePickerRollerCell) ->
      Blob

/// Data-only input snapshot for <see cref="MetroDatePickerCell"/>.
[<Struct; NoComparison>]
type MetroDatePickerInput = {
  /// Currently committed date (or <c>ValueNone</c> when empty).
  Value: DateOnly voption
  /// Opens the modal roller picker sheet when <c>true</c>.
  Open: bool
  /// Fallback date seeded into the roller when <see cref="Value"/> is
  /// <c>ValueNone</c> (defaults to <c>2026-01-01</c>).
  FallbackDate: DateOnly
  /// Earliest selectable year in the popup roller (defaults to <c>1950</c>).
  MinYear: int
  /// Latest selectable year in the popup roller (defaults to <c>2050</c>).
  MaxYear: int
  /// Optional field label rendered above the trigger button.
  Label: string voption
  /// Optional placeholder text shown when <see cref="Value"/> is <c>ValueNone</c>.
  Placeholder: string voption
  /// Disables opening the picker when <c>true</c>.
  Disabled: bool
  /// Optional explicit control width.
  Width: Length voption
  /// Optional accessible name overriding <see cref="Label"/>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="MetroDatePickerCell"/>.
[<Struct; NoComparison>]
type MetroDatePickerEvent =
  /// The user accepted <c>date</c> in the popup roller.
  | DateCommitted of date: DateOnly
  /// The popup roller opened or closed.
  | OpenChanged of isOpen: bool

/// Stateful compact Metro date picker button that opens a modal
/// <see cref="MetroDatePickerRollerCell"/> sheet with Accept and Cancel
/// commands. <c>MetroDatePicker.create</c> makes the cell.
[<AbstractClass>]
type MetroDatePickerCell =
  inherit Cell<MetroDatePickerInput>

  /// <c>true</c> while the modal roller sheet is open.
  abstract IsOpen: bool

  /// Current staged draft date inside the popup roller before confirmation.
  abstract DraftDate: DateOnly

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

  /// Drives one event for tests. <c>DateCommitted d</c> commits the date
  /// <c>d</c> of the event clamped to <c>MinYear</c>..<c>MaxYear</c>.
  abstract Handle: event: MetroDatePickerEvent -> unit

  /// Gives one event to the host cell through <c>InvokeInputCallback</c>, so
  /// the owner rule rebuilds the host cell.
  member Raise:
    callback: Action<MetroDatePickerEvent> * value: MetroDatePickerEvent -> unit

/// Formatting helpers and cell constructors for <see cref="MetroDatePickerCell"/>.
[<RequireQualifiedAccess>]
module MetroDatePicker =

  /// Creates default <see cref="MetroDatePickerInput"/> for
  /// <paramref name="value"/>.
  val defaults: value: DateOnly voption -> MetroDatePickerInput

  /// Formats <paramref name="date"/> in ISO <c>yyyy-MM-dd</c> form.
  val formatIso: date: DateOnly -> string

  /// Creates a <see cref="MetroDatePickerCell"/> delivering events to
  /// <paramref name="onEvent"/> through <c>InvokeInputCallback</c>.
  val create: onEvent: (MetroDatePickerEvent -> unit) -> MetroDatePickerCell

  /// Creates a reusable cell factory for <see cref="MetroDatePickerCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onEvent: (MetroDatePickerEvent -> unit) -> (unit -> MetroDatePickerCell)

  /// Mounts a <see cref="MetroDatePickerCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroDatePickerInput *
    factory: (unit -> MetroDatePickerCell) ->
      Blob

/// Data-only input snapshot for <see cref="MetroTimePickerCell"/>.
[<Struct; NoComparison>]
type MetroTimePickerInput = {
  /// Currently committed time (or <c>ValueNone</c> when empty).
  Value: TimeOnly voption
  /// Opens the modal roller picker sheet when <c>true</c>.
  Open: bool
  /// Fallback time seeded into the roller when <see cref="Value"/> is
  /// <c>ValueNone</c> (defaults to <c>09:00</c>).
  FallbackTime: TimeOnly
  /// 12-hour or 24-hour clock format.
  Format: MetroClockFormat
  /// Step between selectable minutes (<c>1..30</c>; defaults to <c>1</c>).
  MinuteIncrement: int
  /// Optional field label rendered above the trigger button.
  Label: string voption
  /// Optional placeholder text shown when <see cref="Value"/> is <c>ValueNone</c>.
  Placeholder: string voption
  /// Disables opening the picker when <c>true</c>.
  Disabled: bool
  /// Optional explicit control width.
  Width: Length voption
  /// Optional accessible name overriding <see cref="Label"/>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="MetroTimePickerCell"/>.
[<Struct; NoComparison>]
type MetroTimePickerEvent =
  /// The user accepted <c>time</c> in the popup roller.
  | TimeCommitted of time: TimeOnly
  /// The popup roller opened or closed.
  | OpenChanged of isOpen: bool

/// Stateful compact Metro time picker button that opens a modal
/// <see cref="MetroTimePickerRollerCell"/> sheet with Accept and Cancel
/// commands. <c>MetroTimePicker.create</c> makes the cell.
[<AbstractClass>]
type MetroTimePickerCell =
  inherit Cell<MetroTimePickerInput>

  /// <c>true</c> while the modal roller sheet is open.
  abstract IsOpen: bool

  /// Current staged draft time inside the popup roller before confirmation.
  abstract DraftTime: TimeOnly

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

  /// Drives one event for tests. <c>TimeCommitted t</c> commits the time
  /// <c>t</c> of the event with the minute snapped to <c>MinuteIncrement</c>.
  abstract Handle: event: MetroTimePickerEvent -> unit

  /// Gives one event to the host cell through <c>InvokeInputCallback</c>, so
  /// the owner rule rebuilds the host cell.
  member Raise:
    callback: Action<MetroTimePickerEvent> * value: MetroTimePickerEvent -> unit

/// Arguments for <c>DateTimeInternals.buildSheet</c>.
[<Struct; NoComparison>]
type PickerModalArgs = {
  /// Stable key of the portal; the sheet and backdrop keys derive from it.
  PortalKey: string
  /// Title rendered at the top of the sheet.
  Title: string
  /// Roller content mounted between the title and the Accept / Cancel row.
  Roller: Blob
}

/// Shared trigger, sheet, and formatting helpers of the Metro date and time
/// pickers.
module DateTimeInternals =

  /// Builds the content of the modal sheet portal. Returns the dimmed
  /// backdrop and the sheet as a pair; the sheet is not inside the backdrop.
  /// The backdrop carries the dim color and the cancel press handler. The
  /// sheet carries the title, the roller, and the Accept and Cancel buttons.
  /// The cell adds the backdrop first and the sheet second to its
  /// <see cref="Portal"/>. Call it inside a build where the Metro theme is
  /// readable. A test reads it.
  val buildSheet:
    args: PickerModalArgs * onAccept: (unit -> unit) * onCancel: (unit -> unit) ->
      struct (Blob * Blob)

/// Cell constructors for <see cref="MetroTimePickerCell"/>.
[<RequireQualifiedAccess>]
module MetroTimePicker =

  /// Creates default <see cref="MetroTimePickerInput"/> for
  /// <paramref name="value"/> in 24-hour format.
  val defaults: value: TimeOnly voption -> MetroTimePickerInput

  /// Creates a <see cref="MetroTimePickerCell"/> delivering events to
  /// <paramref name="onEvent"/> through <c>InvokeInputCallback</c>.
  val create: onEvent: (MetroTimePickerEvent -> unit) -> MetroTimePickerCell

  /// Creates a reusable cell factory for <see cref="MetroTimePickerCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onEvent: (MetroTimePickerEvent -> unit) -> (unit -> MetroTimePickerCell)

  /// Mounts a <see cref="MetroTimePickerCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroTimePickerInput *
    factory: (unit -> MetroTimePickerCell) ->
      Blob
