namespace FunGoo.Metro

open System.Collections.Generic
open Goo

/// Resolved Metro accent color family with hover/pressed shades and
/// WCAG contrast-safe foreground paint.
[<Struct; NoComparison>]
type MetroAccent = {
  /// Primary accent fill color.
  Base: Color
  /// Lighter shade used for dark-surface hover and highlights.
  Light: Color
  /// Darker shade used for light-surface hover and pressed states.
  Dark: Color
  /// High-contrast foreground color (white or black) painted on top of <see cref="Base"/>.
  OnAccent: Color
}

/// Selects a built-in Metro accent palette, a custom color, or the operating system accent.
[<Struct; NoComparison>]
type MetroAccentKind =
  /// Metro Blue (<c>#0078d4</c>).
  | Blue
  /// Metro Red (<c>#e51400</c>).
  | Red
  /// Metro Orange (<c>#fa6800</c>).
  | Orange
  /// Metro Green (<c>#00a300</c>).
  | Green
  /// Metro Teal (<c>#00aba9</c>).
  | Teal
  /// Metro Purple (<c>#a200ff</c>).
  | Purple
  /// Metro Magenta (<c>#d80073</c>).
  | Magenta
  /// Metro Lime (<c>#a4c400</c>).
  | Lime
  /// Metro Brown (<c>#825a2c</c>).
  | Brown
  /// Metro Pink (<c>#f472d6</c>).
  | Pink
  /// Metro Mango (<c>#f09609</c>).
  | Mango
  /// Metro Cobalt (<c>#0050ef</c>).
  | Cobalt
  /// Metro Indigo (<c>#6a00ff</c>).
  | Indigo
  /// Metro Violet (<c>#aa00ff</c>).
  | Violet
  /// Metro Crimson (<c>#a20025</c>).
  | Crimson
  /// Metro Emerald (<c>#008a00</c>).
  | Emerald
  /// Metro Mauve (<c>#765f89</c>).
  | Mauve
  /// Metro Sienna (<c>#a0522d</c>).
  | Sienna
  /// Metro Olive (<c>#6d8764</c>).
  | Olive
  /// Metro Steel (<c>#647687</c>).
  | Steel
  /// Metro Taupe (<c>#87794e</c>).
  | Taupe
  /// User-supplied base color with derived light, dark, and foreground shades.
  | Custom of Color
  /// Tracks the operating system accent color, falling back to <see cref="Blue"/> when unavailable.
  | System

/// Operations for resolving Metro accent palettes and deriving shades from custom colors.
[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroAccent =

  /// Derives a complete <see cref="MetroAccent"/> from a base color, blending 22% toward white
  /// for <c>Light</c>, 22% toward black for <c>Dark</c>, and choosing WCAG contrast-safe
  /// <c>Color.White</c> or <c>Color.Black</c> for <c>OnAccent</c>.
  val fromColor: color: Color -> MetroAccent

  /// Resolves a <see cref="MetroAccentKind"/> using the optional operating system accent color.
  /// When <paramref name="kind"/> is <c>System</c> and <paramref name="systemAccent"/> is
  /// <c>ValueNone</c>, falls back to <c>MetroAccentKind.Blue</c>.
  val resolve:
    kind: MetroAccentKind * systemAccent: Color voption -> MetroAccent

  /// All 21 built-in named Metro accents in canonical display order.
  val named: IReadOnlyList<MetroAccentKind * string * MetroAccent>
