namespace FunGoo.Metro

open System.Collections.Generic
open Goo

/// Surface, foreground, border, interaction, and semantic color roles for a Metro theme.
[<Struct; NoComparison>]
type MetroPalette = {
  /// Primary canvas background (<c>#000000</c> in dark mode, <c>#ffffff</c> in light mode).
  Background: Color
  /// Secondary chrome surface (<c>#1a1a1a</c> in dark mode, <c>#f3f3f3</c> in light mode).
  BackgroundAlt: Color
  /// Translucent elevated surface tint.
  Surface: Color
  /// Primary foreground text and icon color.
  Foreground: Color
  /// Secondary muted foreground color.
  ForegroundSecondary: Color
  /// Disabled text and control foreground color.
  ForegroundDisabled: Color
  /// Standard subtle divider and container border color.
  Border: Color
  /// High-emphasis control border color.
  BorderStrong: Color
  /// Neutral highlight fill color.
  Highlight: Color
  /// Neutral pointer hover surface fill.
  Hover: Color
  /// Neutral pressed/active surface fill.
  Active: Color
  /// Legible informational foreground color.
  Info: Color
  /// Legible success foreground color.
  Success: Color
  /// Legible warning foreground color.
  Warning: Color
  /// Legible danger/error foreground color.
  Danger: Color
  /// Destructive action fill color.
  DangerFill: Color
  /// Hovered destructive action fill color.
  DangerHover: Color
}

/// Built-in dark and light Metro color palettes.
[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroPalette =

  /// True-black dark mode Metro palette.
  val dark: MetroPalette

  /// Pure-white light mode Metro palette.
  val light: MetroPalette

/// Semantic roles in the 10-step Metro typographic ramp.
[<Struct>]
type MetroTextRole =
  /// 56 px light display headline.
  | Hero
  /// 42 px light page title.
  | Title
  /// 28 px light section header.
  | Header
  /// 26 px light subsection header.
  | Subheader
  /// 26 px regular secondary header.
  | Header2
  /// 20 px light subtitle.
  | Subtitle
  /// 16 px regular medium lead text.
  | Medium
  /// 14 px regular body copy.
  | Body
  /// 12 px regular caption and metadata text.
  | Caption
  /// 11 px bold uppercase badge label.
  | Badge

/// Concrete font metrics for one <see cref="MetroTextRole"/>.
[<Struct>]
type MetroTypeSpec = {
  /// Logical font size in pixels.
  FontSize: float
  /// CSS/OpenType numeric font weight (300, 400, or 700).
  FontWeight: int
  /// Letter spacing in logical pixels (<c>em * FontSize</c>).
  LetterSpacing: float
  /// Line-height multiplier relative to <see cref="FontSize"/>.
  LineHeight: float
  /// Whether text in this role renders uppercase.
  Uppercase: bool
}

/// Metro typographic ramp lookup.
[<RequireQualifiedAccess>]
module MetroTypography =

  /// Returns the <see cref="MetroTypeSpec"/> for the requested <see cref="MetroTextRole"/>.
  val spec: role: MetroTextRole -> MetroTypeSpec

  val internal size: role: MetroTextRole -> float

/// Metro 4 px base spacing scale in logical pixels.
[<RequireQualifiedAccess>]
module MetroSpacing =

  /// Base spacing unit (4 px).
  val unit: float
  /// Extra-small spacing (4 px).
  val xs: float
  /// Small spacing (8 px).
  val sm: float
  /// Medium spacing (12 px).
  val md: float
  /// Large spacing (16 px).
  val lg: float
  /// Extra-large spacing (24 px).
  val xl: float
  /// Double-extra-large spacing (40 px).
  val xxl: float

/// Canonical Metro motion durations and entrance stagger delays in milliseconds.
[<RequireQualifiedAccess>]
module MetroTiming =

  /// Fast micro-interaction transition duration (167 ms).
  val fastMs: float
  /// Standard transition duration (250 ms).
  val normalMs: float
  /// Slow emphasis transition duration (333 ms).
  val slowMs: float
  /// Continuum shared-element flight duration (150 ms).
  val continuumMs: float
  /// Turnstile forward entrance duration (500 ms).
  val turnstileInMs: float
  /// Turnstile exit duration (400 ms).
  val turnstileOutMs: float
  /// Sub-linear entrance stagger delays in milliseconds for sequential list or tile items.
  val staggerDelaysMs: IReadOnlyList<float>
