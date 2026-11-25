namespace FunGoo.Metro

open System
open Goo

// The arguments of a cubic Bezier simulation.
[<NoComparison; NoEquality>]
type CubicBezierArgs = {
  DurationSeconds: float
  FromValue: float
  ToValue: float
  X1: float
  Y1: float
  X2: float
  Y2: float
}

// The arguments of an exponential ease-out simulation.
[<NoComparison; NoEquality>]
type ExponentialEaseArgs = {
  DurationSeconds: float
  FromValue: float
  ToValue: float
  Exponent: float
}

[<RequireQualifiedAccess>]
module CubicBezierSimulation =

  // The Bezier coefficients of the two axes.
  type Curve = {
    Cx: float
    Bx: float
    Ax: float
    Cy: float
    By: float
    Ay: float
  }

  let validate(args: CubicBezierArgs) : unit =
    if
      not(Double.IsFinite args.DurationSeconds) || args.DurationSeconds < 0.0
    then
      raise(ArgumentOutOfRangeException("durationSeconds"))

    if not(Double.IsFinite args.FromValue) then
      raise(ArgumentOutOfRangeException("fromValue"))

    if not(Double.IsFinite args.ToValue) then
      raise(ArgumentOutOfRangeException("toValue"))

    if not(Double.IsFinite args.X1) || args.X1 < 0.0 || args.X1 > 1.0 then
      raise(ArgumentOutOfRangeException("x1"))

    if not(Double.IsFinite args.Y1) then
      raise(ArgumentOutOfRangeException("y1"))

    if not(Double.IsFinite args.X2) || args.X2 < 0.0 || args.X2 > 1.0 then
      raise(ArgumentOutOfRangeException("x2"))

    if not(Double.IsFinite args.Y2) then
      raise(ArgumentOutOfRangeException("y2"))

  let curveOf(args: CubicBezierArgs) : Curve =
    let cx = 3.0 * args.X1
    let bx = 3.0 * (args.X2 - args.X1) - cx
    let ax = 1.0 - cx - bx
    let cy = 3.0 * args.Y1
    let by = 3.0 * (args.Y2 - args.Y1) - cy
    let ay = 1.0 - cy - by

    {
      Cx = cx
      Bx = bx
      Ax = ax
      Cy = cy
      By = by
      Ay = ay
    }

  let sampleX(curve: Curve, u: float) : float =
    ((curve.Ax * u + curve.Bx) * u + curve.Cx) * u

  let sampleY(curve: Curve, u: float) : float =
    ((curve.Ay * u + curve.By) * u + curve.Cy) * u

  let sampleDX(curve: Curve, u: float) : float =
    (3.0 * curve.Ax * u + 2.0 * curve.Bx) * u + curve.Cx

  let sampleDY(curve: Curve, u: float) : float =
    (3.0 * curve.Ay * u + 2.0 * curve.By) * u + curve.Cy

  // Newton on x(u) = t first, then a bisection when Newton did not converge.
  let solveU(curve: Curve, t: float) : float =
    if t <= 0.0 then
      0.0
    elif t >= 1.0 then
      1.0
    else
      let mutable lo = 0.0
      let mutable hi = 1.0
      let mutable u = t
      let mutable i = 0
      let mutable converged = false

      while i < 8 && not converged do
        let err = sampleX(curve, u) - t

        if abs err < 1e-10 then
          converged <- true
        else
          if err < 0.0 then lo <- u else hi <- u

          let dx = sampleDX(curve, u)

          if abs dx >= 1e-6 then
            let nextU = u - err / dx

            if nextU > lo && nextU < hi then
              u <- nextU
            else
              u <- 0.5 * (lo + hi)
          else
            u <- 0.5 * (lo + hi)

          i <- i + 1

      if not converged then
        let mutable iter = 0

        while iter < 50 && not converged do
          let err = sampleX(curve, u) - t

          if abs err < 1e-10 || (hi - lo) < 1e-12 then
            converged <- true
          else
            if err < 0.0 then lo <- u else hi <- u

            u <- 0.5 * (lo + hi)
            iter <- iter + 1

      u

  let position(curve: Curve, args: CubicBezierArgs, elapsed: float) : float =
    if elapsed < 0.0 then
      args.FromValue
    elif args.DurationSeconds <= 0.0 then
      args.ToValue
    elif elapsed = 0.0 then
      args.FromValue
    elif elapsed >= args.DurationSeconds then
      args.ToValue
    else
      let t = elapsed / args.DurationSeconds
      let u = solveU(curve, t)
      args.FromValue + (args.ToValue - args.FromValue) * sampleY(curve, u)

  let velocity(curve: Curve, args: CubicBezierArgs, elapsed: float) : float =
    if
      args.DurationSeconds <= 0.0
      || elapsed < 0.0
      || elapsed >= args.DurationSeconds
    then
      0.0
    else
      let t = elapsed / args.DurationSeconds
      let u = solveU(curve, t)
      let dx = sampleDX(curve, u)
      let dy = sampleDY(curve, u)

      let slope =
        if abs dx >= 1e-9 then
          dy / dx
        else
          // Finite-difference fallback when dx vanishes (vertical or 0/0).
          let step = 1e-5
          let t2 = if t + step <= 1.0 then t + step else t - step
          let u2 = solveU(curve, t2)
          let dxStep = sampleX(curve, u2) - sampleX(curve, u)

          if abs dxStep >= 1e-12 then
            (sampleY(curve, u2) - sampleY(curve, u)) / dxStep
          else
            0.0

      (args.ToValue - args.FromValue) * slope / args.DurationSeconds

  let create(args: CubicBezierArgs) : Simulation =
    validate args
    let curve = curveOf args

    {
      new Simulation() with
        override _.Position(elapsed: float) : float =
          position(curve, args, elapsed)

        override _.Velocity(elapsed: float) : float =
          velocity(curve, args, elapsed)

        override _.Done(elapsed: float) : bool = elapsed >= args.DurationSeconds
    }

