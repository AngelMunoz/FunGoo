namespace FunGoo.Metro

open Goo

/// Descriptor for one item in a <see cref="MetroFlipViewCell"/>.
[<Struct; NoComparison>]
type MetroFlipViewItem = {
  /// Stable unique identifier for the item.
  Id: string
  /// Accessible name announced for the item, for example a short title.
  AccessibilityName: string
}

/// Data-only input snapshot for <see cref="MetroFlipViewCell"/>.
[<Struct; NoComparison>]
type MetroFlipViewInput = {
  /// Ordered items shown one at a time.
  Items: MetroFlipViewItem array
  /// Identifier of the active item. An unknown identifier activates the first item.
  SelectedId: string
  /// Shows the previous and next edge buttons when the view holds two or more items.
  ShowNavigation: bool
  /// Shows the bottom indicator dots when the view holds two or more items.
  ShowIndicators: bool
  /// Optional accessible name of the flip view.
  AccessibilityName: string voption
}

/// Events raised by a tree from <see cref="MetroFlipView.buildView"/>.
[<Struct; NoComparison>]
type MetroFlipViewEvent =
  /// Requests a move to the item at the zero-based <paramref name="index"/>.
  | Navigate of index: int
  /// A pointer went down on the content area.
  | PointerDown of pointer: PointerEvent
  /// A pointer moved over the content area.
  | PointerMoved of pointer: PointerEvent
  /// A pointer went up over the content area.
  | PointerUp of pointer: PointerEvent
  /// The platform canceled the pointer gesture on the content area.
  | PointerCanceled of pointer: PointerEvent

/// Explicit state of a Metro flip view tree. <see cref="MetroFlipViewCell.State"/>
/// supplies the current state of a mounted cell.
[<Struct; NoComparison>]
type MetroFlipViewState = {
  /// Input snapshot that the tree shows.
  Input: MetroFlipViewInput
  /// Index of the active item, or <c>-1</c> when there are no items.
  ActiveIndex: int
  /// Identifier of the item that slides out. <c>ValueNone</c> when no slide runs.
  OutgoingId: string voption
  /// Slide direction: <c>1.0</c> moves the content to the left for the next item,
  /// <c>-1.0</c> moves the content to the right for the previous item.
  Direction: float
  /// Paint-bound translation shared by both content panels.
  TrackTranslate: Anim<Point>
  /// Handle of the content viewport, used to measure the slide distance.
  ViewportHandle: ElementHandle
}

/// Stateful Metro FlipView cell. The FlipView shows one item at a time with a
/// sliding transition, edge navigation buttons, indicator dots, a horizontal
/// swipe, and keyboard navigation. Only the active item mounts, plus the
/// outgoing item while a slide runs, so a <c>Cell.Mount</c> factory inside
/// <c>renderItem</c> must return a fresh <c>Cell</c>.
[<Sealed>]
type MetroFlipViewCell =
  inherit Cell<MetroFlipViewInput>

  /// Creates a flip view cell. Each navigation request invokes
  /// <paramref name="onSelect"/> with the identifier of the requested item.
  /// <paramref name="renderItem"/> builds the content of one item.
  /// The cell calls <paramref name="renderItem"/> inside its own
  /// <c>Build</c>. A callback that <paramref name="renderItem"/> creates
  /// belongs to this cell, so the owner rule rebuilds this cell and not the
  /// caller. Such a callback must rebuild the caller cell when it changes the
  /// state of the caller cell.
  new:
    onSelect: (string -> unit) * renderItem: (string -> Blob) ->
      MetroFlipViewCell

  /// Paint-bound translation shared by both content panels during a slide.
  member TrackTranslate: Anim<Point>

  /// Identifier of the item that slides out. <c>ValueNone</c> when no slide runs.
  member OutgoingId: string voption

  /// <c>true</c> from the start of a slide until its timer completes.
  member IsSliding: bool

  /// Current state of the cell, for <see cref="MetroFlipView.buildView"/>.
  member State: MetroFlipViewState

  /// Builds the flip view tree for <see cref="State"/>. Call it inside a
  /// <c>Cell.Build</c>, because it reads the active Metro theme.
  member View: Blob

  /// Event handler of the view tree. A navigation request invokes <c>onSelect</c>.
  member Handle: event: MetroFlipViewEvent -> unit

  /// Starts the slide when the active item changes, or snaps into place when
  /// reduced motion is active or the host window is not open.
  override InputChanged: input: MetroFlipViewInput -> unit

  /// Cancels the slide timer and the pointer gesture on unmount.
  override Unmounting: unit -> unit

  /// Builds the flip view through <see cref="MetroFlipView.buildView"/>.
  override Build: input: MetroFlipViewInput -> Blob

/// Metro FlipView tree builder and construction helpers.
[<RequireQualifiedAccess>]
module MetroFlipView =

  /// Creates <see cref="MetroFlipViewInput"/> for <paramref name="items"/> and
  /// <paramref name="selectedId"/> with navigation buttons and indicator dots.
  val defaults:
    items: MetroFlipViewItem array * selectedId: string -> MetroFlipViewInput

  /// Returns the index of the item with identifier <paramref name="selectedId"/>
  /// in <paramref name="items"/>. Returns <c>0</c> for an unknown identifier and
  /// <c>-1</c> when there are no items.
  val indexOf: items: MetroFlipViewItem array * selectedId: string -> int

  /// Horizontal pointer travel in logical pixels that starts a flip.
  val swipeThresholdPx: float

  /// Builds the flip view tree for the explicit <paramref name="state"/>. Both
  /// content panels bind <c>TrackTranslate</c>, so a slide runs on paint without
  /// cell rebuilds. Raises <paramref name="onEvent"/> for navigation requests and
  /// for pointer gestures on the content area.
  val buildView:
    state: MetroFlipViewState *
    renderItem: (string -> Blob) *
    onEvent: (MetroFlipViewEvent -> unit) ->
      Blob

  /// Creates a reusable cell factory for <see cref="MetroFlipViewCell"/>. Create
  /// the factory once, outside <c>Build</c>.
  val createFactory:
    onSelect: (string -> unit) * renderItem: (string -> Blob) ->
      unit ->
        MetroFlipViewCell

  /// Mounts a <see cref="MetroFlipViewCell"/> under the stable
  /// <paramref name="key"/> with <paramref name="input"/>.
  val mount:
    key: string *
    input: MetroFlipViewInput *
    factory: (unit -> MetroFlipViewCell) ->
      Blob
