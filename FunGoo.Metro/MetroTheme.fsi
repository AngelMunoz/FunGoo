namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets

/// Appearance mode policy for a Metro-themed window.
[<Struct>]
type MetroMode =
  /// Follows the operating system light or dark preference.
  | System
  /// Locks the window to the light Metro palette.
  | Light
  /// Locks the window to the dark Metro palette.
  | Dark

/// Configuration supplied when attaching Metro theming to a <see cref="Window"/>.
[<Struct; NoComparison>]
type MetroConfig = {
  /// Appearance mode policy (System, Light, or Dark).
  Mode: MetroMode
  /// Accent selection (built-in Metro accent, custom color, or OS accent).
  Accent: MetroAccentKind
  /// Preferred UI sans-serif font family; empty string selects the platform sans-serif.
  FontFamily: string
  /// Preferred monospace font family.
  MonoFontFamily: string
  /// Base tile square edge length in logical pixels (defaults to 150 px).
  TileUnit: float
}

/// Default configuration values for <see cref="MetroConfig"/>.
[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroConfig =

  /// Default Metro configuration: system mode, Metro Blue accent, system sans-serif font,
  /// <c>"monospace"</c> mono font, and a 150 px tile unit.
  val defaultConfig: MetroConfig

/// Resolved reactive Metro theme snapshot published through <c>Context&lt;MetroTheme&gt;</c>.
[<Struct; NoComparison>]
type MetroTheme = {
  /// <c>true</c> when the dark palette is active; <c>false</c> for light mode.
  IsDark: bool
  /// Active surface, foreground, border, and semantic color palette.
  Palette: MetroPalette
  /// Active accent family with base, light, dark, and contrast-safe foreground shades.
  Accent: MetroAccent
  /// Active UI font family.
  FontFamily: string
  /// Active monospace font family.
  MonoFontFamily: string
  /// Base tile unit in logical pixels.
  TileUnit: float
  /// <c>true</c> when the platform requests reduced motion.
  ReducedMotion: bool
  /// <c>true</c> when the platform requests high contrast.
  HighContrast: bool
  /// Effective platform text scale factor (1.0 at standard scale).
  TextScaleFactor: float
}

/// Helper projections and Goo.Widgets bridge functions for <see cref="MetroTheme"/>.
[<RequireQualifiedAccess;
  CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MetroTheme =

  /// Returns the hover color for accent-colored text (<c>Accent.Light</c> in dark mode,
  /// <c>Accent.Dark</c> in light mode).
  val accentTextHover: theme: MetroTheme -> Color

  /// Returns <c>0.0</c> when <see cref="MetroTheme.ReducedMotion"/> is enabled;
  /// otherwise returns <paramref name="durationMs"/> unchanged.
  val effectiveMs: theme: MetroTheme * durationMs: float -> float

  /// Projects a <see cref="MetroTheme"/> to a sharp, flat <see cref="Theme"/> for
  /// standard <c>Goo.Widgets</c> controls with zero corner radii, no shadows, 2 px borders,
  /// and Metro typography and timing metrics.
  val toWidgetTheme:
    theme: MetroTheme * variant: Func<bool, Theme> voption -> Theme

/// Window attachment, runtime updates, reactive reads, and subtree scoping for Metro themes.
[<RequireQualifiedAccess>]
module Metro =

  /// Attaches Metro theming to <paramref name="window"/> using <paramref name="config"/>,
  /// synchronizing both <c>Context&lt;MetroTheme&gt;</c> and <c>Goo.Widgets.Theme</c> and
  /// tracking operating system preferences automatically.
  val attach: window: Window * config: MetroConfig -> unit

  /// Attaches Metro theming to <paramref name="window"/> using <see cref="MetroConfig.defaultConfig"/>.
  val attachDefault: window: Window -> unit

  /// Updates the appearance mode on <paramref name="window"/> and synchronizes both contexts.
  val setMode: window: Window * mode: MetroMode -> unit

  /// Updates the accent selection on <paramref name="window"/> and synchronizes both contexts.
  val setAccent: window: Window * accent: MetroAccentKind -> unit

  /// Reads the nearest retained <see cref="MetroTheme"/> during a <c>Cell.Build</c> or virtual
  /// callback and subscribes the current owner to updates.
  val read: unit -> MetroTheme

  /// Reads the window-level <see cref="MetroTheme"/> snapshot without registering a dependency.
  val readWindow: window: Window -> MetroTheme

  /// Overrides both <see cref="MetroTheme"/> and <c>Goo.Widgets.Theme</c> for the subtree built
  /// by <paramref name="build"/> without affecting siblings or window colors.
  val scope: theme: MetroTheme * build: (unit -> Blob) -> Blob

  /// Builds a fresh root canvas <see cref="Style"/> with Metro background, foreground,
  /// font family, body font size, weight, and line height.
  val canvasStyle: theme: MetroTheme -> Style
