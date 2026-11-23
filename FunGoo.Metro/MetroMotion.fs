namespace FunGoo.Metro

open System
open Goo

[<Sealed>]
type CubicBezierSimulation
  (
    durationSeconds: float,
    fromValue: float,
    toValue: float,
    x1: float,
    y1: float,
    x2: float,
    y2: float
  ) =
  inherit Simulation()

  do
    if not(Double.IsFinite durationSeconds) || durationSeconds < 0.0 then
      raise(ArgumentOutOfRangeException(nameof durationSeconds))

    if not(Double.IsFinite fromValue) then
      raise(ArgumentOutOfRangeException(nameof fromValue))

    if not(Double.IsFinite toValue) then
      raise(ArgumentOutOfRangeException(nameof toValue))

    if not(Double.IsFinite x1) || x1 < 0.0 || x1 > 1.0 then
      raise(ArgumentOutOfRangeException(nameof x1))

    if not(Double.IsFinite y1) then
      raise(ArgumentOutOfRangeException(nameof y1))

    if not(Double.IsFinite x2) || x2 < 0.0 || x2 > 1.0 then
      raise(ArgumentOutOfRangeException(nameof x2))

    if not(Double.IsFinite y2) then
      raise(ArgumentOutOfRangeException(nameof y2))

  let cx = 3.0 * x1
  let bx = 3.0 * (x2 - x1) - cx
  let ax = 1.0 - cx - bx

  let cy = 3.0 * y1
  let by = 3.0 * (y2 - y1) - cy
  let ay = 1.0 - cy - by

  let delta = toValue - fromValue

  let sampleX(u: float) = ((ax * u + bx) * u + cx) * u
  let sampleY(u: float) = ((ay * u + by) * u + cy) * u
  let sampleDX(u: float) = (3.0 * ax * u + 2.0 * bx) * u + cx
  let sampleDY(u: float) = (3.0 * ay * u + 2.0 * by) * u + cy

  let solveU(t: float) =
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
        let err = sampleX u - t

        if abs err < 1e-10 then
          converged <- true
        else
          if err < 0.0 then lo <- u else hi <- u
          let dx = sampleDX u

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
          let err = sampleX u - t

          if abs err < 1e-10 || (hi - lo) < 1e-12 then
            converged <- true
          else
            if err < 0.0 then lo <- u else hi <- u
            u <- 0.5 * (lo + hi)
            iter <- iter + 1

      u

  override _.Position(elapsed: float) : float =
    if elapsed < 0.0 then
      fromValue
    elif durationSeconds <= 0.0 then
      toValue
    elif elapsed = 0.0 then
      fromValue
    elif elapsed >= durationSeconds then
      toValue
    else
      let t = elapsed / durationSeconds
      let u = solveU t
      fromValue + delta * sampleY u

  override _.Velocity(elapsed: float) : float =
    if
      durationSeconds <= 0.0 || elapsed < 0.0 || elapsed >= durationSeconds
    then
      0.0
    else
      let t = elapsed / durationSeconds
      let u = solveU t
      let dx = sampleDX u
      let dy = sampleDY u

      let slope =
        if abs dx >= 1e-9 then
          dy / dx
        else
          // Finite-difference fallback when dx vanishes (vertical or 0/0).
          let step = 1e-5
          let t2 = if t + step <= 1.0 then t + step else t - step
          let u2 = solveU t2
          let dxStep = sampleX u2 - sampleX u

          if abs dxStep >= 1e-12 then
            (sampleY u2 - sampleY u) / dxStep
          else
            0.0

      delta * slope / durationSeconds

  override _.Done(elapsed: float) : bool = elapsed >= durationSeconds

[<Sealed>]
type ExponentialEaseSimulation
  (durationSeconds: float, fromValue: float, toValue: float, exponent: float) =
  inherit Simulation()

  do
    if not(Double.IsFinite durationSeconds) || durationSeconds < 0.0 then
      raise(ArgumentOutOfRangeException(nameof durationSeconds))

    if not(Double.IsFinite fromValue) then
      raise(ArgumentOutOfRangeException(nameof fromValue))

    if not(Double.IsFinite toValue) then
      raise(ArgumentOutOfRangeException(nameof toValue))

    if not(Double.IsFinite exponent) then
      raise(ArgumentOutOfRangeException(nameof exponent))

  let delta = toValue - fromValue
  let isLinear = abs exponent < 1e-6
  let expNegE = if isLinear then 1.0 else Math.Exp(-exponent)

  let invDenom =
    if isLinear || not(Double.IsFinite expNegE) then
      1.0
    else
      1.0 / (1.0 - expNegE)

  let overflowNegative = not isLinear && not(Double.IsFinite expNegE)

  override _.Position(elapsed: float) : float =
    if elapsed < 0.0 then
      fromValue
    elif durationSeconds <= 0.0 then
      toValue
    elif elapsed = 0.0 then
      fromValue
    elif elapsed >= durationSeconds then
      toValue
    else
      let t = elapsed / durationSeconds

      let progress =
        if isLinear then t
        elif overflowNegative then Math.Exp(exponent * (1.0 - t))
        else (1.0 - Math.Exp(-exponent * t)) * invDenom

      fromValue + delta * progress

  override _.Velocity(elapsed: float) : float =
    if
      durationSeconds <= 0.0 || elapsed < 0.0 || elapsed >= durationSeconds
    then
      0.0
    else
      let t = elapsed / durationSeconds

      let slope =
        if isLinear then
          1.0
        elif overflowNegative then
          -exponent * Math.Exp(exponent * (1.0 - t))
        else
          exponent * Math.Exp(-exponent * t) * invDenom

      delta * slope / durationSeconds

  override _.Done(elapsed: float) : bool = elapsed >= durationSeconds

