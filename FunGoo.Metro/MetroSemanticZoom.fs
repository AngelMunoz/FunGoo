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

module private ZoomInternals =

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

[<Sealed>]
type MetroSemanticZoomCell
  (onEvent: MetroSemanticZoomEvent -> unit, render: bool -> Blob) as this =
  inherit Cell<MetroSemanticZoomInput>()

  let scaleAnim = this.AnimateValue(1.0)
  let opacityAnim = this.AnimateValue(1.0)

  let emptyInput: MetroSemanticZoomInput = {
    IsZoomedOut = false
    ShowToggleButton = true
    AccessibilityName = ValueNone
  }

  let mutable currentInput = emptyInput
  let mutable lastZoomedOut: bool voption = ValueNone
  let mutable lastReducedMotion = false
  let mutable scopedReducedMotion = false

  let isReducedMotion() =
    scopedReducedMotion
    || (if isNull this.Host then
          lastReducedMotion
        else
          Metro.readWindow(this.Host).ReducedMotion)

  // Restores the resting scale and opacity of the active view.
  let endZoom() =
    scaleAnim.Snap(1.0)
    opacityAnim.Snap(1.0)

  let handle(event: MetroSemanticZoomEvent) : unit =
    (match event with
     | MetroSemanticZoomEvent.ZoomOut ->
       if not currentInput.IsZoomedOut then
         onEvent(MetroSemanticZoomEvent.ZoomOut)
     | MetroSemanticZoomEvent.ZoomIn ->
       if currentInput.IsZoomedOut then
         onEvent(MetroSemanticZoomEvent.ZoomIn))
    |> ignore

  member _.ScaleAnim = scaleAnim

  member _.OpacityAnim = opacityAnim

  member _.IsZoomedOut = currentInput.IsZoomedOut

  member _.State: MetroSemanticZoomState = {
    Input = currentInput
    ScaleAnim = scaleAnim
    OpacityAnim = opacityAnim
  }

  member this.View: Blob = ZoomInternals.buildView(this.State, render, handle)

  member _.Handle(event: MetroSemanticZoomEvent) : unit = handle event

  override this.InputChanged(input: MetroSemanticZoomInput) =
    currentInput <- input

    let viewChanged =
      lastZoomedOut
      |> ValueOption.map(fun previous -> previous <> input.IsZoomedOut)
      |> ValueOption.defaultValue false

    if viewChanged then
      if isReducedMotion() then
        endZoom()
      else
        scaleAnim.Play(0.96, 1.0, MetroMotion.normal)
        opacityAnim.Play(0.0, 1.0, MetroMotion.normal)
    elif lastZoomedOut.IsNone then
      endZoom()

    lastZoomedOut <- ValueSome input.IsZoomedOut

  override _.Unmounting() =
    lastZoomedOut <- ValueNone
    scopedReducedMotion <- false
    endZoom()

  override this.Build(input: MetroSemanticZoomInput) : Blob =
    currentInput <- input
    let theme = Metro.read()

    let windowReduced =
      if isNull this.Host then
        false
      else
        Metro.readWindow(this.Host).ReducedMotion

    scopedReducedMotion <- theme.ReducedMotion && not windowReduced
    lastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      endZoom()

    this.View

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

  let createFactory
    (onEvent: MetroSemanticZoomEvent -> unit, render: bool -> Blob)
    : unit -> MetroSemanticZoomCell =
    fun () -> MetroSemanticZoomCell(onEvent, render)

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
