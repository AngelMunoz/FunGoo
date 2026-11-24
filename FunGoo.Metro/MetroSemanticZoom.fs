namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroSemanticZoomInput = {
  IsZoomedOut: bool
  ShowToggleButton: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroSemanticZoomEvent =
  | ZoomOut
  | ZoomIn

[<Struct; NoComparison>]
type MetroSemanticZoomState = {
  Input: MetroSemanticZoomInput
  ScaleAnim: Anim<float>
  OpacityAnim: Anim<float>
}

[<AbstractClass>]
type MetroSemanticZoomCell() =
  inherit Cell<MetroSemanticZoomInput>()

  abstract ScaleAnim: Anim<float>

  abstract OpacityAnim: Anim<float>

  abstract IsZoomedOut: bool

  abstract State: MetroSemanticZoomState

  abstract View: Blob

  abstract Handle: event: MetroSemanticZoomEvent -> unit

  // Gives a zoom request of the host to the host cell, as a Goo.Widgets
  // control does. The owner rule then rebuilds the host cell.
  member this.Raise
    (callback: Action<MetroSemanticZoomEvent>, value: MetroSemanticZoomEvent)
    =
    this.InvokeInputCallback(callback, value)

module ZoomInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let hcBorder = Edges(Length.Pixels 2.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPadding = Edges(Length.Pixels 0.0)
  let cornerMargin = Edges(Length.Pixels 8.0)

  [<Literal>]
  let toggleKey = "zoom-toggle"

  [<Literal>]
  let viewKey = "zoom-view"

  let buildView
    (
      state: MetroSemanticZoomState,
      render: bool -> Blob,
      onEvent: MetroSemanticZoomEvent -> unit
    ) : Blob =
    if isNull state.ScaleAnim || isNull state.OpacityAnim then
      raise(ArgumentNullException(nameof state))

    let theme = Metro.read()
    let p = theme.Palette
    let input = state.Input
    let zoomedOut = input.IsZoomedOut
    let transitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs)

    let view =
      Container(
        Key = viewKey,
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        MinHeight = Length.Pixels 0.0,
        Overflow = Overflow.Hidden,
        ScaleAnim = state.ScaleAnim,
        OpacityAnim = state.OpacityAnim
      )
        .Children(render zoomedOut)

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.Group,
            Name =
              (input.AccessibilityName
               |> ValueOption.defaultValue "Semantic zoom")
          )
      )

    root.Add(view)

    if input.ShowToggleButton then
      let borderWidth = if theme.HighContrast then hcBorder else zeroBorder

      let borderColor =
        if theme.HighContrast then
          Edges<Color>(p.Foreground)
        else
          transparentBorder

      let iconName = if zoomedOut then "zoom_in" else "zoom_out"

      let label = if zoomedOut then "Zoom in" else "Zoom out"

      let focusStyle =
        Style(
          OutlineWidth = Length.Pixels 2.0,
          OutlineColor = p.Foreground,
          OutlineOffset = Length.Pixels 2.0
        )

      let toggle =
        Button(
          Key = toggleKey,
          Position = PositionType.Absolute,
          Top = Length.Pixels 0.0,
          End = Length.Pixels 0.0,
          Margin = cornerMargin,
          Width = Length.Pixels 40.0,
          Height = Length.Pixels 40.0,
          AlignItems = AlignItems.Center,
          JustifyContent = JustifyContent.Center,
          Padding = zeroPadding,
          BorderRadius = zeroRadius,
          BorderWidth = borderWidth,
          BorderColor = borderColor,
          BoxShadows = emptyShadows,
          BackgroundColor = p.Highlight,
          HoverStyle = Style(BackgroundColor = p.Hover),
          ActiveStyle = Style(BackgroundColor = theme.Accent.Base),
          FocusStyle = focusStyle,
          TransitionMs = transitionMs,
          TransitionEasing = Easing.EaseOut,
          Cursor = Cursor.Pointer,
          Focusable = true,
          TabStop = true,
          OnClick =
            (fun () ->
              onEvent(
                if zoomedOut then
                  MetroSemanticZoomEvent.ZoomIn
                else
                  MetroSemanticZoomEvent.ZoomOut
              )),
          Accessibility =
            Accessibility(Role = AccessibilityRole.Button, Name = label)
        )
          .Children(MaterialIcons.Create(iconName, 20.0, Nullable p.Foreground))

      root.Add(toggle)

    root

