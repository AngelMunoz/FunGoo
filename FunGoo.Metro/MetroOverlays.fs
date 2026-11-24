namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroAppBarCommand = {
  Id: string
  Label: string
  IconName: string
  Disabled: bool
}

[<Struct; NoComparison>]
type MetroAppBarProps = {
  Primary: MetroAppBarCommand array
  Secondary: MetroAppBarCommand array
  Expanded: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroSettingsFlyoutProps = {
  Open: bool
  Title: string
  Width: float
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroContentDialogProps = {
  Open: bool
  Title: string
  PrimaryText: string voption
  SecondaryText: string voption
  Dismissible: bool
  AccessibilityName: string voption
}

module OverlayInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)

  let focusOutline(fg: Color) : Style =
    Style(
      OutlineWidth = Length.Pixels 2.0,
      OutlineColor = fg,
      OutlineOffset = Length.Pixels 2.0
    )

  let keyActivateBindings(action: Action) : KeyBinding array = [|
    KeyBinding(Key.Enter, KeyModifiers.None, action)
    KeyBinding(Key.Space, KeyModifiers.None, action)
  |]

[<RequireQualifiedAccess>]
module MetroAppBar =

  let defaults(primary: MetroAppBarCommand array) : MetroAppBarProps = {
    Primary = primary
    Secondary = Array.empty
    Expanded = false
    AccessibilityName = ValueNone
  }

  let build
    (
      props: MetroAppBarProps,
      onCommand: string -> unit,
      onToggleExpanded: unit -> unit
    ) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let primary = if isNull props.Primary then Array.empty else props.Primary

    let secondary =
      if isNull props.Secondary then
        Array.empty
      else
        props.Secondary

    let accName = props.AccessibilityName |> ValueOption.defaultValue "App bar"
    let focusStyle = OverlayInternals.focusOutline p.Foreground
    let hoverStyle = Style(BackgroundColor = p.Hover)
    let activeStyle = Style(BackgroundColor = p.Active)
    let transitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs)

    let topBorderWidth =
      if theme.HighContrast then
        Edges(
          Length.Pixels 2.0,
          Length.Pixels 0.0,
          Length.Pixels 0.0,
          Length.Pixels 0.0
        )
      else
        OverlayInternals.zeroBorder

    let topBorderColor =
      if theme.HighContrast then
        Edges<Color>(p.Foreground)
      else
        OverlayInternals.transparentBorder

    let primaryStrip =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexGrow = 1.0,
        Gap = Length.Pixels 24.0
      )

    let disabledStyle =
      Style(
        Color = p.Foreground,
        Opacity = (if theme.HighContrast then 1.0 else 0.4)
      )

    for i = 0 to primary.Length - 1 do
      let cmd = primary[i]
      let cmdId = cmd.Id
      let label = if isNull cmd.Label then "" else cmd.Label
      let iconName = if isNull cmd.IconName then "circle" else cmd.IconName
      let fg = p.Foreground
      let icon = MaterialIcons.Create(iconName, 20.0, Nullable fg)

      let circle =
        Container(
          Width = Length.Pixels 40.0,
          Height = Length.Pixels 40.0,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          BorderRadius = Length.Pixels 20.0,
          BorderWidth = Edges(Length.Pixels 2.0),
          BorderColor = Edges<Color>(fg),
          BoxShadows = OverlayInternals.emptyShadows,
          BackgroundColor = Color.Transparent
        )
          .Children(icon)

      let caption =
        Text(
          Content = label,
          Color = fg,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
          FontWeight = 400.0,
          TextWrap = TextWrap.NoWrap
        )

      let cmdBtn =
        Button(
          Key = $"appbar-cmd-{cmdId}",
          FlexDirection = FlexDirection.Column,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          Gap = Length.Pixels 4.0,
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 8.0),
          BorderRadius = OverlayInternals.zeroRadius,
          BorderWidth = OverlayInternals.zeroBorder,
          BorderColor = OverlayInternals.transparentBorder,
          BoxShadows = OverlayInternals.emptyShadows,
          BackgroundColor = Color.Transparent,
          Color = fg,
          HoverStyle = hoverStyle,
          ActiveStyle = activeStyle,
          DisabledStyle = disabledStyle,
          FocusStyle = focusStyle,
          TransitionMs = transitionMs,
          TransitionEasing = Easing.EaseOut,
          Cursor = (if cmd.Disabled then Cursor.Default else Cursor.Pointer),
          Focusable = not cmd.Disabled,
          TabStop = not cmd.Disabled,
          Disabled = cmd.Disabled,
          Accessibility =
            Accessibility(Role = AccessibilityRole.Button, Name = label)
        )
          .Children(circle, caption)

      if not cmd.Disabled then
        let action = Action(fun () -> onCommand cmdId)
        cmdBtn.OnClick <- action
        cmdBtn.KeyBindings <- OverlayInternals.keyActivateBindings action

      primaryStrip.Add(cmdBtn)

    let primaryRow =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Width = Length.Percent 100.0,
        Padding = Edges(Length.Pixels 10.0, Length.Pixels 20.0),
        Gap = Length.Pixels 12.0
      )
        .Children(primaryStrip)

    if secondary.Length > 0 then
      let toggleAction = Action(onToggleExpanded)

      let moreBtn =
        Button(
          Width = Length.Pixels 40.0,
          Height = Length.Pixels 40.0,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          FlexShrink = 0.0,
          BorderRadius = OverlayInternals.zeroRadius,
          BorderWidth = OverlayInternals.zeroBorder,
          BorderColor = OverlayInternals.transparentBorder,
          BoxShadows = OverlayInternals.emptyShadows,
          BackgroundColor = Color.Transparent,
          Color = p.Foreground,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(20.0 * theme.TextScaleFactor),
          FontWeight = 700.0,
          HoverStyle = hoverStyle,
          ActiveStyle = activeStyle,
          FocusStyle = focusStyle,
          TransitionMs = transitionMs,
          TransitionEasing = Easing.EaseOut,
          Cursor = Cursor.Pointer,
          Focusable = true,
          TabStop = true,
          OnClick = toggleAction,
          KeyBindings = OverlayInternals.keyActivateBindings toggleAction,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.Button,
              Name = "More commands",
              Expanded = Nullable props.Expanded
            )
        )
          .Children(Text(Content = "...", TextWrap = TextWrap.NoWrap))

      primaryRow.Add(moreBtn)

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexShrink = 0.0,
        Width = Length.Percent 100.0,
        BackgroundColor = p.BackgroundAlt,
        BorderRadius = OverlayInternals.zeroRadius,
        BoxShadows = OverlayInternals.emptyShadows,
        BorderWidth = topBorderWidth,
        BorderColor = topBorderColor,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Toolbar, Name = accName)
      )
        .Children(primaryRow)

    if props.Expanded && secondary.Length > 0 then
      let secBorderWidth =
        Edges(
          Length.Pixels(if theme.HighContrast then 2.0 else 1.0),
          Length.Pixels 0.0,
          Length.Pixels 0.0,
          Length.Pixels 0.0
        )

      let secBorderColor =
        Edges<Color>(if theme.HighContrast then p.Foreground else p.Border)

      let secondaryList =
        Container(
          FlexDirection = FlexDirection.Column,
          Width = Length.Percent 100.0,
          Padding = Edges(Length.Pixels 8.0, Length.Pixels 20.0),
          Gap = Length.Pixels 4.0,
          BorderWidth = secBorderWidth,
          BorderColor = secBorderColor
        )

      for i = 0 to secondary.Length - 1 do
        let cmd = secondary[i]
        let cmdId = cmd.Id
        let label = if isNull cmd.Label then "" else cmd.Label
        let iconName = if isNull cmd.IconName then "circle" else cmd.IconName
        let fg = p.Foreground
        let icon = MaterialIcons.Create(iconName, 18.0, Nullable fg)

        let secBtn =
          Button(
            Key = $"appbar-sec-{cmdId}",
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.FlexStart,
            Width = Length.Percent 100.0,
            MinHeight = Length.Pixels 36.0,
            Gap = Length.Pixels 12.0,
            Padding = Edges(Length.Pixels 6.0, Length.Pixels 8.0),
            BorderRadius = OverlayInternals.zeroRadius,
            BorderWidth = OverlayInternals.zeroBorder,
            BorderColor = OverlayInternals.transparentBorder,
            BoxShadows = OverlayInternals.emptyShadows,
            BackgroundColor = Color.Transparent,
            Color = fg,
            HoverStyle = hoverStyle,
            ActiveStyle = activeStyle,
            DisabledStyle = disabledStyle,
            FocusStyle = focusStyle,
            TransitionMs = transitionMs,
            TransitionEasing = Easing.EaseOut,
            Cursor = (if cmd.Disabled then Cursor.Default else Cursor.Pointer),
            Focusable = not cmd.Disabled,
            TabStop = not cmd.Disabled,
            Disabled = cmd.Disabled,
            Accessibility =
              Accessibility(Role = AccessibilityRole.Button, Name = label)
          )
            .Children(
              icon,
              Text(
                Content = label,
                Color = fg,
                FontFamily = theme.FontFamily,
                FontSize = Length.Pixels(14.0 * theme.TextScaleFactor),
                FontWeight = 400.0,
                TextWrap = TextWrap.NoWrap
              )
            )

        if not cmd.Disabled then
          let action = Action(fun () -> onCommand cmdId)
          secBtn.OnClick <- action
          secBtn.KeyBindings <- OverlayInternals.keyActivateBindings action

        secondaryList.Add(secBtn)

      root.Add(secondaryList)

    root