[<RequireQualifiedAccess>]
module ExponentialEaseSimulation =

  // The values that each evaluation needs, resolved once.
  type Curve = {
    Delta: float
    IsLinear: bool
    InvDenom: float
    OverflowNegative: bool
  }

  let validate(args: ExponentialEaseArgs) : unit =
    if
      not(Double.IsFinite args.DurationSeconds) || args.DurationSeconds < 0.0
    then
      raise(ArgumentOutOfRangeException("durationSeconds"))

    if not(Double.IsFinite args.FromValue) then
      raise(ArgumentOutOfRangeException("fromValue"))

    if not(Double.IsFinite args.ToValue) then
      raise(ArgumentOutOfRangeException("toValue"))

    if not(Double.IsFinite args.Exponent) then
      raise(ArgumentOutOfRangeException("exponent"))

  let curveOf(args: ExponentialEaseArgs) : Curve =
    let isLinear = abs args.Exponent < 1e-6
    let expNegE = if isLinear then 1.0 else Math.Exp(-args.Exponent)

    let invDenom =
      if isLinear || not(Double.IsFinite expNegE) then
        1.0
      else
        1.0 / (1.0 - expNegE)

    {
      Delta = args.ToValue - args.FromValue
      IsLinear = isLinear
      InvDenom = invDenom
      OverflowNegative = not isLinear && not(Double.IsFinite expNegE)
    }

  let progress(curve: Curve, exponent: float, t: float) : float =
    if curve.IsLinear then
      t
    elif curve.OverflowNegative then
      Math.Exp(exponent * (1.0 - t))
    else
      (1.0 - Math.Exp(-exponent * t)) * curve.InvDenom

  let position
    (curve: Curve, args: ExponentialEaseArgs, elapsed: float)
    : float =
    if elapsed < 0.0 then
      args.FromValue
    elif args.DurationSeconds <= 0.0 then
      args.ToValue
    elif elapsed = 0.0 then
      args.FromValue
    elif elapsed >= args.DurationSeconds then
      args.ToValue
    else
      let t = elapsed / args.DurationSeconds
      args.FromValue + curve.Delta * progress(curve, args.Exponent, t)

  let velocity
    (curve: Curve, args: ExponentialEaseArgs, elapsed: float)
    : float =
    if
      args.DurationSeconds <= 0.0
      || elapsed < 0.0
      || elapsed >= args.DurationSeconds
    then
      0.0
    else
      let t = elapsed / args.DurationSeconds

      let slope =
        if curve.IsLinear then
          1.0
        elif curve.OverflowNegative then
          -args.Exponent * Math.Exp(args.Exponent * (1.0 - t))
        else
          args.Exponent * Math.Exp(-args.Exponent * t) * curve.InvDenom

      curve.Delta * slope / args.DurationSeconds

  let create(args: ExponentialEaseArgs) : Simulation =
    validate args
    let curve = curveOf args

    {
      new Simulation() with
        override _.Position(elapsed: float) : float =
          position(curve, args, elapsed)

        override _.Velocity(elapsed: float) : float =
          velocity(curve, args, elapsed)

        override _.Done(elapsed: float) : bool = elapsed >= args.DurationSeconds
    }

