namespace FunGoo.Metro

open System
open Goo

/// Descriptor for a command button inside a <see cref="MetroAppBar"/>.
[<Struct; NoComparison>]
type MetroAppBarCommand = {
  /// Stable unique command identifier passed to the command handler.
  Id: string
  /// Visible caption label rendered under or beside the icon.
  Label: string
  /// Material symbol icon name rendered inside the circular button glyph.
  IconName: string
  /// Disables pointer and keyboard activation when <c>true</c>.
  Disabled: bool
}

/// Data-only properties for constructing a bottom-aligned Metro application
/// bar.
[<Struct; NoComparison>]
type MetroAppBarProps = {
  /// Primary icon commands displayed horizontally in the bar.
  Primary: MetroAppBarCommand array
  /// Secondary overflow commands revealed when <see cref="Expanded"/> is
  /// <c>true</c>.
  Secondary: MetroAppBarCommand array
  /// Shows the expandable secondary command drawer when <c>true</c>.
  Expanded: bool
  /// Optional accessible name for the toolbar landmark.
  AccessibilityName: string voption
}

/// Bottom-aligned Metro application bar with circular 40 px Material icon
/// commands, caption labels, and an expandable secondary command list.
[<RequireQualifiedAccess>]
module MetroAppBar =

  /// Creates default <see cref="MetroAppBarProps"/> for
  /// <paramref name="primary"/> with no secondary commands and collapsed state.
  val defaults: primary: MetroAppBarCommand array -> MetroAppBarProps

  /// Builds a Metro application bar <see cref="Blob"/> invoking
  /// <paramref name="onCommand"/> with command IDs and
  /// <paramref name="onToggleExpanded"/> when the ellipsis toggle is activated.
  val build:
    props: MetroAppBarProps *
    onCommand: (string -> unit) *
    onToggleExpanded: (unit -> unit) ->
      Blob

/// Data-only properties for a right-docked Metro settings flyout presented
/// through a modal <see cref="Portal"/>.
[<Struct; NoComparison>]
type MetroSettingsFlyoutProps = {
  /// Displays the settings flyout portal when <c>true</c>; collapses when
  /// <c>false</c>.
  Open: bool
  /// Title displayed inside the accent header band beside the back button.
  Title: string
  /// Flyout panel width in logical pixels (defaults to <c>346.0</c>).
  Width: float
  /// Optional accessible name overriding <see cref="Title"/>.
  AccessibilityName: string voption
}

/// Stateful right-docked Metro settings flyout cell that animates the panel
/// slide and backdrop fade on open and close over 333 ms, honoring reduced
/// motion. <c>MetroSettingsFlyout.create</c> makes the cell.
[<AbstractClass>]
type MetroSettingsFlyoutCell =
  inherit Cell<MetroSettingsFlyoutProps>

  /// <c>true</c> while the flyout portal is mounted (open or running its close
  /// animation). A test reads it.
  abstract IsVisible: bool

  /// <c>true</c> while the open or close transition is running. A test reads
  /// it.
  abstract IsAnimating: bool

  /// Paint-bound horizontal slide animation of the flyout panel. A test reads
  /// it.
  abstract PanelTranslate: Anim<Point>

  /// Paint-bound opacity animation of the modal backdrop. A test reads it.
  abstract BackdropOpacity: Anim<float>

  /// The tree of the current state. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

  /// Gives one dismiss request to the host cell, as a Goo.Widgets control
  /// does. The owner rule then rebuilds the host cell, so a host does not call
  /// <c>Rebuild</c> on itself in the dismiss callback.
  member Raise: callback: Action -> unit

/// Right-docked Metro settings flyout builders and stateful animated cell
/// constructors backed by <see cref="Portal"/> and a modal focus scope.
[<RequireQualifiedAccess>]
module MetroSettingsFlyout =

  /// Creates default <see cref="MetroSettingsFlyoutProps"/> with a 346 px panel
  /// width.
  val defaults: title: string * isOpen: bool -> MetroSettingsFlyoutProps

  /// Builds a stateless right-docked Metro settings flyout <see cref="Blob"/>,
  /// returning a hidden container when closed.
  val build:
    props: MetroSettingsFlyoutProps *
    onDismiss: (unit -> unit) *
    renderContent: (unit -> Blob) ->
      Blob

  /// Creates a stateful animated <see cref="MetroSettingsFlyoutCell"/>. The
  /// cell delivers <paramref name="onDismiss"/> through
  /// <c>InvokeInputCallback</c>, so the owner rule rebuilds the host cell.
  /// Callbacks inside <paramref name="renderContent"/> belong to this cell;
  /// when they mutate host state, the host must rebuild itself.
  val create:
    onDismiss: (unit -> unit) * renderContent: (unit -> Blob) ->
      MetroSettingsFlyoutCell

  /// Creates a reusable cell factory for <see cref="MetroSettingsFlyoutCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onDismiss: (unit -> unit) * renderContent: (unit -> Blob) ->
      unit ->
        MetroSettingsFlyoutCell

  /// Mounts a <see cref="MetroSettingsFlyoutCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroSettingsFlyoutProps *
    factory: (unit -> MetroSettingsFlyoutCell) ->
      Blob

/// User action emitted by <see cref="MetroContentDialogCell"/>.
[<Struct; NoComparison>]
type MetroContentDialogAction =
  /// The user activated the primary action button.
  | Primary
  /// The user activated the secondary action button.
  | Secondary
  /// The user dismissed the dialog via Escape or backdrop click.
  | Dismiss

