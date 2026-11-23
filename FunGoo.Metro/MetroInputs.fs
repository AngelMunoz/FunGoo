namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Inputs
open FunGoo.Children

[<Struct; NoComparison>]
type MetroTextBoxProps = {
  Label: string voption
  Value: string
  Placeholder: string voption
  Error: string voption
  Password: bool
  RevealButton: bool
  Disabled: bool
  ReadOnly: bool
  Width: Length voption
}

[<RequireQualifiedAccess>]
module MetroTextBox =

  let defaults(value: string) : MetroTextBoxProps = {
    Label = ValueNone
    Value = value
    Placeholder = ValueNone
    Error = ValueNone
    Password = false
    RevealButton = false
    Disabled = false
    ReadOnly = false
    Width = ValueNone
  }

  let build
    (
      props: MetroTextBoxProps,
      onChange: string -> unit,
      onSubmit: (string -> unit) voption
    ) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    let scaledFont = 14.0 * theme.TextScaleFactor

    let hasError =
      props.Error
      |> ValueOption.map(fun e -> not(String.IsNullOrWhiteSpace e))
      |> ValueOption.defaultValue false

    let mutable field =
      TextField(
        Label = ValueOption.defaultValue null props.Label,
        Value = (if isNull props.Value then "" else props.Value),
        Controlled = true,
        Placeholder = ValueOption.defaultValue null props.Placeholder,
        Error = ValueOption.defaultValue null props.Error,
        Invalid = hasError,
        Password = props.Password,
        RevealButton = props.RevealButton,
        Disabled = props.Disabled,
        ReadOnly = props.ReadOnly,
        OnChange = Action<string>(onChange),
        OnSubmit =
          (onSubmit
           |> ValueOption.map(fun f -> Action<string>(f))
           |> ValueOption.defaultValue null),
        Width = ValueOption.toNullable props.Width,
        BorderRadius = Nullable 0.0,
        BorderWidth = Nullable 2.0,
        EntryHeight = Nullable(Length.Pixels 36.0),
        BackgroundColor = Nullable p.Background,
        BorderColor =
          Nullable(if theme.HighContrast then p.Foreground else p.BorderStrong),
        FocusColor = Nullable a.Base,
        TextColor = Nullable p.Foreground,
        MutedTextColor = Nullable p.ForegroundSecondary,
        InvalidColor = Nullable p.Danger,
        DisabledTextColor =
          Nullable(
            if theme.HighContrast then
              p.Foreground
            else
              p.ForegroundDisabled
          ),
        LabelFontSize = Nullable scaledFont,
        FontSize = Nullable scaledFont,
        FontFamily = theme.FontFamily,
        TransitionMs =
          Nullable(MetroTheme.effectiveMs(theme, MetroTiming.fastMs)),
        TransitionEasing = Nullable Easing.EaseOut
      )

    field.Build()

[<Struct; NoComparison>]
type MetroToggleSwitchProps = {
  Header: string voption
  Checked: bool
  Disabled: bool
  OnLabel: string
  OffLabel: string
  AccessibilityName: string voption
}

