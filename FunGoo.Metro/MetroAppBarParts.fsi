namespace FunGoo.Metro

open Goo

/// Data-only properties of a standalone <see cref="MetroAppBarButton"/>.
[<Struct; NoComparison>]
type MetroAppBarButtonProps = {
  /// Visible command label.
  Label: string
  /// Material icon name rendered inside the 2 px circular ring.
  IconName: string
  /// When <c>true</c>, hides the text label below the icon circle.
  Compact: bool
  /// When <c>true</c>, renders as a horizontal overflow menu row.
  MenuItem: bool
  /// When <c>true</c>, disables activation.
  Disabled: bool
  /// Optional accessible name override.
  AccessibilityName: string voption
}

/// Data-only properties of a standalone <see cref="MetroAppBarToggleButton"/>.
[<Struct; NoComparison>]
type MetroAppBarToggleProps = {
  /// Visible command label.
  Label: string
  /// Material icon name rendered inside the 2 px circular ring.
  IconName: string
  /// Controlled checked state. Fills the icon circle with the Metro accent
  /// color when <c>true</c>.
  Checked: bool
  /// When <c>true</c>, hides the text label below the icon circle.
  Compact: bool
  /// When <c>true</c>, renders as a horizontal overflow menu row.
  MenuItem: bool
  /// When <c>true</c>, disables activation.
  Disabled: bool
  /// Optional accessible name override.
  AccessibilityName: string voption
}

/// Composable Metro app-bar command button with a 40 px circular 2 px ring and
/// optional compact or overflow-menu presentation.
[<RequireQualifiedAccess>]
module MetroAppBarButton =

  /// Returns expanded app-bar button properties with a visible label.
  val defaults: label: string * iconName: string -> MetroAppBarButtonProps

  /// Returns compact app-bar button properties (icon ring only).
  val compact: label: string * iconName: string -> MetroAppBarButtonProps

  /// Builds the app-bar button inside a <c>Cell.Build</c> scope.
  val build: props: MetroAppBarButtonProps * onClick: (unit -> unit) -> Blob

/// Composable Metro app-bar toggle button with accent circle fill when checked.
[<RequireQualifiedAccess>]
module MetroAppBarToggleButton =

  /// Returns toggle button properties with controlled <paramref name="isChecked"/>.
  val defaults:
    label: string * iconName: string * isChecked: bool -> MetroAppBarToggleProps

  /// Builds the app-bar toggle button inside a <c>Cell.Build</c> scope.
  val build: props: MetroAppBarToggleProps * onChange: (bool -> unit) -> Blob

/// Vertical visual divider for grouping commands inside a Metro app bar.
[<RequireQualifiedAccess>]
module MetroAppBarSeparator =

  /// Builds an app-bar separator. When <paramref name="visible"/> is
  /// <c>false</c>, preserves layout width while hiding the rule.
  val build: visible: bool -> Blob

  /// Builds a visible app-bar separator.
  val standard: unit -> Blob
