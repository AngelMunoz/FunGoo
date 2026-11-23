namespace FunGoo.Metro

open System
open Goo

/// Data-only properties for a sharp, 2 px bordered Metro text input field.
[<Struct; NoComparison>]
type MetroTextBoxProps = {
  /// Optional visible label rendered above the text entry.
  Label: string voption
  /// Controlled text value displayed by the input.
  Value: string
  /// Optional placeholder text shown when <see cref="Value"/> is empty.
  Placeholder: string voption
  /// Optional validation error message; non-empty marks the field invalid.
  Error: string voption
  /// Masks entered characters when <c>true</c>.
  Password: bool
  /// Shows a password reveal toggle when <see cref="Password"/> is <c>true</c>.
  RevealButton: bool
  /// Disables pointer and keyboard interaction when <c>true</c>.
  Disabled: bool
  /// Prevents edits while preserving selection and copy when <c>true</c>.
  ReadOnly: bool
  /// Optional explicit width for the text box container.
  Width: Length voption
}

/// Metro text box builder wrapping <see cref="Goo.Widgets.Inputs.TextField"/>.
[<RequireQualifiedAccess>]
module MetroTextBox =

  /// Creates default <see cref="MetroTextBoxProps"/> for
  /// <paramref name="value"/>.
  val defaults: value: string -> MetroTextBoxProps

  /// Builds a Metro-styled text input <see cref="Blob"/> bound to the active
  /// <see cref="MetroTheme"/>.
  val build:
    props: MetroTextBoxProps *
    onChange: (string -> unit) *
    onSubmit: (string -> unit) voption ->
      Blob

/// Data-only properties for a sharp rectangular Metro toggle switch.
[<Struct; NoComparison>]
type MetroToggleSwitchProps = {
  /// Optional header text displayed above the switch.
  Header: string voption
  /// Current checked state of the switch.
  Checked: bool
  /// Disables toggling and dims visuals when <c>true</c>.
  Disabled: bool
  /// State text displayed beside the track when <see cref="Checked"/> is
  /// <c>true</c> (defaults to <c>"On"</c>).
  OnLabel: string
  /// State text displayed beside the track when <see cref="Checked"/> is
  /// <c>false</c> (defaults to <c>"Off"</c>).
  OffLabel: string
  /// Optional accessible name overriding <see cref="Header"/> and state text.
  AccessibilityName: string voption
}

/// Native sharp rectangular Metro toggle switch builder.
[<RequireQualifiedAccess>]
module MetroToggleSwitch =

  /// Creates default <see cref="MetroToggleSwitchProps"/> for
  /// <paramref name="isChecked"/> with <c>"On"</c> and <c>"Off"</c> labels.
  val defaults: isChecked: bool -> MetroToggleSwitchProps

  /// Builds a sharp rectangular Metro toggle switch <see cref="Blob"/>.
  val build: props: MetroToggleSwitchProps * onChange: (bool -> unit) -> Blob

/// Data-only properties for a sharp, 2 px bordered Metro checkbox.
[<Struct; NoComparison>]
type MetroCheckBoxProps = {
  /// Visible label text rendered beside the checkbox.
  Label: string
  /// Current checked state.
  Checked: bool
  /// Disables toggling when <c>true</c>.
  Disabled: bool
}

/// Metro checkbox builder wrapping <see cref="Goo.Widgets.Inputs.Checkbox"/>.
[<RequireQualifiedAccess>]
module MetroCheckBox =

  /// Builds a Metro-styled checkbox <see cref="Blob"/> with zero corner radius
  /// and 2 px borders.
  val build: props: MetroCheckBoxProps * onChange: (bool -> unit) -> Blob

/// Data-only properties for a Metro radio button.
[<Struct; NoComparison>]
type MetroRadioButtonProps = {
  /// Visible label text rendered beside the radio indicator.
  Label: string
  /// Whether this radio option is currently selected.
  Selected: bool
  /// Disables selection when <c>true</c>.
  Disabled: bool
}

/// Metro radio button builder wrapping
/// <see cref="Goo.Widgets.Inputs.RadioButton"/>.
[<RequireQualifiedAccess>]
module MetroRadioButton =

  /// Builds a Metro-styled radio button <see cref="Blob"/> with 2 px indicator
  /// borders and theme accent selection colors.
  val build: props: MetroRadioButtonProps * onSelect: (unit -> unit) -> Blob

/// Data-only properties for a sharp rectangular Metro slider.
[<Struct; NoComparison>]
type MetroSliderProps = {
  /// Optional label rendered above the slider.
  Label: string voption
  /// Controlled numeric value.
  Value: float
  /// Minimum slider value.
  Min: float
  /// Maximum slider value.
  Max: float
  /// Step increment; values <c>&lt;= 0.0</c> leave dragging continuous.
  Step: float
  /// Disables dragging and keyboard adjustments when <c>true</c>.
  Disabled: bool
}

/// Metro slider mounter wrapping <see cref="Goo.Widgets.Inputs.Slider"/>.
[<RequireQualifiedAccess>]
module MetroSlider =

  /// Creates default <see cref="MetroSliderProps"/> for
  /// <paramref name="value"/> in <c>[minValue, maxValue]</c>.
  val defaults:
    value: float * minValue: float * maxValue: float -> MetroSliderProps

  /// Mounts a stateful <see cref="Goo.Widgets.Inputs.Slider"/> under stable
  /// <paramref name="key"/> with sharp zero-radius thumb and track geometry.
  /// Cache <paramref name="onChange"/> outside <c>Build</c> to preserve input
  /// equality.
  val mount:
    key: string * props: MetroSliderProps * onChange: Action<float> -> Blob
