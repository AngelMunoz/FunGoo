namespace FunGoo.Metro

open System
open Goo

/// Preferred placement of an anchored <see cref="MetroFlyoutCell"/> or
/// <see cref="MetroTooltipCell"/> relative to its target handle.
[<Struct>]
type MetroFlyoutPlacement =
  /// Above the target, centered horizontally.
  | Top
  /// Above the target, aligned to its start edge.
  | TopStart
  /// Above the target, aligned to its end edge.
  | TopEnd
  /// Below the target, centered horizontally.
  | Bottom
  /// Below the target, aligned to its start edge.
  | BottomStart
  /// Below the target, aligned to its end edge.
  | BottomEnd
  /// To the left of the target.
  | Left
  /// To the right of the target.
  | Right

/// Data-only properties for an anchored Metro popup flyout.
[<Struct; NoComparison>]
type MetroFlyoutProps = {
  /// Displays the flyout when <c>true</c>.
  Open: bool
  /// Handle of the anchor element to position the flyout beside.
  Anchor: ElementHandle
  /// Preferred placement relative to <see cref="Anchor"/>
  /// (<see cref="MetroFlyoutPlacement.BottomStart"/> by default).
  Placement: MetroFlyoutPlacement
  /// Minimum panel width in logical pixels (defaults to <c>200.0</c>).
  MinWidth: float
  /// Optional maximum panel width in logical pixels.
  MaxWidth: float voption
  /// Dismisses on outside click or Escape when <c>true</c> (defaults to
  /// <c>true</c>).
  LightDismiss: bool
  /// Optional accessible name overriding <c>"Flyout"</c>.
  AccessibilityName: string voption
}

/// Stateful anchored Metro flyout cell that animates slide and fade over 250 ms
/// on open and close, honoring reduced motion.
/// <c>MetroFlyout.create</c> makes the cell.
[<AbstractClass>]
type MetroFlyoutCell =
  inherit Cell<MetroFlyoutProps>

  /// <c>true</c> when the input asks for the flyout to be open.
  abstract IsOpen: bool

  /// <c>true</c> while the flyout portal is mounted (open or running its close
  /// animation).
  abstract IsVisible: bool

  /// <c>true</c> while the open or close transition is running.
  abstract IsAnimating: bool

  /// The tree of the current state. Call inside a build where the Metro theme
  /// is readable.
  abstract View: Blob

  /// Triggers dismissal through the same callback path as Escape and outside
  /// clicks. A test calls it to verify host rebuilds without reflection.
  abstract Dismiss: unit -> unit

  /// Gives one dismiss request to the host cell through
  /// <c>InvokeInputCallback</c>, so the owner rule rebuilds the host cell.
  member Raise: callback: Action -> unit

/// Stateless builders and stateful animated cell constructors for anchored
/// Metro flyouts.
[<RequireQualifiedAccess>]
module MetroFlyout =

  /// Maps <paramref name="placement"/> onto a core <see cref="PortalPlacement"/>.
  val toPortalPlacement: placement: MetroFlyoutPlacement -> PortalPlacement

  /// Creates default <see cref="MetroFlyoutProps"/> anchored to
  /// <paramref name="anchor"/> with <see cref="MetroFlyoutPlacement.BottomStart"/>.
  val defaults: anchor: ElementHandle * isOpen: bool -> MetroFlyoutProps

  /// Builds a stateless anchored Metro flyout <see cref="Blob"/>.
  val build:
    props: MetroFlyoutProps *
    onDismiss: (unit -> unit) *
    renderContent: (unit -> Blob) ->
      Blob

  /// Creates an animated <see cref="MetroFlyoutCell"/>. Delivers
  /// <paramref name="onDismiss"/> through <c>InvokeInputCallback</c>. Callbacks
  /// inside <paramref name="renderContent"/> belong to this cell; when they
  /// mutate host state, the host must rebuild itself.
  val create:
    onDismiss: (unit -> unit) * renderContent: (unit -> Blob) -> MetroFlyoutCell

  /// Creates a reusable cell factory for <see cref="MetroFlyoutCell"/>. Cache
  /// the returned factory outside <c>Build</c>.
  val createFactory:
    onDismiss: (unit -> unit) * renderContent: (unit -> Blob) ->
      unit ->
        MetroFlyoutCell

  /// Mounts a <see cref="MetroFlyoutCell"/> under stable <paramref name="key"/>
  /// using the cached <paramref name="factory"/>.
  val mount:
    key: string * input: MetroFlyoutProps * factory: (unit -> MetroFlyoutCell) ->
      Blob