[<Sealed>]
type PeriodicSimulation(periodSeconds: float) =
  inherit Simulation()

  do
    if
      not(Double.IsFinite periodSeconds)
      || periodSeconds <= 0.0
      || not(Double.IsFinite(1.0 / periodSeconds))
    then
      raise(ArgumentOutOfRangeException(nameof periodSeconds))

  let invPeriod = 1.0 / periodSeconds

  override _.IsInfinite = true

  override _.Position(elapsed: float) : float =
    let phase = (elapsed * invPeriod) % 1.0

    if phase < 0.0 then
      let wrapped = phase + 1.0
      if wrapped >= 1.0 then 0.0 else wrapped
    elif phase = 0.0 || phase >= 1.0 then
      0.0
    else
      phase

  override _.Velocity(elapsed: float) : float =
    ignore elapsed
    invPeriod

  override _.Done(elapsed: float) : bool =
    ignore elapsed
    false

[<RequireQualifiedAccess>]
module MetroMotion =

  let private instant = Motion.Tween(0.0)

  let cubicBezier
    (durationMs: float, x1: float, y1: float, x2: float, y2: float)
    : Func<float, float, float, Simulation> =
    if not(Double.IsFinite durationMs) || durationMs < 0.0 then
      raise(ArgumentOutOfRangeException(nameof durationMs))

    if not(Double.IsFinite x1) || x1 < 0.0 || x1 > 1.0 then
      raise(ArgumentOutOfRangeException(nameof x1))

    if not(Double.IsFinite y1) then
      raise(ArgumentOutOfRangeException(nameof y1))

    if not(Double.IsFinite x2) || x2 < 0.0 || x2 > 1.0 then
      raise(ArgumentOutOfRangeException(nameof x2))

    if not(Double.IsFinite y2) then
      raise(ArgumentOutOfRangeException(nameof y2))

    let durationSeconds = durationMs / 1000.0

    Func<float, float, float, Simulation>(fun fromValue toValue _velocity ->
      CubicBezierSimulation(
        durationSeconds,
        fromValue,
        toValue,
        x1,
        y1,
        x2,
        y2
      ))

  let exponentialOut
    (durationMs: float, exponent: float)
    : Func<float, float, float, Simulation> =
    if not(Double.IsFinite durationMs) || durationMs < 0.0 then
      raise(ArgumentOutOfRangeException(nameof durationMs))

    if not(Double.IsFinite exponent) then
      raise(ArgumentOutOfRangeException(nameof exponent))

    let durationSeconds = durationMs / 1000.0

    Func<float, float, float, Simulation>(fun fromValue toValue _velocity ->
      ExponentialEaseSimulation(durationSeconds, fromValue, toValue, exponent))

  let decelerate(durationMs: float) : Func<float, float, float, Simulation> =
    cubicBezier(durationMs, 0.1, 0.9, 0.2, 1.0)

  let pageGlide(durationMs: float) : Func<float, float, float, Simulation> =
    cubicBezier(durationMs, 0.25, 0.46, 0.45, 0.94)

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

  let playEnter
    (
      theme: MetroTheme,
      translate: Anim<Point>,
      opacity: Anim<float>,
      delta: ContinuumDelta
    ) : unit =
    if isNull translate then
      raise(ArgumentNullException(nameof translate))

    if isNull opacity then
      raise(ArgumentNullException(nameof opacity))

    let spec = MetroMotion.forTheme(theme, MetroMotion.continuum)
    let startPoint = Point(X = delta.OffsetX, Y = delta.OffsetY)
    let endPoint = Point(X = 0.0, Y = 0.0)
    translate.Play(startPoint, endPoint, spec)
    opacity.Play(0.0, 1.0, spec)

  let playExit
    (
      theme: MetroTheme,
      translate: Anim<Point>,
      opacity: Anim<float>,
      delta: ContinuumDelta
    ) : unit =
    if isNull translate then
      raise(ArgumentNullException(nameof translate))

    if isNull opacity then
      raise(ArgumentNullException(nameof opacity))

    let spec = MetroMotion.forTheme(theme, MetroMotion.continuum)
    let startPoint = Point(X = 0.0, Y = 0.0)
    let endPoint = Point(X = delta.OffsetX, Y = delta.OffsetY)
    translate.Play(startPoint, endPoint, spec)
    opacity.Play(1.0, 0.0, spec)

[<RequireQualifiedAccess>]
module MetroFallbackMotion =

  let private clamp01(value: float) : float =
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
