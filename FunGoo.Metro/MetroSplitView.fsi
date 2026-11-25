namespace FunGoo.Metro

open Goo

/// Presentation mode of the side pane in a <see cref="MetroSplitViewCell"/>.
[<Struct>]
type MetroSplitViewMode =
  /// Pane floats above the main content with a dim backdrop. Known limit: the
  /// slide takes its direction from the Goo.Widgets theme direction. An
  /// ancestor container with <c>Direction = RightToLeft</c> moves the pane to
  /// the other edge, but the slide keeps its direction.
  | Overlay
  /// Pane sits inline beside the main content, pushing it when open.
  | Inline
  /// Pane stays visible at <c>CompactPaneLength</c> when closed and expands to
  /// <c>OpenPaneLength</c> when open. The pane shows the first
  /// <c>CompactPaneLength</c> pixels of the pane content and clips the rest.
  /// The host selects the content that fits a compact pane.
  | Compact

/// Logical inline edge where the split-view pane is placed. Automatically
/// flips in right-to-left layouts.
[<Struct>]
type MetroSplitViewPlacement =
  | Start
  | End

/// Data-only input of <see cref="MetroSplitViewCell"/>.
[<Struct; NoComparison>]
type MetroSplitViewInput = {
  /// Whether the pane is expanded/open.
  Open: bool
  /// Display mode of the pane.
  DisplayMode: MetroSplitViewMode
  /// Logical edge hosting the pane.
  PanePlacement: MetroSplitViewPlacement
  /// Expanded pane width in logical pixels (defaults to <c>280.0</c>).
  OpenPaneLength: float
  /// Collapsed pane width in <see cref="MetroSplitViewMode.Compact"/> mode
  /// (defaults to <c>48.0</c>).
  CompactPaneLength: float
  /// When <c>true</c>, clicking the backdrop or pressing Escape dismisses an
  /// open <see cref="MetroSplitViewMode.Overlay"/> pane.
  LightDismissOverlay: bool
  /// Optional accessible name for the navigation pane.
  AccessibilityName: string voption
}

/// Retained Metro split-view container supporting <c>Overlay</c>,
/// <c>Inline</c>, and <c>Compact</c> pane modes with logical RTL-aware
/// placement and <c>333 ms</c> Metro transitions.
[<AbstractClass>]
type MetroSplitViewCell =
  inherit Cell<MetroSplitViewInput>

  internal new: unit -> MetroSplitViewCell

  /// <c>true</c> while the overlay pane or its close animation is visible.
  abstract IsOverlayVisible: bool

  /// Handle attached to the pane container.
  abstract PaneHandle: ElementHandle

  /// Slide animation of the pane in <see cref="MetroSplitViewMode.Overlay"/>.
  abstract PaneTranslate: Anim<Point>

  /// Opacity animation of the scrim in <see cref="MetroSplitViewMode.Overlay"/>.
  abstract BackdropOpacity: Anim<float>

  /// Dismisses an open overlay pane through the host callback.
  abstract DismissOverlay: unit -> unit

/// Metro split-view builder and retained cell factory.
[<RequireQualifiedAccess>]
module MetroSplitView =

  /// Returns default split-view input in <see cref="MetroSplitViewMode.Overlay"/>
  /// mode placed at <see cref="MetroSplitViewPlacement.Start"/>.
  val defaults: isOpen: bool -> MetroSplitViewInput

  /// Creates a <see cref="MetroSplitViewCell"/> whose dismiss callbacks report
  /// to the host through <c>InvokeInputCallback</c>. The cell calls
  /// <paramref name="renderPane"/> and <paramref name="renderContent"/> inside
  /// its own <c>Build</c>. A Blob callback that either function creates
  /// belongs to this cell, so the owner rule rebuilds this cell and not the
  /// host. When such a callback changes the state of the host, it must also
  /// rebuild the host.
  val create:
    onOpenChange: (bool -> unit) *
    renderPane: (unit -> Blob) *
    renderContent: (unit -> Blob) ->
      MetroSplitViewCell

  /// Creates a reusable mount factory for <see cref="MetroSplitViewCell"/>.
  /// The cell calls <paramref name="renderPane"/> and
  /// <paramref name="renderContent"/> inside its own <c>Build</c>. A Blob
  /// callback that either function creates belongs to this cell, so the owner
  /// rule rebuilds this cell and not the host. When such a callback changes
  /// the state of the host, it must also rebuild the host.
  val createFactory:
    onOpenChange: (bool -> unit) *
    renderPane: (unit -> Blob) *
    renderContent: (unit -> Blob) ->
      (unit -> MetroSplitViewCell)

  /// Mounts a <see cref="MetroSplitViewCell"/> under <paramref name="key"/>.
  val mount:
    key: string *
    input: MetroSplitViewInput *
    factory: (unit -> MetroSplitViewCell) ->
      Blob
