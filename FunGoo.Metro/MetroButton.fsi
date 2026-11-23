namespace FunGoo.Metro

open Goo

/// Visual emphasis variant for a <see cref="MetroButton"/>.
[<Struct>]
type MetroButtonVariant =
  /// Sharp 2 px bordered outline button inverting fill on press.
  | Standard
  /// Filled theme-accent primary action button.
  | Accent
  /// Borderless accent-colored inline hyperlink button.
  | Hyperlink
  /// Borderless neutral foreground button with subtle hover and active fills.
  | Subtle

/// Data-only properties for constructing a sharp, flat Metro button.
[<Struct; NoComparison>]
type MetroButtonProps = {
  /// Visible button label.
  Content: string
  /// Visual variant controlling border, fill, and hover/active treatment.
  Variant: MetroButtonVariant
  /// When <c>true</c>, blocks pointer/keyboard activation and dims visuals.
  Disabled: bool
  /// Optional toggle pressed state reflected in visuals and accessibility.
  Pressed: bool voption
  /// Optional accessible name overriding <see cref="Content"/>.
  AccessibilityName: string voption
  /// Optional minimum width in logical pixels.
  MinWidth: float voption
}

/// Sharp, flat Metro button builders with zero corner radii, zero shadows,
/// press scale feedback, and keyboard accessibility.
[<RequireQualifiedAccess>]
module MetroButton =

  /// Creates default <see cref="MetroButtonProps"/> for
  /// <paramref name="content"/> using the standard variant.
  val defaults: content: string -> MetroButtonProps

  /// Builds a Metro button <see cref="Blob"/> styled from the active
  /// <see cref="MetroTheme"/> and invoking <paramref name="onClick"/> when
  /// activated.
  val build: props: MetroButtonProps * onClick: (unit -> unit) -> Blob

  /// Builds a standard 2 px bordered Metro button.
  val inline standard: content: string * onClick: (unit -> unit) -> Blob

  /// Builds an accent-filled primary Metro button.
  val inline accent: content: string * onClick: (unit -> unit) -> Blob

  /// Builds a borderless accent hyperlink Metro button.
  val inline hyperlink: content: string * onClick: (unit -> unit) -> Blob

  /// Builds a borderless subtle Metro button.
  val inline subtle: content: string * onClick: (unit -> unit) -> Blob
