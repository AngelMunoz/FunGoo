namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroProgressBarProps = {
  Value: float voption
  Height: float
  Width: Length
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroIndeterminateBarInput = {
  Height: float
  Width: Length
  AccessibilityName: string voption
}

[<Struct>]
type MetroProgressRingSize =
  | Small
  | Normal
  | Large

[<Struct; NoComparison>]
type MetroProgressRingProps = {
  Active: bool
  Size: MetroProgressRingSize
  Color: Color voption
  AccessibilityName: string voption
}

module ProgressInternals =

  [<Literal>]
  let dotCount = 5

  [<Literal>]
  let barPeriodSeconds = 2.2

  [<Literal>]
  let ringPeriodSeconds = 3.0

  [<Literal>]
  let barStaggerStep = 0.075

  [<Literal>]
  let barActiveWindow = 0.80

  [<Literal>]
  let ringStaggerStep = 0.065

  [<Literal>]
  let ringActiveWindow = 0.84

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  let clamp01(value: float) : float =
    if not(Double.IsFinite value) || value <= 0.0 then 0.0
    elif value >= 1.0 then 1.0
    else value

  let resolveBarHeight(height: float) : float =
    if Double.IsFinite height && height > 0.0 then
      height
    else
      4.0

  let ringMetrics
    (size: MetroProgressRingSize)
    : struct (float * float * float) =
    match size with
    | MetroProgressRingSize.Small -> struct (24.0, 9.0, 4.0)
    | MetroProgressRingSize.Normal -> struct (36.0, 14.0, 5.0)
    | MetroProgressRingSize.Large -> struct (52.0, 21.0, 7.0)

  let wrapPhase(phase: float, offset: float) : float =
    let shifted = phase - offset
    if shifted < 0.0 then shifted + 1.0 else shifted

  // Smooth fast-in, slow-cruise, fast-out horizontal travel for Metro dots.
  let barDotX(u: float, trackWidth: float, dotSize: float) : float =
    if u >= barActiveWindow then
      -dotSize
    else
      let s = u / barActiveWindow

      if s < 0.30 then
        let k = s / 0.30
        let omt = 1.0 - k
        let ease = 1.0 - omt * omt * omt
        -dotSize + (0.34 * trackWidth + dotSize) * ease
      elif s < 0.70 then
        let k = (s - 0.30) / 0.40
        (0.34 + 0.32 * k) * trackWidth
      else
        let k = (s - 0.70) / 0.30
        let ease = k * k * k
        0.66 * trackWidth + (0.34 * trackWidth + dotSize) * ease

  let barDotOpacity(u: float) : float =
    if u >= barActiveWindow then
      0.0
    else
      let s = u / barActiveWindow

      if s < 0.08 then s / 0.08
      elif s > 0.92 then (1.0 - s) / 0.08
      else 1.0

  let ringDotAngle(u: float) : float =
    let s = u / ringActiveWindow
    -Math.PI * 0.5 + 2.0 * Math.PI * (s - 0.14 * Math.Sin(2.0 * Math.PI * s))

  let ringDotOpacity(u: float) : float =
    if u >= ringActiveWindow then
      0.0
    else
      let s = u / ringActiveWindow

      if s < 0.08 then s / 0.08
      elif s > 0.92 then (1.0 - s) / 0.08
      else 1.0

  // Paint-bound infinite simulations driven by a periodic simulation, so Goo
  // ticks TranslateAnim and OpacityAnim without rebuilding the owning Cell.
  let barDotXSimulation
    (
      periodic: Simulation,
      offset: float,
      getGeometry: unit -> struct (float * float)
    ) : Simulation = {
    new Simulation() with
      override _.IsInfinite = true

      override _.Position(elapsed: float) : float =
        let struct (trackWidth, dotSize) = getGeometry()
        let u = wrapPhase(periodic.Position elapsed, offset)
        barDotX(u, trackWidth, dotSize)

      override _.Velocity(_elapsed: float) : float = 0.0

      override _.Done(_elapsed: float) : bool = false
  }

  let constantInfinite(value: float) : Simulation = {
    new Simulation() with
      override _.IsInfinite = true
      override _.Position(_elapsed: float) : float = value
      override _.Velocity(_elapsed: float) : float = 0.0
      override _.Done(_elapsed: float) : bool = false
  }

  let barDotOpacitySimulation
    (periodic: Simulation, offset: float)
    : Simulation = {
    new Simulation() with
      override _.IsInfinite = true

      override _.Position(elapsed: float) : float =
        let u = wrapPhase(periodic.Position elapsed, offset)
        barDotOpacity u

      override _.Velocity(_elapsed: float) : float = 0.0

      override _.Done(_elapsed: float) : bool = false
  }

  // The arguments of the simulation of one axis of a ring dot.
  type RingDotAxisArgs = {
    Periodic: Simulation
    Offset: float
    IsX: bool
    Geometry: unit -> struct (float * float * float)
  }

  let ringDotAxisSimulation(args: RingDotAxisArgs) : Simulation = {
    new Simulation() with
      override _.IsInfinite = true

      override _.Position(elapsed: float) : float =
        let struct (boxSize, radius, dotSize) = args.Geometry()
        let center = (boxSize - dotSize) * 0.5
        let u = wrapPhase(args.Periodic.Position elapsed, args.Offset)

        if u >= ringActiveWindow then
          center
        else
          let angle = ringDotAngle u

          if args.IsX then
            center + radius * Math.Cos(angle)
          else
            center + radius * Math.Sin(angle)

      override _.Velocity(_elapsed: float) : float = 0.0

      override _.Done(_elapsed: float) : bool = false
  }

  let ringDotOpacitySimulation
    (periodic: Simulation, offset: float)
    : Simulation = {
    new Simulation() with
      override _.IsInfinite = true

      override _.Position(elapsed: float) : float =
        let u = wrapPhase(periodic.Position elapsed, offset)
        ringDotOpacity u

      override _.Velocity(_elapsed: float) : float = 0.0

      override _.Done(_elapsed: float) : bool = false
  }

  let zeroYFactory =
    Func<float, float, float, Simulation>(fun _ _ _ -> constantInfinite 0.0)

[<AbstractClass>]
type MetroIndeterminateBarCell() =
  inherit Cell<MetroIndeterminateBarInput>()

  abstract TranslateAnims: Anim<Point> array

  abstract OpacityAnims: Anim<float> array

  abstract View: Blob

[<RequireQualifiedAccess>]
module MetroIndeterminateBar =

  /// State of an indeterminate bar cell. One mutable field per concern.
  type BarState = {
    TrackHandle: ElementHandle
    Periodic: Simulation
    mutable Input: MetroIndeterminateBarInput
    mutable PixelWidth: float
    mutable DotSize: float
    mutable Running: bool
    // Slots for the animation values. `create` fills them after the cell
    // exists.
    mutable Translate: Anim<Point> array
    mutable Opacity: Anim<float> array
  }

  /// The values that the functions of an indeterminate bar cell work on.
  type BarContext = {
    State: BarState
    XFactories: Func<float, float, float, Simulation> array
    OpacityFactories: Func<float, float, float, Simulation> array
  }

  let emptyInput: MetroIndeterminateBarInput = {
    Height = 4.0
    Width = Length.Percent 100.0
    AccessibilityName = ValueNone
  }

  let initialState() : BarState = {
    TrackHandle = ElementHandle()
    Periodic = PeriodicSimulation.create ProgressInternals.barPeriodSeconds
    Input = emptyInput
    PixelWidth = 0.0
    DotSize = 4.0
    Running = false
    Translate = Array.empty
    Opacity = Array.empty
  }

  /// The track width and the dot size of the horizontal travel. The width
  /// comes from the measured track, else from the input, else from the
  /// default of 240 px.
  let geometry(state: BarState) : struct (float * float) =
    let boxWidth = state.TrackHandle.BorderBox.Width

    let measured =
      if Double.IsFinite boxWidth && boxWidth > 0.0 then boxWidth
      elif state.PixelWidth > 0.0 then state.PixelWidth
      else 240.0

    struct (measured, state.DotSize)

  let xFactoriesOf
    (state: BarState)
    : Func<float, float, float, Simulation> array =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.barStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.barDotXSimulation(
          state.Periodic,
          offset,
          fun () -> geometry state
        )))

  let opacityFactoriesOf
    (state: BarState)
    : Func<float, float, float, Simulation> array =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.barStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.barDotOpacitySimulation(state.Periodic, offset)))

  let stopDots(state: BarState) : unit =
    if state.Running then
      for i = 0 to ProgressInternals.dotCount - 1 do
        state.Translate[i].Snap(Point(X = -state.DotSize, Y = 0.0))
        state.Opacity[i].Snap(0.0)

      state.Running <- false

  let startDots(ctx: BarContext) : unit =
    let state = ctx.State

    if not state.Running then
      for i = 0 to ProgressInternals.dotCount - 1 do
        state.Translate[i]
          .Play(
            ProgressInternals.zeroPoint,
            ProgressInternals.zeroPoint,
            ctx.XFactories[i],
            ProgressInternals.zeroYFactory
          )

        state.Opacity[i].Play(0.0, 1.0, ctx.OpacityFactories[i])

      state.Running <- true

  let view
    (ctx: BarContext, input: MetroIndeterminateBarInput, theme: MetroTheme)
    : Blob =
    let state = ctx.State
    let accName = input.AccessibilityName |> ValueOption.defaultValue "Loading"

    let accessibility =
      Accessibility(
        Role = AccessibilityRole.ProgressBar,
        Name = accName,
        Busy = Nullable true
      )

    let track =
      Container(
        Handle = state.TrackHandle,
        Width = input.Width,
        Height = Length.Pixels state.DotSize,
        Position = PositionType.Relative,
        BackgroundColor = Color.Transparent,
        BorderRadius = ProgressInternals.zeroRadius,
        BoxShadows = ProgressInternals.emptyShadows,
        Overflow = Overflow.Hidden,
        Accessibility = accessibility
      )

    if theme.ReducedMotion then
      // A static indicator bar of 30 percent of the track.
      let indicator =
        Container(
          Width = Length.Percent 30.0,
          Height = Length.Percent 100.0,
          BackgroundColor = theme.Accent.Base,
          BorderRadius = ProgressInternals.zeroRadius,
          BoxShadows = ProgressInternals.emptyShadows
        )

      track.BackgroundColor <-
        (if theme.HighContrast then
           theme.Palette.BorderStrong
         else
           theme.Palette.Border)

      track.JustifyContent <- JustifyContent.Center
      track.FlexDirection <- FlexDirection.Row
      track.Add(indicator)
    else
      for i = 0 to ProgressInternals.dotCount - 1 do
        track.Add(
          Container(
            Width = Length.Pixels state.DotSize,
            Height = Length.Pixels state.DotSize,
            Position = PositionType.Absolute,
            Left = ProgressInternals.zeroRadius,
            Top = ProgressInternals.zeroRadius,
            BorderRadius = ProgressInternals.zeroRadius,
            BoxShadows = ProgressInternals.emptyShadows,
            BackgroundColor = theme.Accent.Base,
            TranslateAnim = state.Translate[i],
            OpacityAnim = state.Opacity[i]
          )
        )

    track

  /// Reads the theme and the geometry of the input, starts or stops the
  /// dots, then builds the tree.
  let build(ctx: BarContext, input: MetroIndeterminateBarInput) : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    state.DotSize <- ProgressInternals.resolveBarHeight input.Height

    state.PixelWidth <-
      (if
         not input.Width.IsPercent
         && input.Width.HasMagnitude
         && Double.IsFinite input.Width.Magnitude
         && input.Width.Magnitude > 0.0
       then
         input.Width.Magnitude
       else
         0.0)

    if theme.ReducedMotion then
      stopDots state
    else
      startDots ctx

    view(ctx, input, theme)

  let create() : MetroIndeterminateBarCell =
    let state = initialState()

    // The factories are made once: each dot keeps its simulation spec.
    let context: BarContext = {
      State = state
      XFactories = xFactoriesOf state
      OpacityFactories = opacityFactoriesOf state
    }

    let cell = {
      new MetroIndeterminateBarCell() with
        override _.TranslateAnims = state.Translate
        override _.OpacityAnims = state.Opacity

        override _.View = view(context, state.Input, Metro.read())

        override _.Unmounting() = stopDots state

        override _.Build(input) : Blob = build(context, input)
    }

    // The cell exists now: Goo ties each animation to it.
    state.Translate <-
      Array.init ProgressInternals.dotCount (fun _ ->
        cell.AnimateValue(Point(X = -4.0, Y = 0.0)))

    state.Opacity <-
      Array.init ProgressInternals.dotCount (fun _ -> cell.AnimateValue(0.0))

    cell

