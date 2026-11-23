namespace FunGoo.Metro

open Goo

/// Data-only input snapshot for <see cref="MetroSemanticZoomCell"/>.
[<Struct; NoComparison>]
type MetroSemanticZoomInput = {
  /// Shows the zoomed-out overview when <c>true</c>, and the zoomed-in detail
  /// when <c>false</c>.
  IsZoomedOut: bool
  /// Shows the zoom toggle button in the top end corner.
  ShowToggleButton: bool
  /// Optional accessible name of the semantic zoom region.
  AccessibilityName: string voption
}

/// Events raised by a tree from <see cref="MetroSemanticZoom.buildView"/>.
[<Struct; NoComparison>]
type MetroSemanticZoomEvent =
  /// Requests the zoomed-out overview.
  | ZoomOut
  /// Requests the zoomed-in detail.
  | ZoomIn

/// Explicit state of a Metro semantic zoom tree.
/// <see cref="MetroSemanticZoomCell.State"/> supplies the current state of a
/// mounted cell.
[<Struct; NoComparison>]
type MetroSemanticZoomState = {
  /// Input snapshot that the tree shows.
  Input: MetroSemanticZoomInput
  /// Paint-bound scale of the active view during a zoom transition.
  ScaleAnim: Anim<float>
  /// Paint-bound opacity of the active view during a zoom transition.
  OpacityAnim: Anim<float>
}

/// Stateful Metro SemanticZoom cell: a zoomed-in detail view and a zoomed-out
/// overview view of the same content, with a zoom toggle button and a short
/// scale and fade transition. Only the active view mounts.
[<Sealed>]
type MetroSemanticZoomCell =
  inherit Cell<MetroSemanticZoomInput>

  /// Creates a semantic zoom cell. The cell raises <paramref name="onEvent"/>
  /// for zoom requests. <paramref name="render"/> builds the active view:
  /// <c>true</c> selects the zoomed-out overview, <c>false</c> the zoomed-in
  /// detail.
  new:
    onEvent: (MetroSemanticZoomEvent -> unit) * render: (bool -> Blob) ->
      MetroSemanticZoomCell

  /// Paint-bound scale of the active view during a zoom transition.
  member ScaleAnim: Anim<float>

  /// Paint-bound opacity of the active view during a zoom transition.
  member OpacityAnim: Anim<float>

  /// <c>true</c> when the active view is the zoomed-out overview.
  member IsZoomedOut: bool

  /// Current state of the cell, for <see cref="MetroSemanticZoom.buildView"/>.
  member State: MetroSemanticZoomState

  /// Builds the semantic zoom tree for <see cref="State"/>. Call it inside a
  /// <c>Cell.Build</c>, because it reads the active Metro theme.
  member View: Blob

  /// Event handler of the view tree.
  member Handle: event: MetroSemanticZoomEvent -> unit

  /// Starts the zoom transition when the view changes, or snaps into place when
  /// reduced motion is active.
  override InputChanged: input: MetroSemanticZoomInput -> unit

  /// Resets the transition on unmount.
  override Unmounting: unit -> unit

  /// Builds the semantic zoom through <see cref="MetroSemanticZoom.buildView"/>.
  override Build: input: MetroSemanticZoomInput -> Blob

/// Metro SemanticZoom tree builder and construction helpers.
[<RequireQualifiedAccess>]
module MetroSemanticZoom =

  /// Creates <see cref="MetroSemanticZoomInput"/> for <paramref name="isZoomedOut"/>
  /// with the zoom toggle button shown.
  val defaults: isZoomedOut: bool -> MetroSemanticZoomInput

  /// Builds the semantic zoom tree for the explicit <paramref name="state"/>.
  /// <paramref name="render"/> builds the active view. Raises
  /// <paramref name="onEvent"/> for zoom requests.
  val buildView:
    state: MetroSemanticZoomState *
    render: (bool -> Blob) *
    onEvent: (MetroSemanticZoomEvent -> unit) ->
      Blob

  /// Creates a reusable cell factory for <see cref="MetroSemanticZoomCell"/>.
  /// Create the factory once, outside <c>Build</c>.
  val createFactory:
    onEvent: (MetroSemanticZoomEvent -> unit) * render: (bool -> Blob) ->
      unit ->
        MetroSemanticZoomCell

  /// Mounts a <see cref="MetroSemanticZoomCell"/> under the stable
  /// <paramref name="key"/> with <paramref name="input"/>.
  val mount:
    key: string *
    input: MetroSemanticZoomInput *
    factory: (unit -> MetroSemanticZoomCell) ->
      Blob
