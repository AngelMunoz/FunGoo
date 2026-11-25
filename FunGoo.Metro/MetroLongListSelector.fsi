namespace FunGoo.Metro

open System
open Goo

/// Descriptor for a single item inside a <see cref="MetroLongListGroup"/>.
[<Struct; NoComparison>]
type MetroLongListItem = {
  /// Stable unique identifier across all groups in the selector.
  Id: string
  /// Primary visible row label.
  Label: string
  /// Optional secondary caption rendered below or beside the label.
  Caption: string voption
  /// Disables activation and selection when <c>true</c>.
  Disabled: bool
}

/// Descriptor for one section group in a <see cref="MetroLongListSelectorCell"/>.
[<Struct; NoComparison>]
type MetroLongListGroup = {
  /// Stable unique group key used for jump navigation and scroll targets.
  Key: string
  /// Group header title rendered in uppercase in the list and on jump tiles.
  Title: string
  /// Items belonging to this group. An empty array renders a dimmed, disabled
  /// tile in the jump grid so full A–Z grids keep empty letters visible.
  Items: MetroLongListItem array
}

/// Flattened virtual row produced by <c>MetroLongListSelector.flattenRows</c>.
[<Struct; NoComparison>]
type MetroLongListRow =
  /// Group header row for <c>key</c> and <c>title</c>.
  | Header of key: string * title: string * interactive: bool
  /// Item row with its resolved selection flag.
  | Item of item: MetroLongListItem * selected: bool

/// Data-only input snapshot for <see cref="MetroLongListSelectorCell"/>.
[<Struct; NoComparison>]
type MetroLongListInput = {
  /// Ordered groups rendered in the list and in the jump grid.
  Groups: MetroLongListGroup array
  /// Currently selected item identifiers.
  SelectedIds: string array
  /// Selection mode (<see cref="MetroListSelectionMode.Activate"/>,
  /// <see cref="MetroListSelectionMode.Single"/>, or
  /// <see cref="MetroListSelectionMode.Multiple"/>).
  Mode: MetroListSelectionMode
  /// Displays the zoomed-out group jump grid when <c>true</c>; shows the
  /// grouped virtual list when <c>false</c>.
  JumpOpen: bool
  /// Makes group headers interactive buttons that open the jump grid when
  /// <c>true</c>.
  ShowJumpGrid: bool
  /// Uniform virtual row height in logical pixels at text scale 1.0
  /// (defaults to <c>44.0</c>). The built rows scale this base value with
  /// the theme text scale factor.
  RowHeight: float
  /// Optional explicit viewport height.
  Height: Length voption
  /// Optional accessible name overriding <c>"Long list selector"</c>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="MetroLongListSelectorCell"/>.
[<Struct; NoComparison>]
type MetroLongListEvent =
  /// The selected item set changed.
  | SelectionChanged of ids: string array
  /// An item was activated in <see cref="MetroListSelectionMode.Activate"/>
  /// mode.
  | ItemClick of id: string
  /// The user opened or dismissed the group jump grid.
  | JumpOpenChanged of isOpen: bool
  /// The user selected a group tile in the jump grid.
  | GroupJumped of groupKey: string

/// Stateful Metro grouped virtual list with an interactive group jump grid.
/// Owns its <see cref="Virtual"/> scroll viewport and <see cref="ElementHandle"/>
/// so selecting a group tile scrolls directly to that group header.
/// <c>MetroLongListSelector.create</c> makes the cell.
[<AbstractClass>]
type MetroLongListSelectorCell =
  inherit Cell<MetroLongListInput>

  /// Handle of the grouped virtual list viewport.
  abstract ScrollHandle: ElementHandle

  /// Paint-bound scale animation of the active view. A test reads it.
  abstract ScaleAnim: Anim<float>

  /// Paint-bound opacity animation of the active view. A test reads it.
  abstract OpacityAnim: Anim<float>

  /// <c>true</c> when the jump grid is currently shown. A test reads it.
  abstract IsJumpOpen: bool

  /// The tree of the current state. Call inside a mounted cell <c>Build</c>,
  /// where the Metro theme and virtual items context are active.
  abstract View: Blob

  /// Drives one event through the same path as pointer and keyboard actions. A
  /// test calls it to verify host rebuilds without reflection.
  abstract Handle: event: MetroLongListEvent -> unit

  /// Gives one event to the host cell through <c>InvokeInputCallback</c>, so
  /// the owner rule rebuilds the host cell without an explicit <c>Rebuild</c>.
  member Raise:
    callback: Action<MetroLongListEvent> * value: MetroLongListEvent -> unit

/// Builders and cell constructors for <see cref="MetroLongListSelectorCell"/>.
[<RequireQualifiedAccess>]
module MetroLongListSelector =

  /// Default uniform row height (<c>44.0</c> logical pixels).
  val defaultRowHeightPx: float

  /// Creates default <see cref="MetroLongListInput"/> for
  /// <paramref name="groups"/> in single-selection mode with jump-grid support
  /// enabled.
  val defaults: groups: MetroLongListGroup array -> MetroLongListInput

  /// Purely flattens non-empty <see cref="MetroLongListGroup"/> entries into
  /// header and item virtual rows.
  val flattenRows: input: MetroLongListInput -> MetroLongListRow array

  /// Builds the group jump grid <see cref="Blob"/> for <paramref name="input"/>,
  /// invoking <paramref name="onSelectGroup"/> when an active group tile is
  /// clicked and <paramref name="onCancel"/> on Escape. Call inside a build
  /// where the Metro theme is readable.
  val buildJumpGrid:
    input: MetroLongListInput *
    onSelectGroup: (string -> unit) *
    onCancel: (unit -> unit) ->
      Blob

  /// Creates a <see cref="MetroLongListSelectorCell"/> delivering events to
  /// <paramref name="onEvent"/> through <c>InvokeInputCallback</c>.
  val create: onEvent: (MetroLongListEvent -> unit) -> MetroLongListSelectorCell

  /// Creates a reusable cell factory for <see cref="MetroLongListSelectorCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onEvent: (MetroLongListEvent -> unit) -> (unit -> MetroLongListSelectorCell)

  /// Mounts a <see cref="MetroLongListSelectorCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroLongListInput *
    factory: (unit -> MetroLongListSelectorCell) ->
      Blob
