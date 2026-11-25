namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct>]
type MetroSplitViewMode =
  | Overlay
  | Inline
  | Compact

[<Struct>]
type MetroSplitViewPlacement =
  | Start
  | End

[<Struct; NoComparison>]
type MetroSplitViewInput = {
  Open: bool
  DisplayMode: MetroSplitViewMode
  PanePlacement: MetroSplitViewPlacement
  OpenPaneLength: float
  CompactPaneLength: float
  LightDismissOverlay: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroSplitViewCell() =
  inherit Cell<MetroSplitViewInput>()

  abstract IsOverlayVisible: bool

  abstract PaneHandle: ElementHandle

  abstract PaneTranslate: Anim<Point>

  abstract BackdropOpacity: Anim<float>

  abstract DismissOverlay: unit -> unit

  member internal this.Raise(callback: Action<bool>, isOpen: bool) : unit =
    this.InvokeInputCallback(callback, isOpen)

module SplitViewInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroPoint = Point(X = 0.0, Y = 0.0)
  let scrimColor = Color.Rgba(0, 0, 0, 102)

  let resolveOpenLength(len: float) : float =
    if Double.IsFinite len && len > 0.0 then len else 280.0

  let resolveCompactLength(len: float) : float =
    if Double.IsFinite len && len >= 0.0 then len else 48.0

  let overlayClosedOffset
    (placement: MetroSplitViewPlacement, openWidth: float, rtl: bool)
    : Point =
    let sign =
      match placement, rtl with
      | MetroSplitViewPlacement.Start, false -> -1.0
      | MetroSplitViewPlacement.Start, true -> 1.0
      | MetroSplitViewPlacement.End, false -> 1.0
      | MetroSplitViewPlacement.End, true -> -1.0

    Point(X = sign * openWidth, Y = 0.0)

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

[<RequireQualifiedAccess>]
module MetroSplitView =

  let defaults(isOpen: bool) : MetroSplitViewInput = {
    Open = isOpen
    DisplayMode = MetroSplitViewMode.Overlay
    PanePlacement = MetroSplitViewPlacement.Start
    OpenPaneLength = 280.0
    CompactPaneLength = 48.0
    LightDismissOverlay = true
    AccessibilityName = ValueNone
  }

  type SplitState = {
    PaneHandle: ElementHandle
    ContentHandle: ElementHandle
    mutable Input: MetroSplitViewInput
    mutable LastOverlayOpen: bool voption
    mutable OverlayClosing: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable RightToLeft: bool
    mutable PaneTranslate: Anim<Point>
    mutable BackdropOpacity: Anim<float>
  }

  type SplitContext = {
    State: SplitState
    RenderPane: unit -> Blob
    RenderContent: unit -> Blob
    BackdropPress: Action<PointerEvent>
    EscapeBindings: KeyBinding array
  }

  let snapOverlayOpen(state: SplitState) : unit =
    state.OverlayClosing <- false
    state.PaneTranslate.Snap(SplitViewInternals.zeroPoint)
    state.BackdropOpacity.Snap(1.0)

  let snapOverlayClosed(state: SplitState) : unit =
    state.OverlayClosing <- false
    state.PaneTranslate.Snap(SplitViewInternals.zeroPoint)
    state.BackdropOpacity.Snap(0.0)

  let buildView(ctx: SplitContext, theme: MetroTheme) : Blob =
    let state = ctx.State
    let input = state.Input
    let p = theme.Palette
    let openW = SplitViewInternals.resolveOpenLength input.OpenPaneLength

    let compactW =
      SplitViewInternals.resolveCompactLength input.CompactPaneLength

    let dividerColor = if theme.HighContrast then p.Foreground else p.Border

    let a11yName =
      input.AccessibilityName |> ValueOption.defaultValue "Split pane"

    let contentHost =
      Container(
        Key = "metro-splitview-content",
        Handle = state.ContentHandle,
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        MinWidth = Length.Pixels 0.0,
        Height = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        BackgroundColor = p.Background
      )

    let builtContent = ctx.RenderContent()

    if not(isNull builtContent) then
      contentHost.Add(builtContent)

    let innerPaneContent =
      Container(
        Width = Length.Pixels openW,
        Height = Length.Percent 100.0,
        FlexShrink = 0.0,
        FlexDirection = FlexDirection.Column,
        OverflowY = Overflow.Scroll,
        OverflowX = Overflow.Hidden
      )

    match input.DisplayMode with
    | MetroSplitViewMode.Inline
    | MetroSplitViewMode.Compact ->
      let targetW =
        if input.Open then
          openW
        elif input.DisplayMode = MetroSplitViewMode.Compact then
          compactW
        else
          0.0

      let showDivider =
        input.Open || input.DisplayMode = MetroSplitViewMode.Compact

      let builtPane = ctx.RenderPane()

      if not(isNull builtPane) then
        innerPaneContent.Add(builtPane)

      let pane =
        Container(
          Key = "metro-splitview-pane",
          Handle = state.PaneHandle,
          Width = Length.Pixels targetW,
          Height = Length.Percent 100.0,
          FlexShrink = 0.0,
          Overflow = Overflow.Hidden,
          BorderRadius = SplitViewInternals.zeroRadius,
          BoxShadows = SplitViewInternals.emptyShadows,
          BackgroundColor = p.BackgroundAlt,
          TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.slowMs),
          TransitionEasing = Easing.EaseOut,
          Accessibility =
            Accessibility(Role = AccessibilityRole.Group, Name = a11yName)
        )
          .Children(innerPaneContent)

      if showDivider then
        match input.PanePlacement with
        | MetroSplitViewPlacement.Start ->
          pane.BorderEndWidth <- Length.Pixels 1.0
          pane.BorderEndColor <- dividerColor
        | MetroSplitViewPlacement.End ->
          pane.BorderStartWidth <- Length.Pixels 1.0
          pane.BorderStartColor <- dividerColor

      let root =
        Container(
          Key = "metro-splitview-root",
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Row,
          Overflow = Overflow.Hidden
        )

      match input.PanePlacement with
      | MetroSplitViewPlacement.Start ->
        root.Add(pane)
        root.Add(contentHost)
      | MetroSplitViewPlacement.End ->
        root.Add(contentHost)
        root.Add(pane)

      root :> Blob

    | MetroSplitViewMode.Overlay ->
      let root =
        Container(
          Key = "metro-splitview-root",
          Position = PositionType.Relative,
          Width = Length.Percent 100.0,
          Height = Length.Percent 100.0,
          FlexDirection = FlexDirection.Row,
          Overflow = Overflow.Hidden,
          KeyBindings =
            (if input.Open && input.LightDismissOverlay then
               ctx.EscapeBindings
             else
               Array.empty)
        )
          .Children(contentHost)

      if input.Open || state.OverlayClosing then
        let canDismiss = input.Open && input.LightDismissOverlay
        let builtPane = ctx.RenderPane()

        if not(isNull builtPane) then
          innerPaneContent.Add(builtPane)

        let backdrop: Container =
          Container(
            Key = "metro-splitview-backdrop",
            Position = PositionType.Absolute,
            Top = Length.Pixels 0.0,
            Right = Length.Pixels 0.0,
            Bottom = Length.Pixels 0.0,
            Left = Length.Pixels 0.0,
            BackgroundColor = SplitViewInternals.scrimColor,
            OpacityAnim = state.BackdropOpacity,
            HitTestSelf = canDismiss
          )

        if canDismiss then
          backdrop.OnPointerDown <- ctx.BackdropPress

        let pane =
          Container(
            Key = "metro-splitview-pane",
            Handle = state.PaneHandle,
            Position = PositionType.Absolute,
            Top = Length.Pixels 0.0,
            Bottom = Length.Pixels 0.0,
            Width = Length.Pixels openW,
            Overflow = Overflow.Hidden,
            BorderRadius = SplitViewInternals.zeroRadius,
            BoxShadows = SplitViewInternals.emptyShadows,
            BackgroundColor = p.BackgroundAlt,
            TranslateAnim = state.PaneTranslate,
            HitTestSelf = true,
            Accessibility =
              Accessibility(Role = AccessibilityRole.Group, Name = a11yName)
          )
            .Children(innerPaneContent)

        match input.PanePlacement with
        | MetroSplitViewPlacement.Start ->
          pane.Start <- Length.Pixels 0.0
          pane.BorderEndWidth <- Length.Pixels 1.0
          pane.BorderEndColor <- dividerColor
        | MetroSplitViewPlacement.End ->
          pane.End <- Length.Pixels 0.0
          pane.BorderStartWidth <- Length.Pixels 1.0
          pane.BorderStartColor <- dividerColor

        root.Add(backdrop)
        root.Add(pane)

      root :> Blob

  let syncOverlayMotion
    (state: SplitState, input: MetroSplitViewInput, reducedMotion: bool)
    : unit =
    let isOverlay = input.DisplayMode = MetroSplitViewMode.Overlay
    let nextOpen = isOverlay && input.Open

    if not isOverlay then
      state.LastOverlayOpen <- ValueSome false
      snapOverlayClosed state
    else
      match state.LastOverlayOpen with
      | ValueNone ->
        // First input of a fresh mount: show the input state at once.
        state.LastOverlayOpen <- ValueSome nextOpen

        if nextOpen then
          snapOverlayOpen state
        else
          snapOverlayClosed state

      | ValueSome wasOpen ->
        if nextOpen <> wasOpen then
          state.LastOverlayOpen <- ValueSome nextOpen
          let openW = SplitViewInternals.resolveOpenLength input.OpenPaneLength

          let offset =
            SplitViewInternals.overlayClosedOffset(
              input.PanePlacement,
              openW,
              state.RightToLeft
            )

          if nextOpen then
            if reducedMotion then
              snapOverlayOpen state
            elif state.OverlayClosing then
              // The pane is still in the tree: continue the slide from the
              // current offset instead of restarting it at the closed edge.
              state.OverlayClosing <- false

              state.PaneTranslate.To(
                SplitViewInternals.zeroPoint,
                MetroMotion.slow
              )

              state.BackdropOpacity.To(1.0, MetroMotion.slow)
            else
              state.OverlayClosing <- false

              state.PaneTranslate.Play(
                offset,
                SplitViewInternals.zeroPoint,
                MetroMotion.slow
              )

              state.BackdropOpacity.Play(0.0, 1.0, MetroMotion.slow)
          elif reducedMotion then
            snapOverlayClosed state
          else
            state.OverlayClosing <- true
            state.PaneTranslate.To(offset, MetroMotion.slow)
            state.BackdropOpacity.To(0.0, MetroMotion.slow)

  let create
    (
      onOpenChange: bool -> unit,
      renderPane: unit -> Blob,
      renderContent: unit -> Blob
    ) : MetroSplitViewCell =
    let state: SplitState = {
      PaneHandle = ElementHandle()
      ContentHandle = ElementHandle()
      Input = defaults false
      LastOverlayOpen = ValueNone
      OverlayClosing = false
      LastReducedMotion = false
      ScopedReducedMotion = false
      RightToLeft = false
      PaneTranslate = Unchecked.defaultof<Anim<Point>>
      BackdropOpacity = Unchecked.defaultof<Anim<float>>
    }

    let mutable self: MetroSplitViewCell voption = ValueNone
    let onChangeAction = Action<bool>(onOpenChange)

    let requestClose() =
      if state.Input.Open then
        self |> ValueOption.iter(fun cell -> cell.Raise(onChangeAction, false))

    let escapeAction = Action(fun () -> requestClose())

    let ctx: SplitContext = {
      State = state
      RenderPane = renderPane
      RenderContent = renderContent
      BackdropPress =
        Action<PointerEvent>(fun e ->
          if e.IsPrimary then
            requestClose())
      EscapeBindings = [|
        KeyBinding(Key.Escape, KeyModifiers.None, escapeAction)
      |]
    }

    let cell = {
      new MetroSplitViewCell() with
        override _.IsOverlayVisible =
          state.Input.DisplayMode = MetroSplitViewMode.Overlay
          && (state.Input.Open || state.OverlayClosing)

        override _.PaneHandle = state.PaneHandle
        override _.PaneTranslate = state.PaneTranslate
        override _.BackdropOpacity = state.BackdropOpacity
        override _.DismissOverlay() = requestClose()

        override this.InputChanged(input) =
          state.Input <- input

          let reducedMotion =
            state.ScopedReducedMotion
            || ValueOption.defaultValue
              state.LastReducedMotion
              (SplitViewInternals.reducedMotionOf this.Host)

          syncOverlayMotion(state, input, reducedMotion)

        override _.Unmounting() =
          state.LastOverlayOpen <- ValueNone
          snapOverlayClosed state

        override this.Build(input) : Blob =
          state.Input <- input
          let theme = Metro.read()

          let windowReduced =
            SplitViewInternals.reducedMotionOf this.Host
            |> ValueOption.defaultValue false

          state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced

          state.LastReducedMotion <- theme.ReducedMotion

          state.RightToLeft <-
            Goo.Widgets.Theme.Read().Direction = Direction.RightToLeft

          if theme.ReducedMotion then
            if input.DisplayMode = MetroSplitViewMode.Overlay && input.Open then
              snapOverlayOpen state
            else
              snapOverlayClosed state

          buildView(ctx, theme)
    }

    state.PaneTranslate <- cell.AnimateValue(SplitViewInternals.zeroPoint)
    state.BackdropOpacity <- cell.AnimateValue(0.0)

    state.BackdropOpacity.add_Completed(
      Action<MotionCompletionReason>(fun _ ->
        if not state.Input.Open && state.OverlayClosing then
          state.OverlayClosing <- false
          self |> ValueOption.iter(fun c -> c.Rebuild()))
    )

    self <- ValueSome cell
    cell

  let createFactory
    (
      onOpenChange: bool -> unit,
      renderPane: unit -> Blob,
      renderContent: unit -> Blob
    ) : unit -> MetroSplitViewCell =
    fun () -> create(onOpenChange, renderPane, renderContent)

  let mount
    (
      key: string,
      input: MetroSplitViewInput,
      factory: unit -> MetroSplitViewCell
    ) : Blob =
    Cell.Mount<MetroSplitViewInput, MetroSplitViewCell>(key, input, factory)