[<RequireQualifiedAccess>]
module MetroProgressBar =

  let determinate(value: float) : MetroProgressBarProps = {
    Value = ValueSome value
    Height = 4.0
    Width = Length.Percent 100.0
    AccessibilityName = ValueNone
  }

  let indeterminate: MetroProgressBarProps = {
    Value = ValueNone
    Height = 4.0
    Width = Length.Percent 100.0
    AccessibilityName = ValueNone
  }

  let buildWith
    (
      key: string,
      props: MetroProgressBarProps,
      factory: unit -> MetroIndeterminateBarCell
    ) : Blob =
    props.Value
    |> ValueOption.map(fun rawValue ->
      let theme = Metro.read()
      let clamped = ProgressInternals.clamp01 rawValue
      let height = ProgressInternals.resolveBarHeight props.Height

      let accName =
        props.AccessibilityName |> ValueOption.defaultValue "Progress"

      let fill =
        Container(
          Width = Length.Percent(clamped * 100.0),
          Height = Length.Percent 100.0,
          BackgroundColor = theme.Accent.Base,
          BorderRadius = ProgressInternals.zeroRadius,
          BoxShadows = ProgressInternals.emptyShadows,
          TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
          TransitionEasing = Easing.EaseOut
        )

      let trackBg =
        if theme.HighContrast then
          theme.Palette.BorderStrong
        else
          theme.Palette.Border

      Container(
        Key = key,
        Width = props.Width,
        Height = Length.Pixels height,
        FlexDirection = FlexDirection.Row,
        BackgroundColor = trackBg,
        BorderRadius = ProgressInternals.zeroRadius,
        BoxShadows = ProgressInternals.emptyShadows,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.ProgressBar,
            Name = accName,
            Range =
              AccessibilityValue(
                Minimum = Nullable 0.0,
                Maximum = Nullable 1.0,
                Now = Nullable clamped
              )
          )
      )
        .Children(fill)
      :> Blob)
    |> ValueOption.defaultWith(fun () ->
      let input: MetroIndeterminateBarInput = {
        Height = props.Height
        Width = props.Width
        AccessibilityName = props.AccessibilityName
      }

      Cell.Mount<MetroIndeterminateBarInput, MetroIndeterminateBarCell>(
        key,
        input,
        Func<MetroIndeterminateBarCell>(factory)
      ))

  let build(key: string, props: MetroProgressBarProps) : Blob =
    buildWith(key, props, MetroIndeterminateBar.create)