[<RequireQualifiedAccess>]
module MetroToggleSwitch =

  let private emptyShadows: BoxShadow array = Array.empty
  let private zeroRadius = Length.Pixels 0.0
  let private zeroBorder = Edges(Length.Pixels 0.0)
  let private metroBorder = Edges(Length.Pixels 2.0)

  let defaults(isChecked: bool) : MetroToggleSwitchProps = {
    Header = ValueNone
    Checked = isChecked
    Disabled = false
    OnLabel = "On"
    OffLabel = "Off"
    AccessibilityName = ValueNone
  }

  let build(props: MetroToggleSwitchProps, onChange: bool -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    let scaledFont = 14.0 * theme.TextScaleFactor

    let onLabel = if isNull props.OnLabel then "On" else props.OnLabel
    let offLabel = if isNull props.OffLabel then "Off" else props.OffLabel
    let stateLabel = if props.Checked then onLabel else offLabel

    let trackBg = if props.Checked then a.Base else Color.Transparent

    let trackBorder =
      if theme.HighContrast then p.Foreground
      elif props.Checked then a.Base
      else p.BorderStrong

    let thumbBg = if props.Checked then a.OnAccent else p.Foreground

    let thumb =
      Container(
        Width = Length.Pixels 12.0,
        Height = Length.Pixels 20.0,
        BorderRadius = zeroRadius,
        BoxShadows = emptyShadows,
        BackgroundColor = thumbBg
      )

    let track =
      Container(
        Width = Length.Pixels 44.0,
        Height = Length.Pixels 20.0,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent =
          (if props.Checked then
             JustifyContent.FlexEnd
           else
             JustifyContent.FlexStart),
        BorderRadius = zeroRadius,
        BoxShadows = emptyShadows,
        BorderWidth = metroBorder,
        BorderColor = Edges<Color>(trackBorder),
        BackgroundColor = trackBg,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut
      )
        .Children(thumb)

    let labelText =
      Text(
        Content = stateLabel,
        FontFamily = theme.FontFamily,
        FontSize = Length.Pixels scaledFont,
        Color = p.Foreground,
        TextWrap = TextWrap.NoWrap
      )

    let accName =
      props.AccessibilityName
      |> ValueOption.orElse props.Header
      |> ValueOption.defaultValue stateLabel

    let switchBtn =
      Button(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        Gap = Length.Pixels MetroSpacing.md,
        MinHeight = Length.Pixels 32.0,
        Padding = Edges(Length.Pixels 4.0, Length.Pixels 0.0),
        BackgroundColor = Color.Transparent,
        BorderRadius = zeroRadius,
        BorderWidth = zeroBorder,
        BoxShadows = emptyShadows,
        Cursor = (if props.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not props.Disabled,
        TabStop = not props.Disabled,
        Disabled = props.Disabled,
        DisabledStyle =
          Style(Opacity = (if theme.HighContrast then 1.0 else 0.4)),
        FocusStyle =
          Style(
            OutlineWidth = Length.Pixels 2.0,
            OutlineColor = p.Foreground,
            OutlineOffset = Length.Pixels 2.0
          ),
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Switch,
            Name = accName,
            Checked =
              (if props.Checked then
                 AccessibilityChecked.True
               else
                 AccessibilityChecked.False)
          )
      )

    if not props.Disabled then
      let nextValue = not props.Checked
      let toggleAction = Action(fun () -> onChange nextValue)
      switchBtn.OnClick <- toggleAction

      switchBtn.KeyBindings <- [|
        KeyBinding(Key.Enter, KeyModifiers.None, toggleAction)
        KeyBinding(Key.Space, KeyModifiers.None, toggleAction)
      |]

    let interactiveRow = switchBtn.Children(track, labelText)

    props.Header
    |> ValueOption.map(fun header ->
      let headerText =
        Text(
          Content = (if isNull header then "" else header),
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels scaledFont,
          Color = p.Foreground
        )

      Container(
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels MetroSpacing.xs
      )
        .Children(headerText, interactiveRow)
      :> Blob)
    |> ValueOption.defaultValue(interactiveRow :> Blob)

[<Struct; NoComparison>]
type MetroCheckBoxProps = {
  Label: string
  Checked: bool
  Disabled: bool
}

[<RequireQualifiedAccess>]
module MetroCheckBox =

  let build(props: MetroCheckBoxProps, onChange: bool -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent

    let borderColor =
      if theme.HighContrast then p.Foreground else p.BorderStrong

    let checkedBorder = if theme.HighContrast then p.Foreground else a.Base

    let mutable cb =
      Checkbox(
        Value =
          (if props.Checked then
             AccessibilityChecked.True
           else
             AccessibilityChecked.False),
        Disabled = props.Disabled,
        Label = (if isNull props.Label then "" else props.Label),
        OnChange =
          Action<AccessibilityChecked>(fun state ->
            onChange(state = AccessibilityChecked.True)),
        BorderRadius = Nullable 0.0,
        BorderWidth = Nullable 2.0,
        Size = Nullable 20.0,
        BackgroundColor = Nullable Color.Transparent,
        CheckedBackgroundColor = Nullable a.Base,
        BorderColor = Nullable borderColor,
        CheckedBorderColor = Nullable checkedBorder,
        MarkColor = Nullable a.OnAccent,
        LabelColor = Nullable p.Foreground,
        LabelFontSize = Nullable(14.0 * theme.TextScaleFactor),
        DisabledOpacity = Nullable(if theme.HighContrast then 1.0 else 0.4),
        TransitionMs =
          Nullable(MetroTheme.effectiveMs(theme, MetroTiming.fastMs)),
        TransitionEasing = Nullable Easing.EaseOut
      )

    cb.Build()

[<Struct; NoComparison>]
type MetroRadioButtonProps = {
  Label: string
  Selected: bool
  Disabled: bool
}

[<RequireQualifiedAccess>]
module MetroRadioButton =

  let private metroBorder = Edges(Length.Pixels 2.0)
  let private zeroRadius = Length.Pixels 0.0

  let build(props: MetroRadioButtonProps, onSelect: unit -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent

    let borderColor =
      if theme.HighContrast then p.Foreground
      elif props.Selected then a.Base
      else p.BorderStrong

    let mutable rb =
      RadioButton(
        Value = props.Selected,
        Label = (if isNull props.Label then "" else props.Label),
        Disabled = props.Disabled,
        OnChange =
          Action<bool>(fun selected ->
            if selected then
              onSelect()),
        ConfigureRoot =
          Action<RadioButton, Button>(fun _ root ->
            root.BorderRadius <- zeroRadius

            root.TransitionMs <-
              MetroTheme.effectiveMs(theme, MetroTiming.fastMs)

            root.TransitionEasing <- Easing.EaseOut

            let rootChildren = root.Children

            if rootChildren.Count > 0 then
              match rootChildren[0] with
              | :? Container as indicator ->
                indicator.BorderWidth <- metroBorder
                indicator.BorderColor <- Edges<Color>(borderColor)
                let indicatorChildren = indicator.Children

                if props.Selected && indicatorChildren.Count > 0 then
                  match indicatorChildren[0] with
                  | :? Container as dot -> dot.BackgroundColor <- a.Base
                  | _ -> ()
              | _ -> ())
      )

    rb.Build()

[<Struct; NoComparison>]
type MetroSliderProps = {
  Label: string voption
  Value: float
  Min: float
  Max: float
  Step: float
  Disabled: bool
}

[<RequireQualifiedAccess>]
module MetroSlider =

  let defaults
    (value: float, minValue: float, maxValue: float)
    : MetroSliderProps = {
    Label = ValueNone
    Value = value
    Min = minValue
    Max = maxValue
    Step = 0.0
    Disabled = false
  }

  let mount
    (key: string, props: MetroSliderProps, onChange: Action<float>)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent

    let input =
      SliderInput(
        Value = props.Value,
        Minimum = Nullable props.Min,
        Maximum = Nullable props.Max,
        Step = (if props.Step > 0.0 then Nullable props.Step else Nullable()),
        Label = ValueOption.defaultValue null props.Label,
        Disabled = props.Disabled,
        OnChange = onChange,
        ThumbRadius = Nullable 0.0,
        TrackRadius = Nullable 0.0,
        TrackThickness = Nullable 4.0,
        ThumbSize = Nullable 16.0,
        ThumbBorderWidth = Nullable 0.0,
        FillColor = Nullable a.Base,
        ThumbColor = Nullable a.Base,
        TrackColor =
          Nullable(if theme.HighContrast then p.Foreground else p.Border),
        LabelColor = Nullable p.Foreground,
        ValueColor = Nullable p.ForegroundSecondary,
        LabelFontSize = Nullable(14.0 * theme.TextScaleFactor),
        FocusOutlineColor = Nullable p.Foreground,
        DisabledOpacity = Nullable(if theme.HighContrast then 1.0 else 0.4)
      )

    Cell.Mount<SliderInput, Slider>(key, input)
