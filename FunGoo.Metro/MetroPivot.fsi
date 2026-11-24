namespace FunGoo.Metro

open System
open Goo

/// Descriptor for a single section tab in a <see cref="MetroPivotCell"/>.
[<Struct; NoComparison>]
type MetroPivotItem = {
  /// Stable unique identifier for the pivot item.
  Id: string
  /// Display header text rendered in the horizontal pivot strip.
  Header: string
}

/// Data-only input snapshot for <see cref="MetroPivotCell"/>.
[<Struct; NoComparison>]
type MetroPivotInput = {
  /// Optional uppercase application or view title rendered above the headers.
  Title: string voption
  /// Ordered pivot items displayed in the header strip.
  Items: MetroPivotItem array
  /// Identifier of the currently active pivot item.
  SelectedId: string
  /// Wraps keyboard and swipe navigation across ends when <c>true</c>;
  /// clamps at the first and last items when <c>false</c>.
  Loop: bool
  /// Optional accessible name for the pivot tab list.
  AccessibilityName: string voption
}

/// Stateful Metro Pivot navigation cell with peeking light-weight headers,
/// keyboard and pointer-swipe navigation, and animated content transitions.
/// <c>MetroPivot.create</c> makes the cell. The type holds the surface that
/// hosts and tests use.
[<AbstractClass>]
type MetroPivotCell =
  inherit Cell<MetroPivotInput>

  /// Current paint-bound content translation animation. A test reads it.
  abstract ContentTranslate: Anim<Point>

  /// Current paint-bound content opacity animation. A test reads it.
  abstract ContentOpacity: Anim<float>

  /// The tree of the current input: the title, the horizontal header strip,
  /// and the active content panel. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

  /// Gives one selection of this cell to the host cell, as a Goo.Widgets
  /// control does. The owner rule then rebuilds the host cell, so a host
  /// does not call <c>Rebuild</c> on itself in the selection callback.
  member Raise: callback: Action<string> * value: string -> unit

/// Construction and mounting helpers for <see cref="MetroPivotCell"/>.
[<RequireQualifiedAccess>]
module MetroPivot =

  /// Creates default <see cref="MetroPivotInput"/> for
  /// <paramref name="items"/> and <paramref name="selectedId"/> with circular
  /// looping enabled.
  val defaults:
    items: MetroPivotItem array * selectedId: string -> MetroPivotInput

  /// Creates a pivot cell. The cell gives each navigation to
  /// <paramref name="onSelect"/>, and the owner rule then rebuilds the host
  /// cell. It builds the active panel through <paramref name="renderContent"/>.
  /// Inactive tab content unmounts on switch, so any <c>Cell.Mount</c> factory
  /// inside <paramref name="renderContent"/> must return a fresh <c>Cell</c>.
  /// The cell calls <paramref name="renderContent"/> inside its own
  /// <c>Build</c>. A callback that <paramref name="renderContent"/> creates
  /// belongs to this cell, so the owner rule rebuilds this cell and not the
  /// caller. Such a callback must rebuild the caller cell when it changes the
  /// state of the caller cell.
  val create:
    onSelect: (string -> unit) * renderContent: (string -> Blob) ->
      MetroPivotCell

  /// Creates a reusable cell factory for <see cref="MetroPivotCell"/>. Cache
  /// the returned factory outside <c>Build</c>.
  val createFactory:
    onSelect: (string -> unit) * renderContent: (string -> Blob) ->
      unit ->
        MetroPivotCell

  /// Mounts a <see cref="MetroPivotCell"/> under stable <paramref name="key"/>
  /// using the cached <paramref name="factory"/>.
  val mount:
    key: string * input: MetroPivotInput * factory: (unit -> MetroPivotCell) ->
      Blob
