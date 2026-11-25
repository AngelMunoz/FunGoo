namespace FunGoo.Metro

open System
open Goo

/// Descriptor for a selectable item in a <see cref="MetroListPickerCell"/>.
[<Struct; NoComparison>]
type MetroListPickerItem = {
  /// Stable unique item identifier.
  Id: string
  /// Visible item label rendered in the trigger and picker rows.
  Label: string
  /// Optional Material symbol icon name rendered before the label.
  IconName: string voption
  /// Disables selection of this item when <c>true</c>.
  Disabled: bool
}

/// Expansion presentation mode for <see cref="MetroListPickerCell"/>.
[<Struct>]
type MetroListPickerMode =
  /// Expands inline when <c>Items.Length &lt;= 5</c>; opens a full modal sheet
  /// when <c>Items.Length &gt; 5</c>.
  | Auto
  /// Always expands the option list inline inside the layout flow.
  | Inline
  /// Always opens a full-window modal sheet via <see cref="Portal"/>. The
  /// Overlay closes immediately, with no close animation.
  | Overlay

/// Data-only input snapshot for <see cref="MetroListPickerCell"/>.
[<Struct; NoComparison>]
type MetroListPickerInput = {
  /// Selectable options presented in the picker.
  Items: MetroListPickerItem array
  /// Currently selected item identifier (or <c>""</c> when unselected). A
  /// selected option gets a full accent fill with text in Accent.OnAccent,
  /// where the Metro lists use a 3 px accent bar and a 30 percent tint.
  SelectedId: string
  /// Expands the inline list or modal overlay when <c>true</c>.
  Open: bool
  /// Expansion mode (<see cref="MetroListPickerMode.Auto"/> by default).
  Mode: MetroListPickerMode
  /// Optional field label rendered above the trigger button.
  Header: string voption
  /// Optional heading shown at the top of the modal overlay sheet.
  Title: string voption
  /// Optional placeholder text shown when no item matches <see cref="SelectedId"/>.
  Placeholder: string voption
  /// Disables opening and keyboard activation when <c>true</c>.
  Disabled: bool
  /// Optional explicit control width.
  Width: Length voption
  /// Optional accessible name overriding <see cref="Header"/> or <see cref="Title"/>.
  AccessibilityName: string voption
}

/// User interaction emitted by <see cref="MetroListPickerCell"/>.
[<Struct; NoComparison>]
type MetroListPickerEvent =
  /// An item was chosen.
  | Selected of id: string
  /// The open state of the picker changed.
  | OpenChanged of isOpen: bool

/// Explicit state snapshot of <see cref="MetroListPickerCell"/>.
[<Struct; NoComparison>]
type MetroListPickerState = {
  /// Latest input snapshot supplied to the cell.
  Input: MetroListPickerInput
  /// Effective resolved mode (<see cref="MetroListPickerMode.Inline"/> or
  /// <see cref="MetroListPickerMode.Overlay"/>).
  ResolvedMode: MetroListPickerMode
  /// Paint-bound vertical slide animation of the expanded list or overlay sheet.
  PanelTranslate: Anim<Point>
  /// Paint-bound opacity animation of the expanded list or overlay backdrop.
  PanelOpacity: Anim<float>
  /// Handle of the Overlay sheet. Bound when the sheet mounts.
  SheetHandle: ElementHandle
}

/// Stateful Windows Phone / Metro list picker cell supporting inline expansion
/// for short lists and a full modal <see cref="Portal"/> sheet for long lists.
/// <c>MetroListPicker.create</c> makes the cell.
[<AbstractClass>]
type MetroListPickerCell =
  inherit Cell<MetroListPickerInput>

  /// <c>true</c> when the picker is open. A test reads it.
  abstract IsOpen: bool

  /// Effective resolved presentation mode (<c>Inline</c> or <c>Overlay</c>).
  abstract ResolvedMode: MetroListPickerMode

  /// Handle of the Overlay sheet. A test reads it.
  abstract SheetHandle: ElementHandle

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

  /// Drives one event through the same path as pointer and keyboard actions. A
  /// test or a headless check calls it to verify host rebuilds without
  /// reflection.
  abstract Handle: event: MetroListPickerEvent -> unit

  /// Gives one event to the host cell through <c>InvokeInputCallback</c>, so
  /// the owner rule rebuilds the host cell.
  member Raise:
    callback: Action<MetroListPickerEvent> * value: MetroListPickerEvent -> unit

/// Builders and cell constructors for <see cref="MetroListPickerCell"/>.
[<RequireQualifiedAccess>]
module MetroListPicker =

  /// Maximum item count before <see cref="MetroListPickerMode.Auto"/> switches
  /// from inline expansion to the modal overlay sheet (<c>5</c>).
  val inlineThreshold: int

  /// Creates default <see cref="MetroListPickerInput"/> for
  /// <paramref name="items"/> and <paramref name="selectedId"/> in
  /// <see cref="MetroListPickerMode.Auto"/> mode.
  val defaults:
    items: MetroListPickerItem array * selectedId: string ->
      MetroListPickerInput

  /// Resolves <see cref="MetroListPickerMode.Auto"/> to either
  /// <see cref="MetroListPickerMode.Inline"/> or
  /// <see cref="MetroListPickerMode.Overlay"/> based on item count.
  val resolveMode: input: MetroListPickerInput -> MetroListPickerMode

  /// Builds one option row of the Overlay list, the same row that the
  /// virtual list of the Overlay builds. A test reads it.
  val buildOverlayRow:
    item: MetroListPickerItem *
    selectedId: string *
    onEvent: (MetroListPickerEvent -> unit) ->
      Blob

  /// Builds the content of the Overlay portal for
  /// <paramref name="state"/>: the dimmed backdrop with the sheet, the
  /// header, the close button, and the virtual list. The cell adds the
  /// result to its <see cref="Portal"/>. Call it inside a build where the
  /// Metro theme is readable. A test reads it.
  val buildOverlay:
    state: MetroListPickerState * onEvent: (MetroListPickerEvent -> unit) ->
      Blob

  /// Builds the <see cref="Blob"/> tree for an explicit
  /// <see cref="MetroListPickerState"/> and <paramref name="onEvent"/> handler.
  /// Call inside a build where the Metro theme is readable.
  val buildView:
    state: MetroListPickerState * onEvent: (MetroListPickerEvent -> unit) ->
      Blob

  /// Creates a <see cref="MetroListPickerCell"/> delivering events to
  /// <paramref name="onEvent"/> through <c>InvokeInputCallback</c>.
  val create: onEvent: (MetroListPickerEvent -> unit) -> MetroListPickerCell

  /// Creates a reusable cell factory for <see cref="MetroListPickerCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onEvent: (MetroListPickerEvent -> unit) -> (unit -> MetroListPickerCell)

  /// Mounts a <see cref="MetroListPickerCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroListPickerInput *
    factory: (unit -> MetroListPickerCell) ->
      Blob