[<RequireQualifiedAccess>]
module PeriodicSimulation =

  let validate(periodSeconds: float) : unit =
    if
      not(Double.IsFinite periodSeconds)
      || periodSeconds <= 0.0
      || not(Double.IsFinite(1.0 / periodSeconds))
    then
      raise(ArgumentOutOfRangeException("periodSeconds"))

  // The phase in [0.0, 1.0).
  let phase(invPeriod: float, elapsed: float) : float =
    let raw = (elapsed * invPeriod) % 1.0

    if raw < 0.0 then
      let wrapped = raw + 1.0
      if wrapped >= 1.0 then 0.0 else wrapped
    elif raw = 0.0 || raw >= 1.0 then
      0.0
    else
      raw

  let create(periodSeconds: float) : Simulation =
    validate periodSeconds
    let invPeriod = 1.0 / periodSeconds

    {
      new Simulation() with
        override _.IsInfinite = true
        override _.Position(elapsed: float) : float = phase(invPeriod, elapsed)
        override _.Velocity(_elapsed: float) : float = invPeriod
        override _.Done(_elapsed: float) : bool = false
    }

[<Struct; NoComparison>]
type MetroCubicBezierArgs = {
  DurationMs: float
  X1: float
  Y1: float
  X2: float
  Y2: float
}

[<RequireQualifiedAccess>]
module MetroMotion =

  let instant = Motion.Tween(0.0)

  let cubicBezier
    (args: MetroCubicBezierArgs)
    : Func<float, float, float, Simulation> =
    if not(Double.IsFinite args.DurationMs) || args.DurationMs < 0.0 then
      raise(ArgumentOutOfRangeException(nameof args.DurationMs))

    if not(Double.IsFinite args.X1) || args.X1 < 0.0 || args.X1 > 1.0 then
      raise(ArgumentOutOfRangeException(nameof args.X1))

    if not(Double.IsFinite args.Y1) then
      raise(ArgumentOutOfRangeException(nameof args.Y1))

    if not(Double.IsFinite args.X2) || args.X2 < 0.0 || args.X2 > 1.0 then
      raise(ArgumentOutOfRangeException(nameof args.X2))

    if not(Double.IsFinite args.Y2) then
      raise(ArgumentOutOfRangeException(nameof args.Y2))

    let durationSeconds = args.DurationMs / 1000.0

    Func<float, float, float, Simulation>(fun fromValue toValue _velocity ->
      CubicBezierSimulation.create {
        DurationSeconds = durationSeconds
        FromValue = fromValue
        ToValue = toValue
        X1 = args.X1
        Y1 = args.Y1
        X2 = args.X2
        Y2 = args.Y2
      })

  let exponentialOut
    (durationMs: float, exponent: float)
    : Func<float, float, float, Simulation> =
    if not(Double.IsFinite durationMs) || durationMs < 0.0 then
      raise(ArgumentOutOfRangeException(nameof durationMs))

    if not(Double.IsFinite exponent) then
      raise(ArgumentOutOfRangeException(nameof exponent))

    let durationSeconds = durationMs / 1000.0

    Func<float, float, float, Simulation>(fun fromValue toValue _velocity ->
      ExponentialEaseSimulation.create {
        DurationSeconds = durationSeconds
        FromValue = fromValue
        ToValue = toValue
        Exponent = exponent
      })

  let decelerate(durationMs: float) : Func<float, float, float, Simulation> =
    cubicBezier {
      DurationMs = durationMs
      X1 = 0.1
      Y1 = 0.9
      X2 = 0.2
      Y2 = 1.0
    }

  let pageGlide(durationMs: float) : Func<float, float, float, Simulation> =
    cubicBezier {
      DurationMs = durationMs
      X1 = 0.25
      Y1 = 0.46
      X2 = 0.45
      Y2 = 0.94
    }

  let fast = decelerate MetroTiming.fastMs
  let normal = decelerate MetroTiming.normalMs
  let slow = decelerate MetroTiming.slowMs
  let turnstileIn = pageGlide MetroTiming.turnstileInMs
  let turnstileOut = pageGlide MetroTiming.turnstileOutMs
  let continuum = exponentialOut(MetroTiming.continuumMs, 6.0)

  let staggerDelayMs(index: int) : float =
    if index <= 0 then
      0.0
    else
      let table = MetroTiming.staggerDelaysMs

      if index < table.Count then
        table[index]
      else
        let lastIndex = table.Count - 1
        let lastValue = table[lastIndex]
        let extra = float(index - lastIndex)
        // Smooth geometric tail continuing step decay toward 400 ms.
        lastValue + 144.0 * (1.0 - Math.Pow(0.9, extra))

  let staggered
    (index: int, spec: Func<float, float, float, Simulation>)
    : Func<float, float, float, Simulation> =
    Motion.Delay(staggerDelayMs index, spec)

  let forTheme
    (theme: MetroTheme, spec: Func<float, float, float, Simulation>)
    : Func<float, float, float, Simulation> =
    if isNull spec then
      raise(ArgumentNullException(nameof spec))

    if theme.ReducedMotion then instant else spec

