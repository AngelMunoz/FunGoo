namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroTeachingTipEvent =
  | Closed
  | ActionInvoked

[<Struct; NoComparison>]
type MetroTeachingTipProps = {
  Open: bool
  Anchor: ElementHandle voption
  Placement: MetroFlyoutPlacement
  Title: string
  Subtitle: string voption
  Body: string voption
  ActionText: string voption
  Closable: bool
  LightDismiss: bool
  MinWidth: float
  MaxWidth: float
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroTeachingTipCell() =
  inherit Cell<MetroTeachingTipProps>()

  abstract IsPresented: bool

  abstract PanelTranslate: Anim<Point>

  abstract PanelOpacity: Anim<float>

  abstract View: Blob

  abstract Close: unit -> unit

  abstract InvokeAction: unit -> unit

  member internal this.Raise
    (callback: Action<MetroTeachingTipEvent>, ev: MetroTeachingTipEvent)
    : unit =
    this.InvokeInputCallback(callback, ev)

module TeachingTipInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  let slideOffset(placement: MetroFlyoutPlacement) : Point =
    match placement with
    | MetroFlyoutPlacement.Top
    | MetroFlyoutPlacement.TopStart
    | MetroFlyoutPlacement.TopEnd -> Point(X = 0.0, Y = 10.0)
    | MetroFlyoutPlacement.Left -> Point(X = 10.0, Y = 0.0)
    | MetroFlyoutPlacement.Right -> Point(X = -10.0, Y = 0.0)
    | MetroFlyoutPlacement.Bottom
    | MetroFlyoutPlacement.BottomStart
    | MetroFlyoutPlacement.BottomEnd -> Point(X = 0.0, Y = -10.0)

  [<Struct; NoComparison>]
  type SurfaceArgs = {
    Props: MetroTeachingTipProps
    ExtraBody: (unit -> Blob) voption
    Translate: Anim<Point> voption
    Opacity: Anim<float> voption
  }

[<RequireQualifiedAccess>]
module MetroTeachingTip =

  let defaults(title: string, isOpen: bool) : MetroTeachingTipProps = {
    Open = isOpen
    Anchor = ValueNone
    Placement = MetroFlyoutPlacement.Bottom
    Title = title
    Subtitle = ValueNone
    Body = ValueNone
    ActionText = ValueNone
    Closable = true
    LightDismiss = true
    MinWidth = 220.0
    MaxWidth = 320.0
    AccessibilityName = ValueNone
  }

  let anchored
    (anchor: ElementHandle, title: string, isOpen: bool)
    : MetroTeachingTipProps = {
    defaults(title, isOpen) with
        Anchor = ValueSome anchor
  }

  let buildSurfaceCore
    (
      args: TeachingTipInternals.SurfaceArgs,
      onEvent: MetroTeachingTipEvent -> unit
    ) : Container =
    let theme = Metro.read()
    let p = theme.Palette
    let props = args.Props
    let outlineColor = if theme.HighContrast then p.Foreground else p.Border
    let titleText = if isNull props.Title then "" else props.Title

    let minWidth =
      if Double.IsFinite props.MinWidth && props.MinWidth > 0.0 then
        props.MinWidth
      else
        220.0

    let maxWidth =
      if Double.IsFinite props.MaxWidth && props.MaxWidth > 0.0 then
        props.MaxWidth
      else
        320.0

    let a11yName =
      props.AccessibilityName
      |> ValueOption.filter(fun name -> not(String.IsNullOrWhiteSpace name))
      |> ValueOption.defaultWith(fun () ->
        if String.IsNullOrWhiteSpace titleText then
          "Teaching tip"
        else
          titleText)

    let titleCol =
      Container(
        Key = "metro-teaching-tip-title",
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        MinWidth = Length.Pixels 0.0,
        Gap = Length.Pixels 4.0
      )

    if not(String.IsNullOrWhiteSpace titleText) then
      titleCol.Add(
        Text(
          Content = titleText,
          Color = p.Foreground,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(16.0 * theme.TextScaleFactor),
          FontWeight = 600.0,
          LineHeight = 1.3
        )
      )

    props.Subtitle
    |> ValueOption.iter(fun sub ->
      if not(String.IsNullOrWhiteSpace sub) then
        titleCol.Add(
          Text(
            Content = sub,
            Color = p.ForegroundSecondary,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(13.0 * theme.TextScaleFactor),
            FontWeight = 400.0,
            LineHeight = 1.35
          )
        ))

    let headerRow =
      Container(
        Width = Length.Percent 100.0,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.FlexStart,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Length.Pixels 8.0
      )
        .Children(titleCol)

    let closeAction = Action(fun () -> onEvent MetroTeachingTipEvent.Closed)

    if props.Closable then
      let closeBtn =
        Button(
          Key = "metro-teaching-tip-close",
          Width = Length.Pixels 28.0,
          Height = Length.Pixels 28.0,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          FlexShrink = 0.0,
          Padding = Edges(Length.Pixels 0.0),
          BorderRadius = TeachingTipInternals.zeroRadius,
          BorderWidth = TeachingTipInternals.zeroBorder,
          BorderColor = TeachingTipInternals.transparentBorder,
          BoxShadows = TeachingTipInternals.emptyShadows,
          BackgroundColor = Color.Transparent,
          HoverStyle = Style(BackgroundColor = p.Hover),
          ActiveStyle = Style(BackgroundColor = p.Active),
          FocusStyle =
            Style(
              OutlineWidth = Length.Pixels 2.0,
              OutlineColor = p.Foreground,
              OutlineOffset = Length.Pixels 1.0
            ),
          Cursor = Cursor.Pointer,
          Focusable = true,
          TabStop = true,
          OnClick = closeAction,
          KeyBindings = [|
            KeyBinding(Key.Enter, KeyModifiers.None, closeAction)
            KeyBinding(Key.Space, KeyModifiers.None, closeAction)
          |],
          Accessibility =
            Accessibility(Role = AccessibilityRole.Button, Name = "Close")
        )
          .Children(MaterialIcons.Create("close", 16.0, p.Foreground))

      headerRow.Add(closeBtn)

    let panel =
      Container(
        Key = "metro-teaching-tip-panel",
        MinWidth = Length.Pixels(max 160.0 minWidth),
        MaxWidth = Length.Pixels(max minWidth maxWidth),
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels 8.0,
        Padding = Edges(Length.Pixels 12.0, Length.Pixels 16.0),
        BorderRadius = TeachingTipInternals.zeroRadius,
        BorderWidth =
          Edges(
            Length.Pixels 3.0,
            Length.Pixels 1.0,
            Length.Pixels 1.0,
            Length.Pixels 1.0
          ),
        BorderColor =
          Edges<Color>(
            theme.Accent.Base,
            outlineColor,
            outlineColor,
            outlineColor
          ),
        BoxShadows = TeachingTipInternals.emptyShadows,
        BackgroundColor = p.BackgroundAlt,
        HitTestSelf = true,
        Focusable = true,
        TabStop = false,
        KeyBindings =
          (if props.LightDismiss then
             [| KeyBinding(Key.Escape, KeyModifiers.None, closeAction) |]
           else
             Array.empty),
        Accessibility =
          Accessibility(Role = AccessibilityRole.Dialog, Name = a11yName)
      )
        .Children(headerRow)

    args.Translate |> ValueOption.iter(fun anim -> panel.TranslateAnim <- anim)
    args.Opacity |> ValueOption.iter(fun anim -> panel.OpacityAnim <- anim)

    props.Body
    |> ValueOption.iter(fun bodyText ->
      if not(String.IsNullOrWhiteSpace bodyText) then
        panel.Add(
          Text(
            Content = bodyText,
            Color = p.Foreground,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(13.0 * theme.TextScaleFactor),
            FontWeight = 400.0,
            LineHeight = 1.4
          )
        ))

    args.ExtraBody
    |> ValueOption.iter(fun buildExtra ->
      let extra = buildExtra()

      if not(isNull extra) then
        panel.Add(extra))

    props.ActionText
    |> ValueOption.iter(fun actionLabel ->
      if not(String.IsNullOrWhiteSpace actionLabel) then
        panel.Add(
          Container(
            FlexDirection = FlexDirection.Row,
            Padding = Edges(Length.Pixels 4.0, Length.Pixels 0.0)
          )
            .Children(
              MetroButton.accent(
                actionLabel,
                fun () -> onEvent MetroTeachingTipEvent.ActionInvoked
              )
            )
        ))

    panel

  let buildSurface
    (props: MetroTeachingTipProps, onEvent: MetroTeachingTipEvent -> unit)
    : Blob =
    buildSurfaceCore(
      {
        Props = props
        ExtraBody = ValueNone
        Translate = ValueNone
        Opacity = ValueNone
      },
      onEvent
    )
    :> Blob

  let wrapInPortal
    (props: MetroTeachingTipProps, panel: Container, dismissOpt: Action voption)
    : Blob =
    // A null anchor presents the window-centered tip, as a missing anchor does.
    let anchorOpt =
      props.Anchor |> ValueOption.filter(fun anchor -> not(isNull anchor))

    let portal: Portal =
      match anchorOpt with
      | ValueSome anchor ->
        Portal(
          Key = "metro-teaching-tip-portal",
          Anchor = anchor,
          Placement = MetroFlyout.toPortalPlacement props.Placement,
          ZIndex = 32,
          Padding = Edges(Length.Pixels 8.0),
          FocusScope = FocusScopeOptions(Modal = false, RestoreFocus = true)
        )
      | ValueNone ->
        Portal(
          Key = "metro-teaching-tip-portal",
          ZIndex = 32,
          Width = Length.Percent 100.0,
          FlexDirection = FlexDirection.Column,
          JustifyContent = JustifyContent.FlexStart,
          AlignItems = AlignItems.Center,
          Padding = Edges(Length.Pixels 24.0),
          FocusScope = FocusScopeOptions(Modal = false, RestoreFocus = true)
        )

    dismissOpt
    |> ValueOption.iter(fun d ->
      if props.LightDismiss then
        portal.OnDismiss <- Action<PopupDismissReason>(fun _ -> d.Invoke()))

    portal.Add(panel)
    portal :> Blob

  let build
    (props: MetroTeachingTipProps, onEvent: MetroTeachingTipEvent -> unit)
    : Blob =
    if not props.Open then
      Container(
        Display = Display.None,
        Accessibility =
          Accessibility(Role = AccessibilityRole.None, Hidden = true)
      )
      :> Blob
    else
      let panel =
        buildSurfaceCore(
          {
            Props = props
            ExtraBody = ValueNone
            Translate = ValueNone
            Opacity = ValueNone
          },
          onEvent
        )

      let dismiss = Action(fun () -> onEvent MetroTeachingTipEvent.Closed)
      wrapInPortal(props, panel, ValueSome dismiss)

  type TipState = {
    mutable Props: MetroTeachingTipProps
    mutable Presented: bool
    mutable Dismissed: bool
    mutable LastOpen: bool
    mutable LastReducedMotion: bool
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
  }

  let snapOpen(state: TipState) : unit =
    state.Presented <- true
    state.Translate.Snap(TeachingTipInternals.zeroPoint)
    state.Opacity.Snap(1.0)

  let snapClosed(state: TipState) : unit =
    state.Presented <- false
    state.Translate.Snap(TeachingTipInternals.zeroPoint)
    state.Opacity.Snap(0.0)

  let createInternal
    (onEvent: MetroTeachingTipEvent -> unit, extraBody: (unit -> Blob) voption)
    : MetroTeachingTipCell =
    let state: TipState = {
      Props = defaults("", false)
      Presented = false
      Dismissed = false
      LastOpen = false
      LastReducedMotion = false
      Translate = Unchecked.defaultof<Anim<Point>>
      Opacity = Unchecked.defaultof<Anim<float>>
    }

    let mutable self: MetroTeachingTipCell voption = ValueNone
    let onEventAction = Action<MetroTeachingTipEvent>(onEvent)

    let emit(ev: MetroTeachingTipEvent) =
      match ev with
      | MetroTeachingTipEvent.Closed when state.Dismissed -> ()
      | MetroTeachingTipEvent.Closed ->
        state.Dismissed <- true
        self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, ev))
      | _ -> self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, ev))

    let dismissAction = Action(fun () -> emit MetroTeachingTipEvent.Closed)

    let renderCurrent() : Blob =
      if not state.Presented && not state.Props.Open then
        Container(
          Display = Display.None,
          Accessibility =
            Accessibility(Role = AccessibilityRole.None, Hidden = true)
        )
        :> Blob
      else
        let panel =
          buildSurfaceCore(
            {
              Props = state.Props
              ExtraBody = extraBody
              Translate = ValueSome state.Translate
              Opacity = ValueSome state.Opacity
            },
            emit
          )

        wrapInPortal(state.Props, panel, ValueSome dismissAction)

    let applyTransition(nextOpen: bool, reducedMotion: bool) : unit =
      if nextOpen <> state.LastOpen then
        state.LastOpen <- nextOpen

        if nextOpen then
          let wasClosing = state.Presented
          state.Presented <- true
          state.Dismissed <- false

          if reducedMotion then
            snapOpen state
          elif wasClosing then
            state.Translate.To(
              TeachingTipInternals.zeroPoint,
              MetroMotion.normal
            )

            state.Opacity.To(1.0, MetroMotion.normal)
          else
            let fromPt = TeachingTipInternals.slideOffset state.Props.Placement

            state.Translate.Play(
              fromPt,
              TeachingTipInternals.zeroPoint,
              MetroMotion.normal
            )

            state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
        else if reducedMotion then
          snapClosed state
        else
          let toPt = TeachingTipInternals.slideOffset state.Props.Placement

          state.Translate.To(toPt, MetroMotion.fast)
          state.Opacity.To(0.0, MetroMotion.fast)

    let cell = {
      new MetroTeachingTipCell() with
        override _.IsPresented = state.Presented || state.Props.Open
        override _.PanelTranslate = state.Translate
        override _.PanelOpacity = state.Opacity
        override _.View = renderCurrent()
        override _.Close() = emit MetroTeachingTipEvent.Closed

        override _.InvokeAction() =
          emit MetroTeachingTipEvent.ActionInvoked

        override _.InputChanged(props) =
          state.Props <- props
          applyTransition(props.Open, state.LastReducedMotion)

        override _.Unmounting() =
          state.LastOpen <- false
          state.Dismissed <- false
          snapClosed state

        override _.Build(props) : Blob =
          state.Props <- props
          let theme = Metro.read()
          state.LastReducedMotion <- theme.ReducedMotion

          if props.Open && not state.Presented && not state.LastOpen then
            state.LastOpen <- true
            state.Dismissed <- false
            snapOpen state
          elif
            theme.ReducedMotion
            && (state.Translate.Running || state.Opacity.Running)
          then
            if props.Open then snapOpen state else snapClosed state

          renderCurrent()
    }

    state.Translate <- cell.AnimateValue(TeachingTipInternals.zeroPoint)
    state.Opacity <- cell.AnimateValue(0.0)

    state.Opacity.add_Completed(
      Action<MotionCompletionReason>(fun _ ->
        if not state.Props.Open && state.Presented then
          state.Presented <- false
          self |> ValueOption.iter(fun c -> c.Rebuild()))
    )

    self <- ValueSome cell
    cell

  let create(onEvent: MetroTeachingTipEvent -> unit) : MetroTeachingTipCell =
    createInternal(onEvent, ValueNone)

  let createWithContent
    (onEvent: MetroTeachingTipEvent -> unit, renderContent: unit -> Blob)
    : MetroTeachingTipCell =
    createInternal(onEvent, ValueSome renderContent)

  let createFactory
    (onEvent: MetroTeachingTipEvent -> unit)
    : unit -> MetroTeachingTipCell =
    fun () -> create onEvent

  let mount
    (
      key: string,
      props: MetroTeachingTipProps,
      factory: unit -> MetroTeachingTipCell
    ) : Blob =
    Cell.Mount<MetroTeachingTipProps, MetroTeachingTipCell>(key, props, factory)
