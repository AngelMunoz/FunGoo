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

[<AbstractClass>]
type MetroSettingsFlyoutCell() =
  inherit Cell<MetroSettingsFlyoutProps>()

  abstract IsVisible: bool

  abstract IsAnimating: bool

  abstract PanelTranslate: Anim<Point>

  abstract BackdropOpacity: Anim<float>

  abstract View: Blob

  // Gives a dismiss request of the host to the host cell, as a Goo.Widgets
  // control does. The owner rule then rebuilds the host cell.
  member this.Raise(callback: Action) = this.InvokeInputCallback(callback)

[<Struct; NoComparison>]
type MetroContentDialogHandlers = {
  OnPrimary: unit -> unit
  OnSecondary: unit -> unit
  OnDismiss: unit -> unit
  RenderContent: unit -> Blob
}

[<Struct; NoComparison>]
type MetroContentDialogAction =
  | Primary
  | Secondary
  | Dismiss

[<Struct; NoComparison>]
type MetroContentDialogProps = {
  Open: bool
  Title: string
  PrimaryText: string voption
  SecondaryText: string voption
  Dismissible: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroContentDialogCell() =
  inherit Cell<MetroContentDialogProps>()

  abstract IsVisible: bool

  abstract IsAnimating: bool

  abstract Mode: MetroTiltMode

  abstract IsShaderAttached: bool

  abstract ProgressAnim: Anim<float>

  abstract BackdropOpacity: Anim<float>

  abstract View: Blob

  abstract Handle: action: MetroContentDialogAction -> unit

  // Gives a dialog action of the host to the host cell, as a Goo.Widgets
  // control does. The owner rule then rebuilds the host cell.
  member this.Raise
    (
      callback: Action<MetroContentDialogAction>,
      action: MetroContentDialogAction
    ) =
    this.InvokeInputCallback(callback, action)

module OverlayInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPoint = Point(X = 0.0, Y = 0.0)
  let dialogSpec = MetroMotion.decelerate 280.0

  let resolveFlyoutWidth(width: float) : float =
    if Double.IsFinite width && width > 0.0 then
      width
    else
      346.0

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

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

  let flyoutPanel
    (
      theme: MetroTheme,
      props: MetroSettingsFlyoutProps,
      dismissAction: Action,
      renderContent: unit -> Blob
    ) : Container =
    let p = theme.Palette
    let a = theme.Accent
    let width = resolveFlyoutWidth props.Width
    let title = if isNull props.Title then "" else props.Title
    let accName = props.AccessibilityName |> ValueOption.defaultValue title

    let backIcon = MaterialIcons.Create("arrow_back", 18.0, Nullable a.OnAccent)

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
        BoxShadows = emptyShadows,
        BackgroundColor = Color.Transparent,
        Color = a.OnAccent,
        HoverStyle = Style(BackgroundColor = MetroTheme.accentTextHover theme),
        ActiveStyle = Style(BackgroundColor = a.Dark),
        FocusStyle = focusOutline a.OnAccent,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = Cursor.Pointer,
        Focusable = true,
        TabStop = true,
        OnClick = dismissAction,
        KeyBindings = keyActivateBindings dismissAction,
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
        BorderRadius = zeroRadius,
        BoxShadows = emptyShadows
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

    Container(
      Width = Length.Pixels width,
      Height = Length.Percent 100.0,
      FlexDirection = FlexDirection.Column,
      FlexShrink = 0.0,
      BackgroundColor = p.BackgroundAlt,
      Color = p.Foreground,
      BorderRadius = zeroRadius,
      BoxShadows = emptyShadows,
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

  let dialogBand
    (
      theme: MetroTheme,
      props: MetroContentDialogProps,
      onPrimary: unit -> unit,
      onSecondary: unit -> unit,
      renderContent: unit -> Blob
    ) : Container =
    let p = theme.Palette
    let a = theme.Accent
    let title = if isNull props.Title then "" else props.Title
    let accName = props.AccessibilityName |> ValueOption.defaultValue title
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

    Container(
      Width = Length.Percent 100.0,
      FlexDirection = FlexDirection.Column,
      AlignItems = AlignItems.Center,
      BackgroundColor = p.BackgroundAlt,
      Color = p.Foreground,
      BorderRadius = zeroRadius,
      BoxShadows = emptyShadows,
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

      let scrim =
        if theme.IsDark then
          Color.Rgba(0, 0, 0, 140)
        else
          Color.Rgba(0, 0, 0, 96)

      let dismissAction = Action(onDismiss)

      let panel =
        OverlayInternals.flyoutPanel(theme, props, dismissAction, renderContent)

      let portal =
        Portal(
          FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
          Scrim = Nullable scrim,
          ZIndex = 50,
          OnDismiss = Action<PopupDismissReason>(fun _ -> onDismiss()),
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Row,
          JustifyContent = JustifyContent.FlexEnd,
          AlignItems = AlignItems.Stretch
        )

      portal.Add(panel)
      portal

  type FlyoutState = {
    mutable Input: MetroSettingsFlyoutProps
    mutable LastOpen: bool voption
    mutable Visible: bool
    mutable Closing: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable PanelTranslate: Anim<Point>
    mutable BackdropOpacity: Anim<float>
  }

  type FlyoutContext = {
    State: FlyoutState
    Dismiss: Action
    DismissPortal: Action<PopupDismissReason>
    DismissPointer: Action<PointerEvent>
    RenderContent: unit -> Blob
  }

  let initialState() : FlyoutState = {
    Input = defaults("", false)
    LastOpen = ValueNone
    Visible = false
    Closing = false
    LastReducedMotion = false
    ScopedReducedMotion = false
    PanelTranslate = Unchecked.defaultof<Anim<Point>>
    BackdropOpacity = Unchecked.defaultof<Anim<float>>
  }

  let snapFlyout(state: FlyoutState, isOpen: bool) : unit =
    let width = OverlayInternals.resolveFlyoutWidth state.Input.Width
    state.Visible <- isOpen
    state.Closing <- false

    if isOpen then
      state.PanelTranslate.Snap(OverlayInternals.zeroPoint)
      state.BackdropOpacity.Snap(1.0)
    else
      state.PanelTranslate.Snap(Point(X = width, Y = 0.0))
      state.BackdropOpacity.Snap(0.0)

  let inputChanged
    (
      state: FlyoutState,
      input: MetroSettingsFlyoutProps,
      hostReduced: bool voption
    ) : unit =
    state.Input <- input

    let reducedMotion =
      state.ScopedReducedMotion
      || ValueOption.defaultValue state.LastReducedMotion hostReduced

    match state.LastOpen with
    | ValueNone -> snapFlyout(state, input.Open)
    | ValueSome wasOpen ->
      if input.Open && (not wasOpen || state.Closing) then
        if reducedMotion then
          snapFlyout(state, true)
        else
          let width = OverlayInternals.resolveFlyoutWidth input.Width
          let wasVisible = state.Visible
          state.Visible <- true
          state.Closing <- false

          if wasVisible then
            state.PanelTranslate.To(
              OverlayInternals.zeroPoint,
              MetroMotion.slow
            )

            state.BackdropOpacity.To(1.0, MetroMotion.slow)
          else
            state.PanelTranslate.Play(
              Point(X = width, Y = 0.0),
              OverlayInternals.zeroPoint,
              MetroMotion.slow
            )

            state.BackdropOpacity.Play(0.0, 1.0, MetroMotion.slow)
      elif not input.Open && wasOpen then
        if reducedMotion then
          snapFlyout(state, false)
        else
          let width = OverlayInternals.resolveFlyoutWidth input.Width
          state.Visible <- true
          state.Closing <- true

          state.PanelTranslate.To(Point(X = width, Y = 0.0), MetroMotion.slow)
          state.BackdropOpacity.To(0.0, MetroMotion.slow)

    state.LastOpen <- ValueSome input.Open

  let unmount(state: FlyoutState) : unit =
    state.LastOpen <- ValueNone
    state.ScopedReducedMotion <- false
    snapFlyout(state, false)

  let view(ctx: FlyoutContext, theme: MetroTheme) : Blob =
    let state = ctx.State

    if not state.Visible then
      Container(Display = Display.None)
    else
      let scrim =
        if theme.IsDark then
          Color.Rgba(0, 0, 0, 140)
        else
          Color.Rgba(0, 0, 0, 96)

      let backdrop =
        Container(
          Position = PositionType.Absolute,
          Inset = Edges(Length.Pixels 0.0),
          BackgroundColor = scrim,
          OpacityAnim = state.BackdropOpacity,
          OnPointerDown = ctx.DismissPointer
        )

      let panel =
        OverlayInternals.flyoutPanel(
          theme,
          state.Input,
          ctx.Dismiss,
          ctx.RenderContent
        )

      panel.TranslateAnim <- state.PanelTranslate

      let portal =
        Portal(
          FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
          Scrim = Nullable Color.Transparent,
          ZIndex = 50,
          OnDismiss = ctx.DismissPortal,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Row,
          JustifyContent = JustifyContent.FlexEnd,
          AlignItems = AlignItems.Stretch
        )

      portal.Add(backdrop)
      portal.Add(panel)
      portal

  let buildCell
    (
      ctx: FlyoutContext,
      input: MetroSettingsFlyoutProps,
      hostReduced: bool voption
    ) : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if
      theme.ReducedMotion && (state.Closing || state.PanelTranslate.Running)
    then
      snapFlyout(state, input.Open)

    view(ctx, theme)

  let create
    (onDismiss: unit -> unit, renderContent: unit -> Blob)
    : MetroSettingsFlyoutCell =
    let state = initialState()
    let mutable self: MetroSettingsFlyoutCell voption = ValueNone
    let onDismissAction = Action(onDismiss)

    let requestDismiss() =
      if not state.Closing then
        self |> ValueOption.iter(fun cell -> cell.Raise(onDismissAction))

    let dismiss = Action(requestDismiss)

    let dismissPortal = Action<PopupDismissReason>(fun _ -> requestDismiss())

    let dismissPointer = Action<PointerEvent>(fun _ -> requestDismiss())

    let ctx: FlyoutContext = {
      State = state
      Dismiss = dismiss
      DismissPortal = dismissPortal
      DismissPointer = dismissPointer
      RenderContent = renderContent
    }

    let cell = {
      new MetroSettingsFlyoutCell() with
        override _.IsVisible = state.Visible

        override _.IsAnimating =
          state.PanelTranslate.Running || state.BackdropOpacity.Running

        override _.PanelTranslate = state.PanelTranslate
        override _.BackdropOpacity = state.BackdropOpacity
        override _.View = view(ctx, Metro.read())

        override this.InputChanged(input) =
          inputChanged(state, input, OverlayInternals.reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          buildCell(ctx, input, OverlayInternals.reducedMotionOf this.Host)
    }

    state.PanelTranslate <- cell.AnimateValue(OverlayInternals.zeroPoint)
    state.BackdropOpacity <- cell.AnimateValue(0.0)

    // Unmounting the Portal after the close slide settles runs from the motion
    // pump, not a Blob callback, so the cell rebuilds itself once.
    state.PanelTranslate.add_Completed(
      Action<MotionCompletionReason>(fun _ ->
        if state.Closing && not state.Input.Open then
          state.Visible <- false
          state.Closing <- false
          cell.Rebuild())
    )

    self <- ValueSome cell
    cell

  let createFactory
    (onDismiss: unit -> unit, renderContent: unit -> Blob)
    : unit -> MetroSettingsFlyoutCell =
    fun () -> create(onDismiss, renderContent)

  let mount
    (
      key: string,
      input: MetroSettingsFlyoutProps,
      factory: unit -> MetroSettingsFlyoutCell
    ) : Blob =
    Cell.Mount<MetroSettingsFlyoutProps, MetroSettingsFlyoutCell>(
      key,
      input,
      factory
    )

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

  let transitionSpec = OverlayInternals.dialogSpec

  let build
    (props: MetroContentDialogProps, handlers: MetroContentDialogHandlers)
    : Blob =
    if not props.Open then
      Container(Display = Display.None)
    else
      let theme = Metro.read()

      let scrim =
        if theme.IsDark then
          Color.Rgba(0, 0, 0, 165)
        else
          Color.Rgba(0, 0, 0, 115)

      let band =
        OverlayInternals.dialogBand(
          theme,
          props,
          handlers.OnPrimary,
          handlers.OnSecondary,
          handlers.RenderContent
        )

      let portal =
        Portal(
          FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
          Scrim = Nullable scrim,
          ZIndex = 50,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          JustifyContent = JustifyContent.Center,
          AlignItems = AlignItems.Stretch
        )

      if props.Dismissible then
        portal.OnDismiss <-
          Action<PopupDismissReason>(fun _ -> handlers.OnDismiss())

      portal.Add(band)
      portal

  type DialogState = {
    Effect: ShaderEffect voption
    mutable Input: MetroContentDialogProps
    mutable LastOpen: bool voption
    mutable Visible: bool
    mutable Entering: bool
    mutable Closing: bool
    mutable ShaderAttached: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable Progress: Anim<float>
    mutable BackdropOpacity: Anim<float>
    mutable BandOpacity: Anim<float>
    mutable BandScale: Anim<float>
    mutable BandTranslate: Anim<Point>
  }

  type DialogContext = {
    State: DialogState
    OnPrimary: unit -> unit
    OnSecondary: unit -> unit
    DismissPortal: Action<PopupDismissReason>
    DismissPointer: Action<PointerEvent>
    RenderContent: unit -> Blob
  }

  let initialState(effect: ShaderEffect voption) : DialogState = {
    Effect = effect
    Input = defaults("", false)
    LastOpen = ValueNone
    Visible = false
    Entering = true
    Closing = false
    ShaderAttached = false
    LastReducedMotion = false
    ScopedReducedMotion = false
    Progress = Unchecked.defaultof<Anim<float>>
    BackdropOpacity = Unchecked.defaultof<Anim<float>>
    BandOpacity = Unchecked.defaultof<Anim<float>>
    BandScale = Unchecked.defaultof<Anim<float>>
    BandTranslate = Unchecked.defaultof<Anim<Point>>
  }

  let snapDialog(state: DialogState, isOpen: bool) : unit =
    state.Visible <- isOpen
    state.Entering <- isOpen
    state.Closing <- false
    state.ShaderAttached <- false

    state.Effect
    |> ValueOption.iter(fun eff ->
      MetroPerspective.applyParams(eff, MetroPerspective.identity))

    if isOpen then
      state.Progress.Snap(1.0)
      state.BackdropOpacity.Snap(1.0)
      state.BandOpacity.Snap(1.0)
      state.BandScale.Snap(1.0)
      state.BandTranslate.Snap(OverlayInternals.zeroPoint)
    else
      state.Progress.Snap(0.0)
      state.BackdropOpacity.Snap(0.0)
      state.BandOpacity.Snap(0.0)
      state.BandScale.Snap(0.88)
      state.BandTranslate.Snap(Point(X = 0.0, Y = -20.0))

  let inputChanged
    (
      state: DialogState,
      input: MetroContentDialogProps,
      hostReduced: bool voption
    ) : unit =
    state.Input <- input

    let reducedMotion =
      state.ScopedReducedMotion
      || ValueOption.defaultValue state.LastReducedMotion hostReduced

    match state.LastOpen with
    | ValueNone -> snapDialog(state, input.Open)
    | ValueSome wasOpen ->
      if input.Open && (not wasOpen || state.Closing) then
        if reducedMotion then
          snapDialog(state, true)
        else
          let wasVisible = state.Visible
          state.Visible <- true
          state.Entering <- true
          state.Closing <- false

          if wasVisible then
            state.BackdropOpacity.To(1.0, OverlayInternals.dialogSpec)
          else
            state.BackdropOpacity.Play(0.0, 1.0, OverlayInternals.dialogSpec)

          match state.Effect with
          | ValueSome eff ->
            state.ShaderAttached <- true
            state.BandOpacity.Snap(1.0)
            state.BandScale.Snap(1.0)
            state.BandTranslate.Snap(OverlayInternals.zeroPoint)

            MetroPerspective.applyParams(
              eff,
              MetroPerspective.dialogSwivel(0.0, true)
            )

            if wasVisible then
              // A reopen during the close animation continues from the
              // current value; Play would snap back to the start value.
              state.Progress.To(1.0, OverlayInternals.dialogSpec)
            else
              state.Progress.Play(0.0, 1.0, OverlayInternals.dialogSpec)
          | ValueNone ->
            state.ShaderAttached <- false

            if wasVisible then
              // A reopen during the close animation continues from the
              // current values; Play would snap to the start values.
              state.BandTranslate.To(
                OverlayInternals.zeroPoint,
                OverlayInternals.dialogSpec
              )

              state.BandScale.To(1.0, OverlayInternals.dialogSpec)
              state.BandOpacity.To(1.0, OverlayInternals.dialogSpec)
              state.Progress.To(1.0, OverlayInternals.dialogSpec)
            else
              state.BandTranslate.Play(
                Point(X = 0.0, Y = -20.0),
                OverlayInternals.zeroPoint,
                OverlayInternals.dialogSpec
              )

              state.BandScale.Play(0.88, 1.0, OverlayInternals.dialogSpec)
              state.BandOpacity.Play(0.0, 1.0, OverlayInternals.dialogSpec)
              state.Progress.Play(0.0, 1.0, OverlayInternals.dialogSpec)
      elif not input.Open && wasOpen then
        if reducedMotion then
          snapDialog(state, false)
        else
          state.Visible <- true
          state.Entering <- false
          state.Closing <- true
          state.BackdropOpacity.To(0.0, OverlayInternals.dialogSpec)

          match state.Effect with
          | ValueSome eff ->
            state.ShaderAttached <- true

            MetroPerspective.applyParams(
              eff,
              MetroPerspective.dialogSwivel(0.0, false)
            )

            state.Progress.Play(0.0, 1.0, OverlayInternals.dialogSpec)
          | ValueNone ->
            state.ShaderAttached <- false

            state.BandTranslate.To(
              Point(X = 0.0, Y = 20.0),
              OverlayInternals.dialogSpec
            )

            state.BandScale.To(0.88, OverlayInternals.dialogSpec)
            state.BandOpacity.To(0.0, OverlayInternals.dialogSpec)
            state.Progress.Play(0.0, 1.0, OverlayInternals.dialogSpec)

    state.LastOpen <- ValueSome input.Open

  let unmount(state: DialogState) : unit =
    state.LastOpen <- ValueNone
    state.ScopedReducedMotion <- false
    snapDialog(state, false)

  let view(ctx: DialogContext, theme: MetroTheme) : Blob =
    let state = ctx.State

    if not state.Visible then
      Container(Display = Display.None)
    else
      let scrim =
        if theme.IsDark then
          Color.Rgba(0, 0, 0, 165)
        else
          Color.Rgba(0, 0, 0, 115)

      let backdrop =
        Container(
          Position = PositionType.Absolute,
          Inset = Edges(Length.Pixels 0.0),
          BackgroundColor = scrim,
          OpacityAnim = state.BackdropOpacity
        )

      if state.Input.Dismissible then
        backdrop.OnPointerDown <- ctx.DismissPointer

      let band =
        OverlayInternals.dialogBand(
          theme,
          state.Input,
          ctx.OnPrimary,
          ctx.OnSecondary,
          ctx.RenderContent
        )

      band.OpacityAnim <- state.BandOpacity
      band.ScaleAnim <- state.BandScale
      band.TranslateAnim <- state.BandTranslate

      if state.ShaderAttached then
        state.Effect |> ValueOption.iter(fun eff -> band.ShaderEffect <- eff)

      let portal =
        Portal(
          FocusScope = FocusScopeOptions(Modal = true, RestoreFocus = true),
          Scrim = Nullable Color.Transparent,
          ZIndex = 50,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          JustifyContent = JustifyContent.Center,
          AlignItems = AlignItems.Stretch
        )

      if state.Input.Dismissible then
        portal.OnDismiss <- ctx.DismissPortal

      portal.Add(backdrop)
      portal.Add(band)
      portal

  let buildCell
    (
      ctx: DialogContext,
      input: MetroContentDialogProps,
      hostReduced: bool voption
    ) : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion && (state.Closing || state.Progress.Running) then
      snapDialog(state, input.Open)

    view(ctx, theme)

  let createWithEffect
    (
      effect: ShaderEffect voption,
      onAction: MetroContentDialogAction -> unit,
      renderContent: unit -> Blob
    ) : MetroContentDialogCell =
    let state = initialState effect
    let mutable self: MetroContentDialogCell voption = ValueNone
    let onActionDelegate = Action<MetroContentDialogAction>(onAction)

    let raiseAction(action: MetroContentDialogAction) =
      if not state.Closing then
        self
        |> ValueOption.iter(fun cell -> cell.Raise(onActionDelegate, action))

    let ctx: DialogContext = {
      State = state
      OnPrimary = fun () -> raiseAction MetroContentDialogAction.Primary
      OnSecondary = fun () -> raiseAction MetroContentDialogAction.Secondary
      DismissPortal =
        Action<PopupDismissReason>(fun _ ->
          raiseAction MetroContentDialogAction.Dismiss)
      DismissPointer =
        Action<PointerEvent>(fun _ ->
          raiseAction MetroContentDialogAction.Dismiss)
      RenderContent = renderContent
    }

    let cell = {
      new MetroContentDialogCell() with
        override _.IsVisible = state.Visible

        override _.IsAnimating =
          state.Progress.Running || state.BackdropOpacity.Running

        override _.Mode =
          if state.Effect.IsSome then
            MetroTiltMode.Perspective3D
          else
            MetroTiltMode.Fallback2D

        override _.IsShaderAttached = state.ShaderAttached
        override _.ProgressAnim = state.Progress
        override _.BackdropOpacity = state.BackdropOpacity
        override _.View = view(ctx, Metro.read())
        override _.Handle(action) = raiseAction action

        override this.InputChanged(input) =
          inputChanged(state, input, OverlayInternals.reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          buildCell(ctx, input, OverlayInternals.reducedMotionOf this.Host)
    }

    state.BackdropOpacity <- cell.AnimateValue(0.0)
    state.BandOpacity <- cell.AnimateValue(0.0)
    state.BandScale <- cell.AnimateValue(1.0)
    state.BandTranslate <- cell.AnimateValue(OverlayInternals.zeroPoint)

    match state.Effect with
    | ValueSome eff ->
      MetroPerspective.applyParams(eff, MetroPerspective.identity)

      state.Progress <-
        cell.Animate(
          0.0,
          fun p ->
            MetroPerspective.applyParams(
              eff,
              MetroPerspective.dialogSwivel(p, state.Entering)
            )
        )
    | ValueNone -> state.Progress <- cell.AnimateValue(0.0)

    // Unmounting the Portal after close or detaching the 3D shader at rest
    // runs from the motion pump, not a Blob callback, so the cell rebuilds
    // itself once.
    state.Progress.add_Completed(
      Action<MotionCompletionReason>(fun _ ->
        if state.Closing && not state.Input.Open then
          state.Visible <- false
          state.Closing <- false
          state.ShaderAttached <- false

          state.Effect
          |> ValueOption.iter(fun eff ->
            MetroPerspective.applyParams(eff, MetroPerspective.identity))

          cell.Rebuild()
        elif state.ShaderAttached then
          state.ShaderAttached <- false

          state.Effect
          |> ValueOption.iter(fun eff ->
            MetroPerspective.applyParams(eff, MetroPerspective.identity))

          cell.Rebuild())
    )

    self <- ValueSome cell
    cell

  let create
    (onAction: MetroContentDialogAction -> unit, renderContent: unit -> Blob)
    : MetroContentDialogCell =
    createWithEffect(
      MetroPerspective.tryCreateDefault(),
      onAction,
      renderContent
    )

  let createFactory
    (onAction: MetroContentDialogAction -> unit, renderContent: unit -> Blob)
    : unit -> MetroContentDialogCell =
    fun () -> create(onAction, renderContent)

  let mount
    (
      key: string,
      input: MetroContentDialogProps,
      factory: unit -> MetroContentDialogCell
    ) : Blob =
    Cell.Mount<MetroContentDialogProps, MetroContentDialogCell>(
      key,
      input,
      factory
    )