[<RequireQualifiedAccess>]
module MetroSettingsFlyout =

  let defaults(title: string, isOpen: bool) : MetroSettingsFlyoutProps = {
    Open = isOpen
    Title = title
    Width = 346.0
    AccessibilityName = ValueNone
  }

  let build
    (
      props: MetroSettingsFlyoutProps,
      onDismiss: unit -> unit,
      renderContent: unit -> Blob
    ) : Blob =
    if not props.Open then
      Container(Display = Display.None)
    else
      let theme = Metro.read()
      let p = theme.Palette
      let a = theme.Accent

      let width =
        if Double.IsFinite props.Width && props.Width > 0.0 then
          props.Width
        else
          346.0

      let title = if isNull props.Title then "" else props.Title
      let accName = props.AccessibilityName |> ValueOption.defaultValue title

      let scrim =
        if theme.IsDark then
          Color.Rgba(0, 0, 0, 140)
        else
          Color.Rgba(0, 0, 0, 96)

      let dismissAction = Action(onDismiss)

      let backIcon =
        MaterialIcons.Create("arrow_back", 18.0, Nullable a.OnAccent)

      let backBtn =
        Button(
          Width = Length.Pixels 32.0,
          Height = Length.Pixels 32.0,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          FlexShrink = 0.0,
          BorderRadius = Length.Pixels 16.0,
          BorderWidth = Edges(Length.Pixels 2.0),
          BorderColor = Edges<Color>(a.OnAccent),
          BoxShadows = OverlayInternals.emptyShadows,
          BackgroundColor = Color.Transparent,
          Color = a.OnAccent,
          HoverStyle = Style(BackgroundColor = MetroTheme.accentTextHover theme),
          ActiveStyle = Style(BackgroundColor = a.Dark),
          FocusStyle = OverlayInternals.focusOutline a.OnAccent,
          TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
          TransitionEasing = Easing.EaseOut,
          Cursor = Cursor.Pointer,
          Focusable = true,
          TabStop = true,
          OnClick = dismissAction,
          KeyBindings = OverlayInternals.keyActivateBindings dismissAction,
          Accessibility =
            Accessibility(Role = AccessibilityRole.Button, Name = "Back")
        )
          .Children(backIcon)

      let headerBand =
        Container(
          FlexDirection = FlexDirection.Row,
          AlignItems = AlignItems.Center,
          FlexShrink = 0.0,
          Gap = Length.Pixels 12.0,
          Padding = Edges(Length.Pixels 24.0, Length.Pixels 20.0),
          BackgroundColor = a.Base,
          Color = a.OnAccent,
          BorderRadius = OverlayInternals.zeroRadius,
          BoxShadows = OverlayInternals.emptyShadows
        )
          .Children(
            backBtn,
            MetroText.build {
              MetroText.defaults(title, MetroTextRole.Subtitle) with
                  Tone = MetroTextTone.OnAccent
                  Wrap = TextWrap.NoWrap
            }
          )

      let bodyScroll =
        Container(
          FlexDirection = FlexDirection.Column,
          FlexGrow = 1.0,
          Width = Length.Percent 100.0,
          OverflowX = Overflow.Hidden,
          OverflowY = Overflow.Scroll,
          Padding = Edges(Length.Pixels 20.0),
          Gap = Length.Pixels 16.0
        )
          .Children(renderContent())

      let leftBorderWidth =
        Edges(
          Length.Pixels 0.0,
          Length.Pixels 0.0,
          Length.Pixels 0.0,
          Length.Pixels(if theme.HighContrast then 2.0 else 1.0)
        )

      let leftBorderColor =
        Edges<Color>(if theme.HighContrast then p.Foreground else p.Border)

      let panel =
        Container(
          Width = Length.Pixels width,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          FlexShrink = 0.0,
          BackgroundColor = p.BackgroundAlt,
          Color = p.Foreground,
          BorderRadius = OverlayInternals.zeroRadius,
          BoxShadows = OverlayInternals.emptyShadows,
          BorderWidth = leftBorderWidth,
          BorderColor = leftBorderColor,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.Dialog,
              Name = accName,
              Modal = Nullable true
            )
        )
          .Children(headerBand, bodyScroll)

      let portal =
        Portal(
          FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
          Scrim = Nullable scrim,
          OnDismiss = Action<PopupDismissReason>(fun _ -> onDismiss()),
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Row,
          JustifyContent = JustifyContent.FlexEnd,
          AlignItems = AlignItems.Stretch
        )

      portal.Add(panel)
      portal

