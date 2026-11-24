namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroAppBarButtonProps = {
  Label: string
  IconName: string
  Compact: bool
  MenuItem: bool
  Disabled: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroAppBarToggleProps = {
  Label: string
  IconName: string
  Checked: bool
  Compact: bool
  MenuItem: bool
  Disabled: bool
  AccessibilityName: string voption
}

module AppBarPartInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)

  [<Struct; NoComparison>]
  type CircleButtonArgs = {
    Label: string
    IconName: string
    Checked: bool voption
    Compact: bool
    MenuItem: bool
    Disabled: bool
    AccessibilityName: string voption
  }

  let buildCircleButton
    (args: CircleButtonArgs, onActivate: unit -> unit)
    : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let labelText = if isNull args.Label then "" else args.Label
    let a11yName = args.AccessibilityName |> ValueOption.defaultValue labelText
    let isChecked = args.Checked |> ValueOption.defaultValue false

    let fgColor = p.Foreground

    let ringFill =
      if isChecked && not args.Disabled then
        theme.Accent.Base
      else
        Color.Transparent

    let ringBorderColor =
      if args.Disabled then p.ForegroundDisabled
      elif isChecked then theme.Accent.Base
      else p.Foreground

    let iconColor =
      if isChecked && not args.Disabled then
        theme.Accent.OnAccent
      else
        fgColor

    let ringSize = if args.MenuItem then 26.0 else 40.0
    let iconSize = if args.MenuItem then 15.0 else 20.0
    let ringBorderPx = if args.MenuItem then 1.5 else 2.0

    let circle =
      Container(
        Width = Length.Pixels ringSize,
        Height = Length.Pixels ringSize,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexShrink = 0.0,
        BorderRadius = Length.Pixels(ringSize * 0.5),
        BorderWidth = Edges(Length.Pixels ringBorderPx),
        BorderColor = Edges<Color>(ringBorderColor),
        BoxShadows = emptyShadows,
        BackgroundColor = ringFill
      )

    if not(String.IsNullOrWhiteSpace args.IconName) then
      circle.Add(MaterialIcons.Create(args.IconName, iconSize, iconColor))

    let activate =
      Action(fun () ->
        if not args.Disabled then
          onActivate())

    let a11y =
      match args.Checked with
      | ValueSome chk ->
        Accessibility(
          Role = AccessibilityRole.Checkbox,
          Name = a11yName,
          Checked =
            if chk then
              AccessibilityChecked.True
            else
              AccessibilityChecked.False
        )
      | ValueNone ->
        Accessibility(Role = AccessibilityRole.Button, Name = a11yName)

    let btn =
      Button(
        FlexDirection =
          (if args.MenuItem then
             FlexDirection.Row
           else
             FlexDirection.Column),
        AlignItems = AlignItems.Center,
        JustifyContent =
          (if args.MenuItem then
             JustifyContent.FlexStart
           else
             JustifyContent.Center),
        MinWidth = Length.Pixels(if args.MenuItem then 140.0 else 56.0),
        MinHeight = Length.Pixels(if args.MenuItem then 40.0 else 48.0),
        Gap = Length.Pixels(if args.MenuItem then 12.0 else 4.0),
        Padding =
          (if args.MenuItem then
             Edges(Length.Pixels 8.0, Length.Pixels 14.0)
           else
             Edges(Length.Pixels 6.0, Length.Pixels 8.0)),
        BorderRadius = zeroRadius,
        BorderWidth = zeroBorder,
        BorderColor = transparentBorder,
        BoxShadows = emptyShadows,
        BackgroundColor = Color.Transparent,
        HoverStyle = Style(BackgroundColor = p.Hover),
        ActiveStyle = Style(BackgroundColor = p.Active),
        DisabledStyle =
          Style(
            Color = p.Foreground,
            Opacity = (if theme.HighContrast then 1.0 else 0.4)
          ),
        FocusStyle =
          Style(
            OutlineWidth = Length.Pixels 2.0,
            OutlineColor = p.Foreground,
            OutlineOffset = Length.Pixels 2.0
          ),
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if args.Disabled then Cursor.Default else Cursor.Pointer),
        Disabled = args.Disabled,
        Focusable = not args.Disabled,
        TabStop = not args.Disabled,
        OnClick = activate,
        KeyBindings =
          (if args.Disabled then
             Array.empty
           else
             [|
               KeyBinding(Key.Enter, KeyModifiers.None, activate)
               KeyBinding(Key.Space, KeyModifiers.None, activate)
             |]),
        Accessibility = a11y
      )
        .Children(circle)

    let showLabel =
      (args.MenuItem || not args.Compact)
      && not(String.IsNullOrWhiteSpace labelText)

    if showLabel then
      btn.Add(
        Text(
          Content = labelText,
          Color = fgColor,
          FontFamily = theme.FontFamily,
          FontSize =
            Length.Pixels(
              (if args.MenuItem then 14.0 else 12.0) * theme.TextScaleFactor
            ),
          FontWeight = 400.0,
          TextWrap = TextWrap.NoWrap
        )
      )

    btn :> Blob

[<RequireQualifiedAccess>]
module MetroAppBarButton =

  let defaults(label: string, iconName: string) : MetroAppBarButtonProps = {
    Label = label
    IconName = iconName
    Compact = false
    MenuItem = false
    Disabled = false
    AccessibilityName = ValueNone
  }

  let compact(label: string, iconName: string) : MetroAppBarButtonProps = {
    defaults(label, iconName) with
        Compact = true
  }

  let build(props: MetroAppBarButtonProps, onClick: unit -> unit) : Blob =
    AppBarPartInternals.buildCircleButton(
      {
        Label = props.Label
        IconName = props.IconName
        Checked = ValueNone
        Compact = props.Compact
        MenuItem = props.MenuItem
        Disabled = props.Disabled
        AccessibilityName = props.AccessibilityName
      },
      onClick
    )

[<RequireQualifiedAccess>]
module MetroAppBarToggleButton =

  let defaults
    (label: string, iconName: string, isChecked: bool)
    : MetroAppBarToggleProps = {
    Label = label
    IconName = iconName
    Checked = isChecked
    Compact = false
    MenuItem = false
    Disabled = false
    AccessibilityName = ValueNone
  }

  let build(props: MetroAppBarToggleProps, onChange: bool -> unit) : Blob =
    let nextState = not props.Checked

    AppBarPartInternals.buildCircleButton(
      {
        Label = props.Label
        IconName = props.IconName
        Checked = ValueSome props.Checked
        Compact = props.Compact
        MenuItem = props.MenuItem
        Disabled = props.Disabled
        AccessibilityName = props.AccessibilityName
      },
      fun () -> onChange nextState
    )

[<RequireQualifiedAccess>]
module MetroAppBarSeparator =

  let build(visible: bool) : Blob =
    let theme = Metro.read()
    let p = theme.Palette

    Container(
      Width = Length.Pixels 17.0,
      Height = Length.Pixels 48.0,
      FlexShrink = 0.0,
      AlignItems = AlignItems.Center,
      JustifyContent = JustifyContent.Center,
      Visibility = (if visible then Visibility.Visible else Visibility.Hidden),
      Accessibility =
        Accessibility(Role = AccessibilityRole.Separator, Hidden = not visible)
    )
      .Children(
        Container(
          Width = Length.Pixels 1.0,
          Height = Length.Pixels 28.0,
          BackgroundColor = p.Border
        )
      )
    :> Blob

  let standard() : Blob = build true
