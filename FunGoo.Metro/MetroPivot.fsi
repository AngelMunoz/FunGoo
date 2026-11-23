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
[<Sealed>]
type MetroPivotCell =
  inherit Cell<MetroPivotInput>

  /// Creates a pivot cell invoking <paramref name="onSelect"/> on navigation
  /// and building the active panel via <paramref name="renderContent"/>.
  new:
    onSelect: (string -> unit) * renderContent: (string -> Blob) ->
      MetroPivotCell

  /// Current paint-bound content translation animation.
  member ContentTranslate: Anim<Point>

  /// Current paint-bound content opacity animation.
  member ContentOpacity: Anim<float>

  /// Triggers the directional content entrance transition when
  /// <see cref="MetroPivotInput.SelectedId"/> changes.
  override InputChanged: input: MetroPivotInput -> unit

  /// Resets pointer swipe state, selection tracking, and animations on unmount.
  override Unmounting: unit -> unit

  /// Builds the pivot title, horizontal header strip, and active content panel.
  override Build: input: MetroPivotInput -> Blob

/// Construction and mounting helpers for <see cref="MetroPivotCell"/>.
[<RequireQualifiedAccess>]
module MetroPivot =

  /// Creates default <see cref="MetroPivotInput"/> for
  /// <paramref name="items"/> and <paramref name="selectedId"/> with circular
  /// looping enabled.
  val defaults:
    items: MetroPivotItem array * selectedId: string -> MetroPivotInput

  /// Creates a reusable cell factory for <see cref="MetroPivotCell"/>. Cache
  /// the returned factory outside <c>Build</c>.
  val createFactory:
    onSelect: (string -> unit) * renderContent: (string -> Blob) ->
      Func<MetroPivotCell>

  /// Mounts a <see cref="MetroPivotCell"/> under stable <paramref name="key"/>
  /// using the cached <paramref name="factory"/>.
  val mount:
    key: string * input: MetroPivotInput * factory: Func<MetroPivotCell> -> Blob
