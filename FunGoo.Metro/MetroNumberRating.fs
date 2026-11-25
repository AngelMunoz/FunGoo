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

[<AbstractClass>]
type MetroNumberBoxCell() =
  inherit Cell<MetroNumberBoxProps>()

  abstract Mount: key: string * props: MetroNumberBoxProps -> Blob

[<RequireQualifiedAccess>]
module MetroNumberBox =

  let numberSubKey = "number"

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

  let create(onChange: float -> unit) : MetroNumberBoxCell =
    let handler = Action<float>(onChange)

    let mountWidget(key: string, props: MetroNumberBoxProps) : Blob =
      Cell.Mount<NumberInputInput, NumberInput>(key, toInput(props, handler))

    {
      new MetroNumberBoxCell() with
        override _.Build(props: MetroNumberBoxProps) : Blob =
          mountWidget(numberSubKey, props)

        override _.Mount(key: string, props: MetroNumberBoxProps) : Blob =
          mountWidget(key, props)
    }

[<Struct; NoComparison>]
type MetroRatingProps = {
  Value: float
  Maximum: int
  Step: float
  AccessibilityName: string voption
  ReadOnly: bool
  Disabled: bool
}

[<AbstractClass>]
type MetroRatingCell() =
  inherit Cell<MetroRatingProps>()

  abstract Mount: key: string * props: MetroRatingProps -> Blob

[<RequireQualifiedAccess>]
module MetroRating =

  let ratingSubKey = "rating"

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

  let create(onChange: float -> unit) : MetroRatingCell =
    let handler = Action<float>(onChange)

    let mountWidget(key: string, props: MetroRatingProps) : Blob =
      Cell.Mount<RatingControlInput, RatingControl>(
        key,
        toInput(props, handler)
      )

    {
      new MetroRatingCell() with
        override _.Build(props: MetroRatingProps) : Blob =
          mountWidget(ratingSubKey, props)

        override _.Mount(key: string, props: MetroRatingProps) : Blob =
          mountWidget(key, props)
    }
