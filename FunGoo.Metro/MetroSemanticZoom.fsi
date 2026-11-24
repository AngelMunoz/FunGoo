namespace FunGoo.Metro

open System
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
/// <c>MetroSemanticZoom.create</c> makes the cell. The type holds the surface
/// that hosts and tests use.
[<AbstractClass>]
type MetroSemanticZoomCell =
  inherit Cell<MetroSemanticZoomInput>

  /// Paint-bound scale of the active view during a zoom transition.
  abstract ScaleAnim: Anim<float>

  /// Paint-bound opacity of the active view during a zoom transition.
  abstract OpacityAnim: Anim<float>

  /// <c>true</c> when the active view is the zoomed-out overview.
  abstract IsZoomedOut: bool

  /// Current state of the cell, for <see cref="MetroSemanticZoom.buildView"/>.
  abstract State: MetroSemanticZoomState

  /// Builds the semantic zoom tree for <see cref="MetroSemanticZoomCell.State"/>.
  /// Call it inside a <c>Cell.Build</c>, because it reads the active Metro
  /// theme.
  abstract View: Blob

  /// Event handler of the view tree. A zoom request goes to the host.
  abstract Handle: event: MetroSemanticZoomEvent -> unit

  /// Gives one zoom request of this cell to the host cell, as a Goo.Widgets
  /// control does. The owner rule then rebuilds the host cell, so a host
  /// does not call <c>Rebuild</c> on itself in the event callback.
  member Raise:
    callback: Action<MetroSemanticZoomEvent> * value: MetroSemanticZoomEvent ->
      unit

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

  /// Creates a semantic zoom cell. The cell gives each zoom request to
  /// <paramref name="onEvent"/>, and the owner rule then rebuilds the host
  /// cell. <paramref name="render"/> builds the active view: <c>true</c>
  /// selects the zoomed-out overview, <c>false</c> the zoomed-in detail.
  /// The cell calls <paramref name="render"/> inside its own
  /// <c>Build</c>. A callback that <paramref name="render"/> creates
  /// belongs to this cell, so the owner rule rebuilds this cell and not the
  /// caller. Such a callback must rebuild the caller cell when it changes the
  /// state of the caller cell.
  val create:
    onEvent: (MetroSemanticZoomEvent -> unit) * render: (bool -> Blob) ->
      MetroSemanticZoomCell

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