/// Data-only properties for a full-width Metro modal band dialog.
[<Struct; NoComparison>]
type MetroContentDialogProps = {
  /// Displays the dialog portal when <c>true</c>; collapses when <c>false</c>.
  Open: bool
  /// Dialog heading rendered with <see cref="MetroTextRole.Header"/>.
  Title: string
  /// Optional label for the primary accent action button.
  PrimaryText: string voption
  /// Optional label for the secondary standard action button.
  SecondaryText: string voption
  /// Allows dismissing via scrim click or Escape when <c>true</c>.
  Dismissible: bool
  /// Optional accessible name overriding <see cref="Title"/>.
  AccessibilityName: string voption
}

/// Stateful full-width Metro content dialog cell that animates open and close
/// transitions over 280 ms using the compiled 3D perspective swivel shader
/// when embedded, or a 2D scale, slide, and fade fallback automatically.
/// <c>MetroContentDialog.create</c> makes the cell.
[<AbstractClass>]
type MetroContentDialogCell =
  inherit Cell<MetroContentDialogProps>

  /// <c>true</c> while the dialog portal is mounted (open or running its close
  /// animation). A test reads it.
  abstract IsVisible: bool

  /// <c>true</c> while the open or close transition is running. A test reads
  /// it.
  abstract IsAnimating: bool

  /// Active transition rendering path (<see cref="MetroTiltMode.Perspective3D"/>
  /// when the perspective shader is available; otherwise
  /// <see cref="MetroTiltMode.Fallback2D"/>).
  abstract Mode: MetroTiltMode

  /// <c>true</c> while a 3D <see cref="ShaderEffect"/> is currently attached
  /// to the dialog band. A test reads it.
  abstract IsShaderAttached: bool

  /// Normalized transition progress animation (<c>0.0</c> to <c>1.0</c>). A
  /// test reads it.
  abstract ProgressAnim: Anim<float>

  /// Paint-bound opacity animation of the modal backdrop. A test reads it.
  abstract BackdropOpacity: Anim<float>

  /// The tree of the current state. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

  /// Runs one dialog action through the same callback path as the buttons and
  /// backdrop. A test calls it to verify host rebuilds without reflection.
  abstract Handle: action: MetroContentDialogAction -> unit

  /// Gives one dialog action to the host cell, as a Goo.Widgets control does.
  /// The owner rule then rebuilds the host cell, so a host does not call
  /// <c>Rebuild</c> on itself in the action callback.
  member Raise:
    callback: Action<MetroContentDialogAction> *
    action: MetroContentDialogAction ->
      unit

/// Data-only callbacks and content for <see cref="MetroContentDialog.build"/>:
/// the three dialog actions plus the body renderer. One record keeps the four
/// behavior parameters named at the call site.
[<Struct; NoComparison>]
type MetroContentDialogHandlers = {
  /// Invoked when the user activates the primary action button.
  OnPrimary: unit -> unit
  /// Invoked when the user activates the secondary action button.
  OnSecondary: unit -> unit
  /// Invoked when the user dismisses the dialog via Escape or backdrop click.
  OnDismiss: unit -> unit
  /// Renders the dialog body content below the header.
  RenderContent: unit -> Blob
}

/// Full-width Metro content dialog builders and stateful animated cell
/// constructors backed by <see cref="Portal"/> and a modal focus scope.
[<RequireQualifiedAccess>]
module MetroContentDialog =

  /// Creates default <see cref="MetroContentDialogProps"/> with an <c>"OK"</c>
  /// primary button and dismissal enabled.
  val defaults: title: string * isOpen: bool -> MetroContentDialogProps

  /// Pre-cached 280 ms Metro decelerate simulation factory for dialog swivel
  /// open and close transitions.
  val transitionSpec: Func<float, float, float, Simulation>

  /// Builds a stateless full-width Metro modal dialog <see cref="Blob"/> for
  /// <paramref name="props"/> and the callbacks and content in
  /// <paramref name="handlers"/>, returning a hidden container when closed.
  val build:
    props: MetroContentDialogProps * handlers: MetroContentDialogHandlers ->
      Blob

  /// Creates a stateful animated <see cref="MetroContentDialogCell"/> that
  /// uses the embedded 3D perspective shader when available, or falls back to
  /// 2D motion automatically. The cell delivers <paramref name="onAction"/>
  /// through <c>InvokeInputCallback</c>, so the owner rule rebuilds the host
  /// cell.
  val create:
    onAction: (MetroContentDialogAction -> unit) * renderContent: (unit -> Blob) ->
      MetroContentDialogCell

  /// Creates a <see cref="MetroContentDialogCell"/> with an explicit optional
  /// <see cref="ShaderEffect"/>. Tests use this to exercise both 3D and 2D
  /// transition paths.
  val createWithEffect:
    effect: ShaderEffect voption *
    onAction: (MetroContentDialogAction -> unit) *
    renderContent: (unit -> Blob) ->
      MetroContentDialogCell

  /// Creates a reusable cell factory for <see cref="MetroContentDialogCell"/>.
  /// Cache the returned factory outside <c>Build</c>.
  val createFactory:
    onAction: (MetroContentDialogAction -> unit) * renderContent: (unit -> Blob) ->
      unit ->
        MetroContentDialogCell

  /// Mounts a <see cref="MetroContentDialogCell"/> under stable
  /// <paramref name="key"/> using the cached <paramref name="factory"/>.
  val mount:
    key: string *
    input: MetroContentDialogProps *
    factory: (unit -> MetroContentDialogCell) ->
      Blob