/// Builds a determinate bar, or mounts the indeterminate cell that
/// <c>factory</c> creates. A test passes a factory to keep the cell.

[<AbstractClass>]
type MetroProgressRingCell() =
  inherit Cell<MetroProgressRingProps>()

  abstract TranslateAnims: Anim<Point> array

  abstract OpacityAnims: Anim<float> array

  abstract View: Blob

[<RequireQualifiedAccess>]
module MetroProgressRing =

  let defaults(active: bool) : MetroProgressRingProps = {
    Active = active
    Size = MetroProgressRingSize.Normal
    Color = ValueNone
    AccessibilityName = ValueNone
  }

  /// State of a progress ring cell. One mutable field per concern.
  type RingState = {
    Periodic: Simulation
    mutable Input: MetroProgressRingProps
    mutable BoxSize: float
    mutable Radius: float
    mutable DotSize: float
    mutable Running: bool
    // Slots for the animation values. `create` fills them after the cell
    // exists.
    mutable Translate: Anim<Point> array
    mutable Opacity: Anim<float> array
  }

  /// The values that the functions of a progress ring cell work on.
  type RingContext = {
    State: RingState
    XFactories: Func<float, float, float, Simulation> array
    YFactories: Func<float, float, float, Simulation> array
    OpacityFactories: Func<float, float, float, Simulation> array
  }

  let initialInput: MetroProgressRingProps = {
    Active = false
    Size = MetroProgressRingSize.Normal
    Color = ValueNone
    AccessibilityName = ValueNone
  }

  let initialState() : RingState = {
    Periodic = PeriodicSimulation.create ProgressInternals.ringPeriodSeconds
    Input = initialInput
    BoxSize = 36.0
    Radius = 14.0
    DotSize = 5.0
    Running = false
    Translate = Array.empty
    Opacity = Array.empty
  }

  /// The box size, the orbit radius, and the dot size of the travel.
  let geometry(state: RingState) : struct (float * float * float) =
    struct (state.BoxSize, state.Radius, state.DotSize)

  let axisFactoriesOf
    (state: RingState, isX: bool)
    : Func<float, float, float, Simulation> array =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.ringStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.ringDotAxisSimulation {
          Periodic = state.Periodic
          Offset = offset
          IsX = isX
          Geometry = fun () -> geometry state
        }))

  let opacityFactoriesOf
    (state: RingState)
    : Func<float, float, float, Simulation> array =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.ringStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.ringDotOpacitySimulation(state.Periodic, offset)))

  let startOrbit(ctx: RingContext) : unit =
    let state = ctx.State

    if not state.Running then
      for i = 0 to ProgressInternals.dotCount - 1 do
        state.Translate[i]
          .Play(
            ProgressInternals.zeroPoint,
            ProgressInternals.zeroPoint,
            ctx.XFactories[i],
            ctx.YFactories[i]
          )

        state.Opacity[i].Play(0.0, 1.0, ctx.OpacityFactories[i])

      state.Running <- true

  /// Puts the dots on the orbit at rest. The opacity fades from dot to dot.
  let snapStaticRing(state: RingState) : unit =
    let center = (state.BoxSize - state.DotSize) * 0.5
    let step = 2.0 * Math.PI / float ProgressInternals.dotCount

    for i = 0 to ProgressInternals.dotCount - 1 do
      let angle = -Math.PI * 0.5 + float i * step
      let x = center + state.Radius * Math.Cos(angle)
      let y = center + state.Radius * Math.Sin(angle)
      state.Translate[i].Snap(Point(X = x, Y = y))
      state.Opacity[i].Snap(1.0 - float i * 0.14)

    state.Running <- false

  /// Puts all the dots in the center, fully transparent.
  let snapHidden(state: RingState) : unit =
    let center = (state.BoxSize - state.DotSize) * 0.5
    let centerPt = Point(X = center, Y = center)

    for i = 0 to ProgressInternals.dotCount - 1 do
      state.Translate[i].Snap(centerPt)
      state.Opacity[i].Snap(0.0)

    state.Running <- false

  let view
    (ctx: RingContext, input: MetroProgressRingProps, theme: MetroTheme)
    : Blob =
    let state = ctx.State
    let dotColor = input.Color |> ValueOption.defaultValue theme.Accent.Base
    let accName = input.AccessibilityName |> ValueOption.defaultValue "Loading"

    let root =
      Container(
        Width = Length.Pixels state.BoxSize,
        Height = Length.Pixels state.BoxSize,
        Position = PositionType.Relative,
        Visibility =
          (if input.Active then
             Visibility.Visible
           else
             Visibility.Hidden),
        BackgroundColor = Color.Transparent,
        BoxShadows = ProgressInternals.emptyShadows,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.ProgressBar,
            Name = accName,
            Busy = Nullable input.Active
          )
      )

    let dotRadius = Length.Pixels(state.DotSize * 0.5)

    for i = 0 to ProgressInternals.dotCount - 1 do
      root.Add(
        Container(
          Width = Length.Pixels state.DotSize,
          Height = Length.Pixels state.DotSize,
          Position = PositionType.Absolute,
          Left = ProgressInternals.zeroRadius,
          Top = ProgressInternals.zeroRadius,
          BorderRadius = dotRadius,
          BoxShadows = ProgressInternals.emptyShadows,
          BackgroundColor = dotColor,
          TranslateAnim = state.Translate[i],
          OpacityAnim = state.Opacity[i]
        )
      )

    root

  /// Reads the theme and the metrics of the size, starts, freezes, or hides
  /// the dots, then builds the tree.
  let build(ctx: RingContext, input: MetroProgressRingProps) : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()

    let struct (nextBox, nextRadius, nextDot) =
      ProgressInternals.ringMetrics input.Size

    state.BoxSize <- nextBox
    state.Radius <- nextRadius
    state.DotSize <- nextDot

    if not input.Active then snapHidden state
    elif theme.ReducedMotion then snapStaticRing state
    else startOrbit ctx

    view(ctx, input, theme)

  let create() : MetroProgressRingCell =
    let state = initialState()

    // The factories are made once: each dot keeps its simulation spec.
    let context: RingContext = {
      State = state
      XFactories = axisFactoriesOf(state, true)
      YFactories = axisFactoriesOf(state, false)
      OpacityFactories = opacityFactoriesOf state
    }

    let cell = {
      new MetroProgressRingCell() with
        override _.TranslateAnims = state.Translate
        override _.OpacityAnims = state.Opacity

        override _.View = view(context, state.Input, Metro.read())

        override _.Unmounting() = snapHidden state

        override _.Build(input) : Blob = build(context, input)
    }

    // The cell exists now: Goo ties each animation to it.
    state.Translate <-
      Array.init ProgressInternals.dotCount (fun _ ->
        cell.AnimateValue(ProgressInternals.zeroPoint))

    state.Opacity <-
      Array.init ProgressInternals.dotCount (fun _ -> cell.AnimateValue(0.0))

    cell

  /// Mounts the ring cell that <c>factory</c> creates. A test passes a
  /// factory to keep the cell.
  let mountWith
    (
      key: string,
      props: MetroProgressRingProps,
      factory: unit -> MetroProgressRingCell
    ) : Blob =
    Cell.Mount<MetroProgressRingProps, MetroProgressRingCell>(
      key,
      props,
      Func<MetroProgressRingCell>(factory)
    )

  let mount(key: string, props: MetroProgressRingProps) : Blob =
    mountWith(key, props, create)
