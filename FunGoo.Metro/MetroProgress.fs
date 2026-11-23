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
type internal MetroIndeterminateBarInput = {
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

module private ProgressInternals =

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

[<Sealed>]
type internal MetroIndeterminateBarCell() as this =
  inherit Cell<MetroIndeterminateBarInput>()

  let periodic = PeriodicSimulation.create ProgressInternals.barPeriodSeconds
  let trackHandle = ElementHandle()

  let mutable pixelWidth = 0.0
  let mutable dotSize = 4.0
  let mutable running = false

  let getGeometry() : struct (float * float) =
    let boxWidth = trackHandle.BorderBox.Width

    let measured =
      if Double.IsFinite boxWidth && boxWidth > 0.0 then boxWidth
      elif pixelWidth > 0.0 then pixelWidth
      else 240.0

    struct (measured, dotSize)

  let translateAnims =
    Array.init ProgressInternals.dotCount (fun _ ->
      this.AnimateValue(Point(X = -4.0, Y = 0.0)))

  let opacityAnims =
    Array.init ProgressInternals.dotCount (fun _ -> this.AnimateValue(0.0))

  let xFactories =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.barStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.barDotXSimulation(periodic, offset, getGeometry)))

  let opacityFactories =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.barStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.barDotOpacitySimulation(periodic, offset)))

  let startDots() =
    if not running then
      for i = 0 to ProgressInternals.dotCount - 1 do
        translateAnims[i]
          .Play(
            ProgressInternals.zeroPoint,
            ProgressInternals.zeroPoint,
            xFactories[i],
            ProgressInternals.zeroYFactory
          )

        opacityAnims[i].Play(0.0, 1.0, opacityFactories[i])

      running <- true

  let stopDots() =
    if running then
      for i = 0 to ProgressInternals.dotCount - 1 do
        translateAnims[i].Snap(Point(X = -dotSize, Y = 0.0))
        opacityAnims[i].Snap(0.0)

      running <- false

  override _.Unmounting() = stopDots()

  override _.Build(input: MetroIndeterminateBarInput) : Blob =
    let theme = Metro.read()
    dotSize <- ProgressInternals.resolveBarHeight input.Height

    pixelWidth <-
      if
        not input.Width.IsPercent
        && input.Width.HasMagnitude
        && Double.IsFinite input.Width.Magnitude
        && input.Width.Magnitude > 0.0
      then
        input.Width.Magnitude
      else
        0.0

    let accName = input.AccessibilityName |> ValueOption.defaultValue "Loading"

    if theme.ReducedMotion then
      stopDots()

      let indicator =
        Container(
          Width = Length.Percent 30.0,
          Height = Length.Percent 100.0,
          BackgroundColor = theme.Accent.Base,
          BorderRadius = ProgressInternals.zeroRadius,
          BoxShadows = ProgressInternals.emptyShadows
        )

      let trackBg =
        if theme.HighContrast then
          theme.Palette.BorderStrong
        else
          theme.Palette.Border

      Container(
        Handle = trackHandle,
        Width = input.Width,
        Height = Length.Pixels dotSize,
        FlexDirection = FlexDirection.Row,
        JustifyContent = JustifyContent.Center,
        BackgroundColor = trackBg,
        BorderRadius = ProgressInternals.zeroRadius,
        BoxShadows = ProgressInternals.emptyShadows,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.ProgressBar,
            Name = accName,
            Busy = Nullable true
          )
      )
        .Children(indicator)
    else
      startDots()

      let track =
        Container(
          Handle = trackHandle,
          Width = input.Width,
          Height = Length.Pixels dotSize,
          Position = PositionType.Relative,
          BackgroundColor = Color.Transparent,
          BorderRadius = ProgressInternals.zeroRadius,
          BoxShadows = ProgressInternals.emptyShadows,
          Overflow = Overflow.Hidden,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.ProgressBar,
              Name = accName,
              Busy = Nullable true
            )
        )

      for i = 0 to ProgressInternals.dotCount - 1 do
        track.Add(
          Container(
            Width = Length.Pixels dotSize,
            Height = Length.Pixels dotSize,
            Position = PositionType.Absolute,
            Left = ProgressInternals.zeroRadius,
            Top = ProgressInternals.zeroRadius,
            BorderRadius = ProgressInternals.zeroRadius,
            BoxShadows = ProgressInternals.emptyShadows,
            BackgroundColor = theme.Accent.Base,
            TranslateAnim = translateAnims[i],
            OpacityAnim = opacityAnims[i]
          )
        )

      track

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

  let build(key: string, props: MetroProgressBarProps) : Blob =
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
        input
      ))

