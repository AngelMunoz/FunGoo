namespace FunGoo.Metro

open Goo

/// Severity level of a Metro inline notification banner or toast.
[<Struct>]
type MetroInfoBarSeverity =
  | Informational
  | Success
  | Warning
  | Error

/// Event raised by a <see cref="MetroInfoBar"/> banner.
[<Struct; NoComparison>]
type MetroInfoBarEvent =
  | Closed
  | ActionInvoked

/// Data-only properties of a <see cref="MetroInfoBar"/> banner.
[<Struct; NoComparison>]
type MetroInfoBarProps = {
  /// Visual and semantic severity level.
  Severity: MetroInfoBarSeverity
  /// Optional bold heading displayed above the message.
  Title: string voption
  /// Primary notification message text.
  Message: string
  /// Optional action button label.
  ActionText: string voption
  /// When <c>true</c>, renders a trailing close button.
  Closable: bool
  /// When <c>false</c>, collapses the banner out of layout.
  Open: bool
  /// Optional accessible label override.
  AccessibilityName: string voption
}

/// Stateless inline Metro notification banner with a 4 px logical start
/// severity stripe, Material icon, optional action button, and close button.
[<RequireQualifiedAccess>]
module MetroInfoBar =

  /// Resolves the Metro stripe and icon color for <paramref name="severity"/>.
  val severityColor: severity: MetroInfoBarSeverity * theme: MetroTheme -> Color

  /// Resolves the Material icon name for <paramref name="severity"/>.
  val severityIconName: severity: MetroInfoBarSeverity -> string

  /// Returns default informational banner properties for
  /// <paramref name="message"/>.
  val defaults: message: string -> MetroInfoBarProps

  /// Returns informational banner properties with <paramref name="title"/> and
  /// <paramref name="message"/>.
  val informational: title: string * message: string -> MetroInfoBarProps

  /// Returns success banner properties with <paramref name="title"/> and
  /// <paramref name="message"/>.
  val success: title: string * message: string -> MetroInfoBarProps

  /// Returns warning banner properties with <paramref name="title"/> and
  /// <paramref name="message"/>.
  val warning: title: string * message: string -> MetroInfoBarProps

  /// Returns error banner properties with <paramref name="title"/> and
  /// <paramref name="message"/>.
  val error: title: string * message: string -> MetroInfoBarProps

  /// Builds the inline notification banner inside a <c>Cell.Build</c> scope.
  val build:
    props: MetroInfoBarProps * onEvent: (MetroInfoBarEvent -> unit) -> Blob
