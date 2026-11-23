namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Inputs

/// Data-only properties for a Metro number box.
[<Struct; NoComparison>]
type MetroNumberBoxProps = {
  /// Current value.
  Value: float
  /// Lower bound. <c>ValueNone</c> leaves the bound open.
  Minimum: float voption
  /// Upper bound. <c>ValueNone</c> leaves the bound open.
  Maximum: float voption
  /// Value change of each spin button press. <c>ValueNone</c> uses the widget
  /// default.
  Step: float voption
  /// Optional header text above the box.
  Label: string voption
  /// Rejects edits.
  Disabled: bool
  /// Prevents edits and keeps selection and copy.
  ReadOnly: bool
  /// Width of the box. <c>ValueNone</c> uses the widget default.
  Width: Length voption
}

/// Metro number box on <see cref="Goo.Widgets.Inputs.NumberInput"/>: a sharp
/// entry with spin buttons, under the Metro widget theme.
[<RequireQualifiedAccess>]
module MetroNumberBox =

  /// Creates <see cref="MetroNumberBoxProps"/> for <paramref name="value"/> with
  /// open bounds, the widget step default, and no label.
  val defaults: value: float -> MetroNumberBoxProps

  /// Maps <paramref name="props"/> to the widget input, with the spin buttons
  /// shown.
  val toInput:
    props: MetroNumberBoxProps * onChange: Action<float> -> NumberInputInput

  /// Mounts a number box under the stable <paramref name="key"/>. Cache
  /// <paramref name="onChange"/> outside <c>Build</c>, so that an unchanged
  /// input does not rebuild the box.
  val mount:
    key: string * props: MetroNumberBoxProps * onChange: Action<float> -> Blob

/// Data-only properties for a Metro rating control.
[<Struct; NoComparison>]
type MetroRatingProps = {
  /// Current rating value.
  Value: float
  /// Highest rating. A value below 1 uses the widget default.
  Maximum: int
  /// Smallest value change. A value that is not finite and positive uses the
  /// widget default.
  Step: float
  /// Optional accessible name of the rating control.
  AccessibilityName: string voption
  /// Prevents changes and keeps the value readable.
  ReadOnly: bool
  /// Rejects interaction.
  Disabled: bool
}

/// Metro rating control on <see cref="Goo.Widgets.Inputs.RatingControl"/>, with
/// the accent color of the active Metro theme.
[<RequireQualifiedAccess>]
module MetroRating =

  /// Creates <see cref="MetroRatingProps"/> for <paramref name="value"/> with a
  /// highest rating of 5 and a step of 1.
  val defaults: value: float -> MetroRatingProps

  /// Maps <paramref name="props"/> to the widget input.
  val toInput:
    props: MetroRatingProps * onChange: Action<float> -> RatingControlInput

  /// Mounts a rating control under the stable <paramref name="key"/>. Cache
  /// <paramref name="onChange"/> outside <c>Build</c>, so that an unchanged
  /// input does not rebuild the control.
  val mount:
    key: string * props: MetroRatingProps * onChange: Action<float> -> Blob