[<Sealed>]
type internal MetroProgressRingCell() as this =
  inherit Cell<MetroProgressRingProps>()

  let periodic = PeriodicSimulation.create ProgressInternals.ringPeriodSeconds

  let mutable boxSize = 36.0
  let mutable radius = 14.0
  let mutable dotSize = 5.0
  let mutable running = false

  let getGeometry() : struct (float * float * float) =
    struct (boxSize, radius, dotSize)

  let translateAnims =
    Array.init ProgressInternals.dotCount (fun _ ->
      this.AnimateValue(ProgressInternals.zeroPoint))

  let opacityAnims =
    Array.init ProgressInternals.dotCount (fun _ -> this.AnimateValue(0.0))

  let xFactories =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.ringStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.ringDotAxisSimulation {
          Periodic = periodic
          Offset = offset
          IsX = true
          Geometry = getGeometry
        }))

  let yFactories =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.ringStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.ringDotAxisSimulation {
          Periodic = periodic
          Offset = offset
          IsX = false
          Geometry = getGeometry
        }))

  let opacityFactories =
    Array.init ProgressInternals.dotCount (fun i ->
      let offset = float i * ProgressInternals.ringStaggerStep

      Func<float, float, float, Simulation>(fun _ _ _ ->
        ProgressInternals.ringDotOpacitySimulation(periodic, offset)))

  let startOrbit() =
    if not running then
      for i = 0 to ProgressInternals.dotCount - 1 do
        translateAnims[i]
          .Play(
            ProgressInternals.zeroPoint,
            ProgressInternals.zeroPoint,
            xFactories[i],
            yFactories[i]
          )

        opacityAnims[i].Play(0.0, 1.0, opacityFactories[i])

      running <- true

  let snapStaticRing() =
    let center = (boxSize - dotSize) * 0.5
    let step = 2.0 * Math.PI / float ProgressInternals.dotCount

    for i = 0 to ProgressInternals.dotCount - 1 do
      let angle = -Math.PI * 0.5 + float i * step
      let x = center + radius * Math.Cos(angle)
      let y = center + radius * Math.Sin(angle)
      translateAnims[i].Snap(Point(X = x, Y = y))
      opacityAnims[i].Snap(1.0 - float i * 0.14)

    running <- false

  let snapHidden() =
    let center = (boxSize - dotSize) * 0.5
    let centerPt = Point(X = center, Y = center)

    for i = 0 to ProgressInternals.dotCount - 1 do
      translateAnims[i].Snap(centerPt)
      opacityAnims[i].Snap(0.0)

    running <- false

  override _.Unmounting() = snapHidden()

  override _.Build(input: MetroProgressRingProps) : Blob =
    let theme = Metro.read()

    let struct (nextBox, nextRadius, nextDot) =
      ProgressInternals.ringMetrics input.Size

    boxSize <- nextBox
    radius <- nextRadius
    dotSize <- nextDot

    if not input.Active then snapHidden()
    elif theme.ReducedMotion then snapStaticRing()
    else startOrbit()

    let dotColor = input.Color |> ValueOption.defaultValue theme.Accent.Base
    let accName = input.AccessibilityName |> ValueOption.defaultValue "Loading"

    let root =
      Container(
        Width = Length.Pixels boxSize,
        Height = Length.Pixels boxSize,
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

    let dotRadius = Length.Pixels(dotSize * 0.5)

    for i = 0 to ProgressInternals.dotCount - 1 do
      root.Add(
        Container(
          Width = Length.Pixels dotSize,
          Height = Length.Pixels dotSize,
          Position = PositionType.Absolute,
          Left = ProgressInternals.zeroRadius,
          Top = ProgressInternals.zeroRadius,
          BorderRadius = dotRadius,
          BoxShadows = ProgressInternals.emptyShadows,
          BackgroundColor = dotColor,
          TranslateAnim = translateAnims[i],
          OpacityAnim = opacityAnims[i]
        )
      )

    root

[<RequireQualifiedAccess>]
module MetroProgressRing =

  let defaults(active: bool) : MetroProgressRingProps = {
    Active = active
    Size = MetroProgressRingSize.Normal
    Color = ValueNone
    AccessibilityName = ValueNone
  }

  let mount(key: string, props: MetroProgressRingProps) : Blob =
    Cell.Mount<MetroProgressRingProps, MetroProgressRingCell>(key, props)
