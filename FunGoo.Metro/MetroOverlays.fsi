namespace FunGoo.Metro

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

/// Right-docked Metro settings flyout builder backed by <see cref="Portal"/>
/// and a modal focus scope.
[<RequireQualifiedAccess>]
module MetroSettingsFlyout =

  /// Creates default <see cref="MetroSettingsFlyoutProps"/> with a 346 px panel
  /// width.
  val defaults: title: string * isOpen: bool -> MetroSettingsFlyoutProps

  /// Builds a right-docked Metro settings flyout <see cref="Blob"/>, returning
  /// a hidden container when closed.
  val build:
    props: MetroSettingsFlyoutProps *
    onDismiss: (unit -> unit) *
    renderContent: (unit -> Blob) ->
      Blob

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

/// Full-width Metro content dialog builder backed by <see cref="Portal"/> and a
/// modal focus scope.
[<RequireQualifiedAccess>]
module MetroContentDialog =

  /// Creates default <see cref="MetroContentDialogProps"/> with an <c>"OK"</c>
  /// primary button and dismissal enabled.
  val defaults: title: string * isOpen: bool -> MetroContentDialogProps

  /// Builds a full-width Metro modal dialog <see cref="Blob"/>, returning a
  /// hidden container when closed.
  val build:
    props: MetroContentDialogProps *
    onPrimary: (unit -> unit) *
    onSecondary: (unit -> unit) *
    onDismiss: (unit -> unit) *
    renderContent: (unit -> Blob) ->
      Blob