[<RequireQualifiedAccess>]
module MetroContentDialog =

  let defaults(title: string, isOpen: bool) : MetroContentDialogProps = {
    Open = isOpen
    Title = title
    PrimaryText = ValueSome "OK"
    SecondaryText = ValueNone
    Dismissible = true
    AccessibilityName = ValueNone
  }

  let build
    (
      props: MetroContentDialogProps,
      onPrimary: unit -> unit,
      onSecondary: unit -> unit,
      onDismiss: unit -> unit,
      renderContent: unit -> Blob
    ) : Blob =
    if not props.Open then
      Container(Display = Display.None)
    else
      let theme = Metro.read()
      let p = theme.Palette
      let a = theme.Accent
      let title = if isNull props.Title then "" else props.Title
      let accName = props.AccessibilityName |> ValueOption.defaultValue title

      let scrim =
        if theme.IsDark then
          Color.Rgba(0, 0, 0, 165)
        else
          Color.Rgba(0, 0, 0, 115)

      let bandBorderColor = if theme.HighContrast then p.Foreground else a.Base

      let bandBorderWidth =
        Edges(
          Length.Pixels 2.0,
          Length.Pixels 0.0,
          Length.Pixels 2.0,
          Length.Pixels 0.0
        )

      let actionsRow =
        Container(
          FlexDirection = FlexDirection.Row,
          JustifyContent = JustifyContent.FlexEnd,
          AlignItems = AlignItems.Center,
          Gap = Length.Pixels 12.0,
          Padding =
            Edges(
              Length.Pixels 8.0,
              Length.Pixels 0.0,
              Length.Pixels 0.0,
              Length.Pixels 0.0
            )
        )

      props.PrimaryText
      |> ValueOption.iter(fun txt ->
        actionsRow.Add(MetroButton.accent(txt, onPrimary)))

      props.SecondaryText
      |> ValueOption.iter(fun txt ->
        actionsRow.Add(MetroButton.standard(txt, onSecondary)))

      let innerContent =
        Container(
          FlexDirection = FlexDirection.Column,
          Width = Length.Percent 100.0,
          MaxWidth = Length.Pixels 640.0,
          Gap = Length.Pixels 16.0
        )
          .Children(MetroText.header title, renderContent(), actionsRow)

      let band =
        Container(
          Width = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          AlignItems = AlignItems.Center,
          BackgroundColor = p.BackgroundAlt,
          Color = p.Foreground,
          BorderRadius = OverlayInternals.zeroRadius,
          BoxShadows = OverlayInternals.emptyShadows,
          BorderWidth = bandBorderWidth,
          BorderColor = Edges<Color>(bandBorderColor),
          Padding = Edges(Length.Pixels 28.0, Length.Pixels 32.0),
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.Dialog,
              Name = accName,
              Modal = Nullable true
            )
        )
          .Children(innerContent)

      let portal =
        Portal(
          FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
          Scrim = Nullable scrim,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          JustifyContent = JustifyContent.Center,
          AlignItems = AlignItems.Stretch
        )

      if props.Dismissible then
        portal.OnDismiss <- Action<PopupDismissReason>(fun _ -> onDismiss())

      portal.Add(band)
      portal
