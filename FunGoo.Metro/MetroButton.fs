namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct>]
type MetroButtonVariant =
  | Standard
  | Accent
  | Hyperlink
  | Subtle

[<Struct; NoComparison>]
type MetroButtonProps = {
  Content: string
  Variant: MetroButtonVariant
  Disabled: bool
  Pressed: bool voption
  AccessibilityName: string voption
  MinWidth: float voption
}

[<RequireQualifiedAccess>]
module MetroButton =

  [<Struct; NoComparison>]
  type VariantVisuals = {
    Background: Color
    Foreground: Color
    BorderWidth: Edges
    BorderColor: Edges<Color>
    MinHeight: float
    Padding: Edges
    FontWeight: float
    HoverStyle: Style
    ActiveStyle: Style
  }

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let metroBorder = Edges(Length.Pixels 2.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let pressScaleTransform = PanelTransform(Scale = 0.985)

  let defaults(content: string) : MetroButtonProps = {
    Content = content
    Variant = MetroButtonVariant.Standard
    Disabled = false
    Pressed = ValueNone
    AccessibilityName = ValueNone
    MinWidth = ValueNone
  }

  let build(props: MetroButtonProps, onClick: unit -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    let content = if isNull props.Content then "" else props.Content
    let isPressed = props.Pressed = ValueSome true
    let accentHover = MetroTheme.accentTextHover theme

    let visuals =
      match props.Variant with
      | MetroButtonVariant.Standard ->
        let border = if theme.HighContrast then p.Foreground else p.BorderStrong
        let active = Style(BackgroundColor = p.Foreground, Color = p.Background)

        if not theme.ReducedMotion then
          active.Transform <- pressScaleTransform

        {
          Background = if isPressed then p.Highlight else Color.Transparent
          Foreground = p.Foreground
          BorderWidth = metroBorder
          BorderColor = Edges<Color>(border)
          MinHeight = 36.0
          Padding = Edges(Length.Pixels 8.0, Length.Pixels 16.0)
          FontWeight = 600.0
          HoverStyle = Style(BackgroundColor = p.Hover)
          ActiveStyle = active
        }
      | MetroButtonVariant.Accent ->
        let border = if theme.HighContrast then p.Foreground else a.Base

        let hoverBorder =
          if theme.HighContrast then p.Foreground else accentHover

        let activeBorder = if theme.HighContrast then p.Foreground else a.Dark

        let active =
          Style(
            BackgroundColor = a.Dark,
            BorderColor = Edges<Color>(activeBorder)
          )

        if not theme.ReducedMotion then
          active.Transform <- pressScaleTransform

        {
          Background = a.Base
          Foreground = a.OnAccent
          BorderWidth = metroBorder
          BorderColor = Edges<Color>(border)
          MinHeight = 36.0
          Padding = Edges(Length.Pixels 8.0, Length.Pixels 16.0)
          FontWeight = 600.0
          HoverStyle =
            Style(
              BackgroundColor = accentHover,
              BorderColor = Edges<Color>(hoverBorder)
            )
          ActiveStyle = active
        }
      | MetroButtonVariant.Hyperlink ->
        let hover =
          Style(Color = accentHover, TextDecoration = TextDecoration.Underline)

        let active =
          Style(Color = a.Dark, TextDecoration = TextDecoration.Underline)

        {
          Background = Color.Transparent
          Foreground = a.Base
          BorderWidth = zeroBorder
          BorderColor = transparentBorder
          MinHeight = 32.0
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 0.0)
          FontWeight = 400.0
          HoverStyle = hover
          ActiveStyle = active
        }
      | MetroButtonVariant.Subtle -> {
          Background = if isPressed then p.Highlight else Color.Transparent
          Foreground = p.Foreground
          BorderWidth = zeroBorder
          BorderColor = transparentBorder
          MinHeight = 36.0
          Padding = Edges(Length.Pixels 8.0, Length.Pixels 12.0)
          FontWeight = 600.0
          HoverStyle = Style(BackgroundColor = p.Hover)
          ActiveStyle = Style(BackgroundColor = p.Active)
        }

    let accName = props.AccessibilityName |> ValueOption.defaultValue content

    let accChecked =
      props.Pressed
      |> ValueOption.map(fun pressed ->
        if pressed then
          AccessibilityChecked.True
        else
          AccessibilityChecked.False)
      |> ValueOption.defaultValue AccessibilityChecked.Unspecified

    let disabledColor =
      match props.Variant with
      | MetroButtonVariant.Accent -> a.OnAccent
      | _ -> p.Foreground

    let disabledStyle =
      Style(
        Color = disabledColor,
        Opacity = (if theme.HighContrast then 1.0 else 0.4)
      )

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let btn =
      Button(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        MinHeight = Length.Pixels visuals.MinHeight,
        Padding = visuals.Padding,
        BorderRadius = zeroRadius,
        BoxShadows = emptyShadows,
        BorderWidth = visuals.BorderWidth,
        BorderColor = visuals.BorderColor,
        BackgroundColor = visuals.Background,
        Color = visuals.Foreground,
        FontFamily = theme.FontFamily,
        FontSize =
          Length.Pixels(
            MetroTypography.size MetroTextRole.Body * theme.TextScaleFactor
          ),
        FontWeight = visuals.FontWeight,
        HoverStyle = visuals.HoverStyle,
        ActiveStyle = visuals.ActiveStyle,
        DisabledStyle = disabledStyle,
        FocusStyle = focusStyle,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if props.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not props.Disabled,
        TabStop = not props.Disabled,
        Disabled = props.Disabled,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Button,
            Name = accName,
            Checked = accChecked
          )
      )

    props.MinWidth |> ValueOption.iter(fun w -> btn.MinWidth <- Length.Pixels w)

    if not props.Disabled then
      let clickAction = Action(onClick)
      btn.OnClick <- clickAction

      btn.KeyBindings <- [|
        KeyBinding(Key.Enter, KeyModifiers.None, clickAction)
        KeyBinding(Key.Space, KeyModifiers.None, clickAction)
      |]

    btn.Children(Text(Content = content, TextWrap = TextWrap.NoWrap))

  let inline standard(content: string, onClick: unit -> unit) : Blob =
    build(defaults content, onClick)

  let inline accent(content: string, onClick: unit -> unit) : Blob =
    build(
      {
        defaults content with
            Variant = MetroButtonVariant.Accent
      },
      onClick
    )

  let inline hyperlink(content: string, onClick: unit -> unit) : Blob =
    build(
      {
        defaults content with
            Variant = MetroButtonVariant.Hyperlink
      },
      onClick
    )

  let inline subtle(content: string, onClick: unit -> unit) : Blob =
    build(
      {
        defaults content with
            Variant = MetroButtonVariant.Subtle
      },
      onClick
    )
