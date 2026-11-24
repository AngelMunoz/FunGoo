namespace FunGoo.Metro

open System
open Goo

/// Descriptor for one selectable slot in a <see cref="MetroLoopingSelectorCell"/>.
[<Struct; NoComparison>]
type MetroLoopingItem = {
  /// Stable unique identifier of the item.
  Id: string
  /// Primary text rendered inside the tile (for example <c>"24"</c> or <c>"03"</c>).
  PrimaryText: string
  /// Optional secondary caption rendered below the primary text (for example
  /// <c>"tuesday"</c> or <c>"march"</c>).
  SecondaryText: string voption
}

/// Data-only input snapshot for <see cref="MetroLoopingSelectorCell"/>.
[<Struct; NoComparison>]
type MetroLoopingSelectorInput = {
  /// Ordered items displayed in the vertical roller column.
  Items: MetroLoopingItem array
  /// Identifier of the active center item.
  SelectedId: string
  /// Wraps around across the first and last items when <c>true</c>; clamps at
  /// the ends when <c>false</c>.
  Loop: bool
  /// Number of neighbor tiles shown above and below the active center tile
  /// (clamped to <c>1..3</c>; defaults to <c>1</c>).
  VisibleNeighbors: int
  /// Tile width in logical pixels (defaults to <c>84.0</c>).
  ItemWidth: float
  /// Tile height in logical pixels (defaults to <c>60.0</c>).
  ItemHeight: float
  /// Disables pointer, wheel, and keyboard stepping when <c>true</c>.
  Disabled: bool
  /// Optional accessible name for the spin-button / listbox column.
  AccessibilityName: string voption
}

/// Explicit state snapshot of <see cref="MetroLoopingSelectorCell"/>.
[<Struct; NoComparison>]
type MetroLoopingSelectorState = {
  /// Latest input snapshot supplied to the cell.
  Input: MetroLoopingSelectorInput
  /// Paint-bound vertical slide animation of the roller column.
  TrackTranslate: Anim<Point>
}

/// Arguments for <c>MetroLoopingSelector.resolveOffsetIndex</c>.
[<Struct; NoComparison>]
type MetroLoopingStepArgs = {
  /// Zero-based index of the current item.
  Current: int
  /// Signed step offset from <see cref="Current"/>.
  Delta: int
  /// Total number of items in the column.
  Count: int
  /// Wraps around across ends when <c>true</c>.
  Loop: bool
}

/// Stateful Metro vertical looping roller column supporting click-neighbor,
/// mouse-wheel, vertical drag, and keyboard navigation with slide animations.
/// <c>MetroLoopingSelector.create</c> makes the cell.
[<AbstractClass>]
type MetroLoopingSelectorCell =
  inherit Cell<MetroLoopingSelectorInput>

  /// Currently selected item identifier: the id of the input as the host
  /// gave it. An unknown id shows and selects the item at index 0.
  abstract SelectedId: string

  /// Zero-based index of the active item (or <c>-1</c> when empty). An unknown
  /// <c>SelectedId</c> selects the item at index 0.
  abstract SelectedIndex: int

  /// Paint-bound vertical slide animation of the column track. A test reads it.
  abstract TrackTranslate: Anim<Point>

  /// Current explicit state snapshot. A test reads it.
  abstract State: MetroLoopingSelectorState

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

  /// Steps the selection by <paramref name="delta"/> items through the same
  /// callback path as wheel, drag, and arrow keys.
  abstract Step: delta: int -> unit

  /// Gives one selection to the host cell through <c>InvokeInputCallback</c>,
  /// so the owner rule rebuilds the host cell.
  member Raise: callback: Action<string> * value: string -> unit

/// Pure index math, view builders, and cell constructors for
/// <see cref="MetroLoopingSelectorCell"/>.
[<RequireQualifiedAccess>]
module MetroLoopingSelector =

  /// Default tile width (<c>84.0</c> logical pixels).
  val defaultWidthPx: float

  /// Default tile height (<c>60.0</c> logical pixels).
  val defaultHeightPx: float

  /// Helper creating a single-line <see cref="MetroLoopingItem"/>.
  val item: id: string * primaryText: string -> MetroLoopingItem

  /// Helper creating a two-line <see cref="MetroLoopingItem"/> with a secondary
  /// caption.
  val itemWithCaption:
    id: string * primaryText: string * secondaryText: string -> MetroLoopingItem

  /// Creates default <see cref="MetroLoopingSelectorInput"/> for
  /// <paramref name="items"/> and <paramref name="selectedId"/> with circular
  /// looping enabled and one neighbor above and below.
  val defaults:
    items: MetroLoopingItem array * selectedId: string ->
      MetroLoopingSelectorInput

  /// Resolves the target index when stepping according to
  /// <paramref name="args"/>. Returns <c>-1</c> when out of range and
  /// <c>args.Loop</c> is <c>false</c> or when <c>args.Count</c> is zero.
  val resolveOffsetIndex: args: MetroLoopingStepArgs -> int

  /// Builds the <see cref="Blob"/> tree for an explicit
  /// <see cref="MetroLoopingSelectorState"/> and <paramref name="onSelect"/>
  /// handler. Call inside a build where the Metro theme is readable.
  val buildView:
    state: MetroLoopingSelectorState * onSelect: (string -> unit) -> Blob

  /// Creates a <see cref="MetroLoopingSelectorCell"/> delivering selected item
  /// IDs to <paramref name="onSelect"/> through <c>InvokeInputCallback</c>.
  val create: onSelect: (string -> unit) -> MetroLoopingSelectorCell

  /// Creates a reusable cell factory for <see cref="MetroLoopingSelectorCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onSelect: (string -> unit) -> (unit -> MetroLoopingSelectorCell)

  /// Mounts a <see cref="MetroLoopingSelectorCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroLoopingSelectorInput *
    factory: (unit -> MetroLoopingSelectorCell) ->
      Blob
