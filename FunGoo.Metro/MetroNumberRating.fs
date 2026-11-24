namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Inputs

[<Struct; NoComparison>]
type MetroNumberBoxProps = {
  Value: float
  Minimum: float voption
  Maximum: float voption
  Step: float voption
  Label: string voption
  Disabled: bool
  ReadOnly: bool
  Width: Length voption
}

module NumberRatingInternals =

  let toNullable(value: float voption) : Nullable<float> =
    match value with
    | ValueSome v -> Nullable v
    | ValueNone -> Nullable()

[<RequireQualifiedAccess>]
module MetroNumberBox =

  let defaults(value: float) : MetroNumberBoxProps = {
    Value = value
    Minimum = ValueNone
    Maximum = ValueNone
    Step = ValueNone
    Label = ValueNone
    Disabled = false
    ReadOnly = false
    Width = ValueNone
  }

  let toInput
    (props: MetroNumberBoxProps, onChange: Action<float>)
    : NumberInputInput =
    NumberInputInput(
      Value = props.Value,
      Minimum = NumberRatingInternals.toNullable props.Minimum,
      Maximum = NumberRatingInternals.toNullable props.Maximum,
      Step = NumberRatingInternals.toNullable props.Step,
      Label = ValueOption.defaultValue null props.Label,
      ShowSpinButtons = Nullable true,
      Disabled = props.Disabled,
      ReadOnly = props.ReadOnly,
      Width =
        (match props.Width with
         | ValueSome width -> Nullable width
         | ValueNone -> Nullable()),
      OnChange = onChange
    )

  let mount
    (key: string, props: MetroNumberBoxProps, onChange: Action<float>)
    : Blob =
    Cell.Mount<NumberInputInput, NumberInput>(key, toInput(props, onChange))

[<Struct; NoComparison>]
type MetroRatingProps = {
  Value: float
  Maximum: int
  Step: float
  AccessibilityName: string voption
  ReadOnly: bool
  Disabled: bool
}

[<RequireQualifiedAccess>]
module MetroRating =

  let defaults(value: float) : MetroRatingProps = {
    Value = value
    Maximum = 5
    Step = 1.0
    AccessibilityName = ValueNone
    ReadOnly = false
    Disabled = false
  }

  let toInput
    (props: MetroRatingProps, onChange: Action<float>)
    : RatingControlInput =
    RatingControlInput(
      Value = props.Value,
      OnChange = onChange,
      Maximum =
        (if props.Maximum > 0 then
           Nullable props.Maximum
         else
           Nullable()),
      Step =
        (if Double.IsFinite props.Step && props.Step > 0.0 then
           Nullable props.Step
         else
           Nullable()),
      ReadOnly = props.ReadOnly,
      Disabled = props.Disabled,
      AccessibilityName = ValueOption.defaultValue null props.AccessibilityName
    )

  let mount
    (key: string, props: MetroRatingProps, onChange: Action<float>)
    : Blob =
    Cell.Mount<RatingControlInput, RatingControl>(key, toInput(props, onChange))
