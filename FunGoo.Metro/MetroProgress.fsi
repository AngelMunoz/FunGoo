namespace FunGoo.Metro

open Goo

/// Data-only properties for a determinate or indeterminate Metro progress bar.
[<Struct; NoComparison>]
type MetroProgressBarProps = {
  /// Normalized progress in <c>[0.0, 1.0]</c> for determinate mode, or
  /// <c>ValueNone</c> for indeterminate 5-dot motion.
  Value: float voption
  /// Bar and dot height in logical pixels (defaults to <c>4.0</c>).
  Height: float
  /// Track width (defaults to <c>Length.Percent 100.0</c>).
  Width: Length
  /// Optional accessible name for screen readers.
  AccessibilityName: string voption
}

/// Metro progress bar supporting sharp determinate fills and zero-rebuild
/// indeterminate 5-dot motion.
[<RequireQualifiedAccess>]
module MetroProgressBar =

  /// Creates determinate <see cref="MetroProgressBarProps"/> with
  /// <paramref name="value"/> (clamped to <c>[0.0, 1.0]</c> when built).
  val determinate: value: float -> MetroProgressBarProps

  /// Default indeterminate <see cref="MetroProgressBarProps"/> with
  /// <c>Value = ValueNone</c>, 4 px height, and 100% width.
  val indeterminate: MetroProgressBarProps

  /// Builds a determinate bar or mounts the indeterminate 5-dot progress cell
  /// under stable <paramref name="key"/>.
  val build: key: string * props: MetroProgressBarProps -> Blob

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

/// Orbiting 5-dot Metro progress ring driven by paint-bound animations without
/// per-tick cell rebuilds.
[<RequireQualifiedAccess>]
module MetroProgressRing =

  /// Creates default <see cref="MetroProgressRingProps"/> with
  /// <see cref="MetroProgressRingSize.Normal"/> and theme accent color.
  val defaults: active: bool -> MetroProgressRingProps

  /// Mounts the orbiting 5-dot progress ring cell under stable
  /// <paramref name="key"/>.
  val mount: key: string * props: MetroProgressRingProps -> Blob
