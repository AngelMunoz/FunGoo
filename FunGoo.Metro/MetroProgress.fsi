namespace FunGoo.Metro

open System
open Goo

/// Data-only properties for a determinate or indeterminate Metro progress bar.
[<Struct; NoComparison>]
type MetroProgressBarProps = {
  /// Raw progress for determinate mode, scaled as <c>Value / Maximum</c>
  /// clamped to <c>[0.0, 1.0]</c>, or <c>ValueNone</c> for indeterminate
  /// 5-dot motion.
  Value: float voption
  /// Scale of <c>Value</c> for determinate mode (defaults to <c>1.0</c>).
  /// Non-finite and non-positive values build as <c>1.0</c>.
  Maximum: float
  /// When <c>true</c>, renders the determinate percent as a
  /// <c>MetroText</c> caption under the bar. Indeterminate bars ignore it.
  ShowLabel: bool
  /// Bar and dot height in logical pixels (defaults to <c>4.0</c>).
  Height: float
  /// Track width (defaults to <c>Length.Percent 100.0</c>).
  Width: Length
  /// Optional accessible name for screen readers.
  AccessibilityName: string voption
}

/// Data-only input snapshot for <see cref="MetroIndeterminateBarCell"/>.
[<Struct; NoComparison>]
type MetroIndeterminateBarInput = {
  /// Bar and dot height in logical pixels.
  Height: float
  /// Track width.
  Width: Length
  /// Optional accessible name.
  AccessibilityName: string voption
}

/// The indeterminate 5-dot progress cell. The dots move on paint, so a frame
/// causes no cell rebuild. <c>MetroIndeterminateBar.create</c> makes the cell.
/// The type holds the surface that tests use.
[<AbstractClass>]
type MetroIndeterminateBarCell =
  inherit Cell<MetroIndeterminateBarInput>

  /// The paint-bound translation of each dot. A test reads them.
  abstract TranslateAnims: Anim<Point> array

  /// The paint-bound opacity of each dot. A test reads them.
  abstract OpacityAnims: Anim<float> array

  /// The tree of the current input. A test reads it, and only inside a
  /// build, where the Metro theme is readable.
  abstract View: Blob

/// Construction of the indeterminate bar cell. <see cref="MetroProgressBar.build"/>
/// mounts it.
[<RequireQualifiedAccess>]
module MetroIndeterminateBar =

  /// Creates the indeterminate 5-dot progress cell.
  val create: unit -> MetroIndeterminateBarCell

/// Metro progress bar supporting sharp determinate fills and zero-rebuild
/// indeterminate 5-dot motion.
[<RequireQualifiedAccess>]
module MetroProgressBar =

  /// Creates determinate <see cref="MetroProgressBarProps"/> with
  /// <paramref name="value"/> (scaled by <c>Maximum</c> and clamped to
  /// <c>[0.0, 1.0]</c> when built).
  val determinate: value: float -> MetroProgressBarProps

  /// Default indeterminate <see cref="MetroProgressBarProps"/> with
  /// <c>Value = ValueNone</c>, <c>Maximum = 1.0</c>,
  /// <c>ShowLabel = false</c>, 4 px height, and 100% width.
  val indeterminate: MetroProgressBarProps

  /// Builds a determinate bar or mounts the indeterminate 5-dot progress cell
  /// under stable <paramref name="key"/>.
  val build: key: string * props: MetroProgressBarProps -> Blob

  /// Builds a determinate bar, or mounts the indeterminate 5-dot progress cell
  /// that <paramref name="factory"/> creates, under stable
  /// <paramref name="key"/>. A test passes a factory to keep the cell.
  val buildWith:
    key: string *
    props: MetroProgressBarProps *
    factory: (unit -> MetroIndeterminateBarCell) ->
      Blob

/// Size preset for <see cref="MetroProgressRing"/>.
[<Struct>]
type MetroProgressRingSize =
  /// 24 px bounding box with 9 px orbit radius.
  | Small
  /// 36 px bounding box with 14 px orbit radius.
  | Normal
  /// 52 px bounding box with 21 px orbit radius.
  | Large

/// Data-only properties for an orbiting 5-dot Metro progress ring.
[<Struct; NoComparison>]
type MetroProgressRingProps = {
  /// Runs the orbiting animation when <c>true</c>; pauses and hides when
  /// <c>false</c>.
  Active: bool
  /// Bounding box and orbit radius preset.
  Size: MetroProgressRingSize
  /// Optional dot color overriding <c>MetroTheme.Accent.Base</c>.
  Color: Color voption
  /// Optional accessible name for screen readers.
  AccessibilityName: string voption
}

/// The orbiting 5-dot progress ring cell. The dots move on paint, so a frame
/// causes no cell rebuild. <c>MetroProgressRing.create</c> makes the cell. The
/// type holds the surface that tests use.
[<AbstractClass>]
type MetroProgressRingCell =
  inherit Cell<MetroProgressRingProps>

  /// The paint-bound translation of each dot. A test reads them.
  abstract TranslateAnims: Anim<Point> array

  /// The paint-bound opacity of each dot. A test reads them.
  abstract OpacityAnims: Anim<float> array

  /// The tree of the current input. A test reads it, and only inside a
  /// build, where the Metro theme is readable.
  abstract View: Blob

/// Orbiting 5-dot Metro progress ring driven by paint-bound animations without
/// per-tick cell rebuilds.
[<RequireQualifiedAccess>]
module MetroProgressRing =

  /// Creates default <see cref="MetroProgressRingProps"/> with
  /// <see cref="MetroProgressRingSize.Normal"/> and theme accent color.
  val defaults: active: bool -> MetroProgressRingProps

  /// Creates the orbiting 5-dot progress ring cell.
  val create: unit -> MetroProgressRingCell

  /// Mounts the orbiting 5-dot progress ring cell under stable
  /// <paramref name="key"/>.
  val mount: key: string * props: MetroProgressRingProps -> Blob

  /// Mounts the orbiting 5-dot progress ring cell that <paramref name="factory"/>
  /// creates, under stable <paramref name="key"/>. A test passes a factory to
  /// keep the cell.
  val mountWith:
    key: string *
    props: MetroProgressRingProps *
    factory: (unit -> MetroProgressRingCell) ->
      Blob