[<Struct>]
type ContinuumDelta = { OffsetX: float; OffsetY: float }

[<NoComparison>]
type MetroContinuumPlayArgs = {
  Theme: MetroTheme
  Translate: Anim<Point>
  Opacity: Anim<float>
  Delta: ContinuumDelta
}

[<RequireQualifiedAccess>]
module Continuum =

  let defaultEnterDelta: ContinuumDelta = { OffsetX = 130.0; OffsetY = -70.0 }
  let defaultExitDelta: ContinuumDelta = { OffsetX = 225.0; OffsetY = 73.0 }

  let measureDelta
    (origin: ElementRect, destination: ElementRect)
    : ContinuumDelta = {
    OffsetX = origin.X - destination.X
    OffsetY = origin.Y - destination.Y
  }

  let playEnter(args: MetroContinuumPlayArgs) : unit =
    if isNull args.Translate then
      raise(ArgumentNullException(nameof args.Translate))

    if isNull args.Opacity then
      raise(ArgumentNullException(nameof args.Opacity))

    let spec = MetroMotion.forTheme(args.Theme, MetroMotion.continuum)
    let startPoint = Point(X = args.Delta.OffsetX, Y = args.Delta.OffsetY)
    let endPoint = Point(X = 0.0, Y = 0.0)
    args.Translate.Play(startPoint, endPoint, spec)
    args.Opacity.Play(0.0, 1.0, spec)

  let playExit(args: MetroContinuumPlayArgs) : unit =
    if isNull args.Translate then
      raise(ArgumentNullException(nameof args.Translate))

    if isNull args.Opacity then
      raise(ArgumentNullException(nameof args.Opacity))

    let spec = MetroMotion.forTheme(args.Theme, MetroMotion.continuum)
    let startPoint = Point(X = 0.0, Y = 0.0)
    let endPoint = Point(X = args.Delta.OffsetX, Y = args.Delta.OffsetY)
    args.Translate.Play(startPoint, endPoint, spec)
    args.Opacity.Play(1.0, 0.0, spec)

[<RequireQualifiedAccess>]
module MetroFallbackMotion =

  let clamp01(value: float) : float =
    if not(Double.IsFinite value) || value <= 0.0 then 0.0
    elif value >= 1.0 then 1.0
    else value

  let turnstile2D
    (progress: float, entering: bool)
    : struct (PanelTransform * float) =
    let p = clamp01 progress

    if entering then
      let remaining = 1.0 - p

      if remaining = 0.0 then
        struct (PanelTransform(), 1.0)
      else
        let scaleX = 1.0 - 0.08 * remaining
        let translateX = 36.0 * remaining

        struct (PanelTransform(
                  ScaleX = scaleX,
                  TranslateX = Length.Pixels translateX
                ),
                p)
    elif p = 0.0 then
      struct (PanelTransform(), 1.0)
    else
      let scaleX = 1.0 - 0.12 * p
      let translateX = -48.0 * p

      struct (PanelTransform(
                ScaleX = scaleX,
                TranslateX = Length.Pixels translateX
              ),
              1.0 - p)

  let swivel2D
    (progress: float, entering: bool)
    : struct (PanelTransform * float) =
    let p = clamp01 progress

    if entering then
      let remaining = 1.0 - p

      if remaining = 0.0 then
        struct (PanelTransform(), 1.0)
      else
        let scaleY = 1.0 - 0.85 * remaining
        let translateY = -20.0 * remaining

        struct (PanelTransform(
                  ScaleY = scaleY,
                  TranslateY = Length.Pixels translateY
                ),
                p)
    elif p = 0.0 then
      struct (PanelTransform(), 1.0)
    else
      let scaleY = 1.0 - 0.85 * p
      let translateY = 20.0 * p

      struct (PanelTransform(
                ScaleY = scaleY,
                TranslateY = Length.Pixels translateY
              ),
              1.0 - p)