[<RequireQualifiedAccess>]
module MetroSemanticZoom =

  let defaults(isZoomedOut: bool) : MetroSemanticZoomInput = {
    IsZoomedOut = isZoomedOut
    ShowToggleButton = true
    AccessibilityName = ValueNone
  }

  let buildView
    (
      state: MetroSemanticZoomState,
      render: bool -> Blob,
      onEvent: MetroSemanticZoomEvent -> unit
    ) : Blob =
    ZoomInternals.buildView(state, render, onEvent)

  /// State of a semantic zoom cell. One mutable field per concern.
  type ZoomState = {
    mutable Input: MetroSemanticZoomInput
    mutable LastZoomedOut: bool voption
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    // Slots for the two animation values. `create` fills them after the cell
    // exists.
    mutable Scale: Anim<float>
    mutable Opacity: Anim<float>
  }

  /// The values that the functions of a semantic zoom cell work on.
  type ZoomContext = {
    State: ZoomState
    /// Gives one zoom request to the host cell.
    Raise: Action<MetroSemanticZoomEvent>
    Render: bool -> Blob
  }

  let emptyInput: MetroSemanticZoomInput = {
    IsZoomedOut = false
    ShowToggleButton = true
    AccessibilityName = ValueNone
  }

  let initialState() : ZoomState = {
    Input = emptyInput
    LastZoomedOut = ValueNone
    LastReducedMotion = false
    ScopedReducedMotion = false
    Scale = Unchecked.defaultof<Anim<float>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  /// The explicit state of the tree, for <c>buildView</c>.
  let currentState(ctx: ZoomContext) : MetroSemanticZoomState = {
    Input = ctx.State.Input
    ScaleAnim = ctx.State.Scale
    OpacityAnim = ctx.State.Opacity
  }

  /// Restores the resting scale and opacity of the active view.
  let endZoom(state: ZoomState) : unit =
    state.Scale.Snap(1.0)
    state.Opacity.Snap(1.0)

  /// Handles one event of the tree. A request for the view that is already
  /// active raises nothing.
  let handle(ctx: ZoomContext, event: MetroSemanticZoomEvent) : unit =
    match event with
    | MetroSemanticZoomEvent.ZoomOut ->
      if not ctx.State.Input.IsZoomedOut then
        ctx.Raise.Invoke(MetroSemanticZoomEvent.ZoomOut)
    | MetroSemanticZoomEvent.ZoomIn ->
      if ctx.State.Input.IsZoomedOut then
        ctx.Raise.Invoke(MetroSemanticZoomEvent.ZoomIn)

  let view(ctx: ZoomContext) : Blob =
    ZoomInternals.buildView(
      currentState ctx,
      ctx.Render,
      fun event -> handle(ctx, event)
    )

  /// Plays the zoom transition when the view changes, or snaps into place
  /// under reduced motion.
  let inputChanged
    (ctx: ZoomContext, input: MetroSemanticZoomInput, hostReduced: bool voption)
    : unit =
    let state = ctx.State
    state.Input <- input

    let viewChanged =
      state.LastZoomedOut
      |> ValueOption.map(fun previous -> previous <> input.IsZoomedOut)
      |> ValueOption.defaultValue false

    if viewChanged then
      let reducedMotion =
        state.ScopedReducedMotion
        || ValueOption.defaultValue state.LastReducedMotion hostReduced

      if reducedMotion then
        endZoom state
      else
        state.Scale.Play(0.96, 1.0, MetroMotion.normal)
        state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
    elif state.LastZoomedOut.IsNone then
      endZoom state

    state.LastZoomedOut <- ValueSome input.IsZoomedOut

  /// Resets the transition on unmount.
  let unmount(state: ZoomState) : unit =
    state.LastZoomedOut <- ValueNone
    state.ScopedReducedMotion <- false
    endZoom state

  /// Reads the theme, snaps into place under reduced motion, then builds the
  /// tree.
  let build
    (ctx: ZoomContext, input: MetroSemanticZoomInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      endZoom state

    view ctx

  /// The reduced motion preference of the host window. <c>ValueNone</c> when
  /// the cell has no window.
  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let create
    (onEvent: MetroSemanticZoomEvent -> unit, render: bool -> Blob)
    : MetroSemanticZoomCell =
    let state = initialState()

    // The slot of the cell: the raise delegate is made before the cell exists.
    let mutable self: MetroSemanticZoomCell voption = ValueNone
    let onEventAction = Action<MetroSemanticZoomEvent>(onEvent)

    // One delegate for the life of the cell. It gives each zoom request to
    // the host cell, so the owner rule rebuilds the host.
    let raise =
      Action<MetroSemanticZoomEvent>(fun event ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onEventAction, event)))

    let context: ZoomContext = {
      State = state
      Raise = raise
      Render = render
    }

    let cell = {
      new MetroSemanticZoomCell() with
        override _.ScaleAnim = state.Scale
        override _.OpacityAnim = state.Opacity
        override _.IsZoomedOut = state.Input.IsZoomedOut
        override _.State = currentState context
        override _.View = view context

        override this.Handle(event) = handle(context, event)

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    // The cell exists now: Goo ties each animation to it.
    state.Scale <- cell.AnimateValue(1.0)
    state.Opacity <- cell.AnimateValue(1.0)
    self <- ValueSome cell
    cell

  let createFactory
    (onEvent: MetroSemanticZoomEvent -> unit, render: bool -> Blob)
    : unit -> MetroSemanticZoomCell =
    fun () -> create(onEvent, render)

  let mount
    (
      key: string,
      input: MetroSemanticZoomInput,
      factory: unit -> MetroSemanticZoomCell
    ) : Blob =
    Cell.Mount<MetroSemanticZoomInput, MetroSemanticZoomCell>(
      key,
      input,
      factory
    )
