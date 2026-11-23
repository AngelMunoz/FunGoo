module FunGoo.Metro.Tests.MotionAndShaderTests

open System
open System.IO
open System.Numerics
open System.Reflection
open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost

let private sampleCubic(p1: float, p2: float, u: float) : float =
  let omt = 1.0 - u
  3.0 * omt * omt * u * p1 + 3.0 * omt * u * u * p2 + u * u * u

let private makeTheme(reducedMotion: bool) : MetroTheme = {
  IsDark = true
  Palette = MetroPalette.dark
  Accent = MetroAccent.resolve(MetroAccentKind.Blue, ValueNone)
  FontFamily = ""
  MonoFontFamily = "monospace"
  TileUnit = 150.0
  ReducedMotion = reducedMotion
  HighContrast = false
  TextScaleFactor = 1.0
}

let private writeMinimalEffectBytes() : byte array =
  use ms = new MemoryStream()
  use bw = new BinaryWriter(ms)
  // Header: Magic ("GEFF"), Schema 2, 1 artifact.
  bw.Write(0x46464547u)
  bw.Write(2u)
  bw.Write(1u)
  // Artifact record: Vulkan SPIR-V kind, 20-byte header payload.
  bw.Write(0x56505356u)
  bw.Write(20u)
  // Minimal valid SPIR-V 1.5 header (magic, version, generator, bound, schema).
  bw.Write(0x07230203u)
  bw.Write(0x00010500u)
  bw.Write(0u)
  bw.Write(1u)
  bw.Write(0u)
  bw.Flush()
  ms.ToArray()

/// A cell with two animations that rebuild it: a translation and an opacity.
/// The tests play the Continuum flight on the two values.
/// <c>ContinuumProbeCell.create</c> makes the cell.
[<AbstractClass>]
type private ContinuumProbeCell() =
  inherit Cell()

  /// The translation animation of the cell.
  abstract Translate: Anim<Point>

  /// The opacity animation of the cell.
  abstract Opacity: Anim<float>

[<RequireQualifiedAccess>]
module private ContinuumProbeCell =

  /// Builds a container at the translation and the opacity of the two values.
  let buildTree(translate: Anim<Point>, opacity: Anim<float>) : Blob =
    let offset = translate.Value

    Container(
      Opacity = opacity.Value,
      Transform =
        PanelTransform(
          TranslateX = Length.Pixels offset.X,
          TranslateY = Length.Pixels offset.Y
        )
    )

  let create() : ContinuumProbeCell =
    // Slots for the two animation values. The cell exists after the object
    // expression, and Goo ties each animation to the cell.
    let mutable translate = Unchecked.defaultof<Anim<Point>>
    let mutable opacity = Unchecked.defaultof<Anim<float>>

    let cell = {
      new ContinuumProbeCell() with
        override _.Translate = translate
        override _.Opacity = opacity
        override _.Build() : Blob = buildTree(translate, opacity)
    }

    translate <- cell.Animate(Point(X = 0.0, Y = 0.0))
    opacity <- cell.Animate(1.0)
    cell

[<Fact>]
let ``CubicBezierSimulation validates inputs and solves Bx(u)=t within 1e-7``
  ()
  =
  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    CubicBezierSimulation(-0.1, 0.0, 1.0, 0.1, 0.9, 0.2, 1.0) |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    CubicBezierSimulation(Double.NaN, 0.0, 1.0, 0.1, 0.9, 0.2, 1.0) |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    CubicBezierSimulation(0.25, 0.0, 1.0, -0.01, 0.9, 0.2, 1.0) |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    CubicBezierSimulation(0.25, 0.0, 1.0, 0.1, 0.9, 1.01, 1.0) |> ignore)
  |> ignore

  let curves = [|
    0.0, 0.0, 1.0, 1.0
    0.1, 0.9, 0.2, 1.0
    0.25, 0.46, 0.45, 0.94
    0.0, 0.0, 0.0, 1.0
    1.0, 0.0, 1.0, 1.0
    0.2, 1.4, 0.4, 1.2
  |]

  for x1, y1, x2, y2 in curves do
    let duration = 0.5
    let fromVal = 10.0
    let toVal = 110.0

    let sim = CubicBezierSimulation(duration, fromVal, toVal, x1, y1, x2, y2)

    Assert.Equal(fromVal, sim.Position(-0.1))
    Assert.Equal(fromVal, sim.Position(0.0))
    Assert.Equal(toVal, sim.Position(duration))
    Assert.Equal(toVal, sim.Position(duration + 0.5))
    Assert.False(sim.Done(0.0))
    Assert.False(sim.Done(duration - 1e-4))
    Assert.True(sim.Done(duration))
    Assert.True(sim.Done(duration + 0.1))
    Assert.Equal(0.0, sim.Velocity(-0.01))
    Assert.Equal(0.0, sim.Velocity(duration))
    Assert.Equal(0.0, sim.Velocity(duration + 0.1))

    for u in [| 0.05; 0.2; 0.5; 0.75; 0.95 |] do
      let t = sampleCubic(x1, x2, u)
      let expectedY = sampleCubic(y1, y2, u)
      let expectedPos = fromVal + (toVal - fromVal) * expectedY
      let actualPos = sim.Position(t * duration)
      Assert.True(abs(actualPos - expectedPos) < 1e-7)

  // Verify analytical velocity matches central difference on decelerate curve.
  let decel = CubicBezierSimulation(0.25, 0.0, 100.0, 0.1, 0.9, 0.2, 1.0)
  let h = 1e-6

  for elapsed in [| 0.02; 0.06; 0.125; 0.2 |] do
    let numeric =
      (decel.Position(elapsed + h) - decel.Position(elapsed - h)) / (2.0 * h)

    let analytic = decel.Velocity(elapsed)
    Assert.True(abs(analytic - numeric) < 1e-3)

  // Verify Velocity(0.0) on linear (0,0,1,1) handles 0/0 via finite diff.
  let linear = CubicBezierSimulation(0.5, 10.0, 110.0, 0.0, 0.0, 1.0, 1.0)
  Assert.True(abs(linear.Velocity(0.0) - 200.0) < 1e-3)

  // Zero-duration settles immediately with zero velocity.
  let zero = CubicBezierSimulation(0.0, 5.0, 20.0, 0.1, 0.9, 0.2, 1.0)
  Assert.Equal(5.0, zero.Position(-0.001))
  Assert.Equal(20.0, zero.Position(0.0))
  Assert.Equal(0.0, zero.Velocity(0.0))
  Assert.True(zero.Done(0.0))

[<Fact>]
let ``ExponentialEaseSimulation evaluates curve linear fallback and velocity``
  ()
  =
  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    ExponentialEaseSimulation(-0.1, 0.0, 1.0, 6.0) |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    ExponentialEaseSimulation(0.15, 0.0, 1.0, Double.PositiveInfinity)
    |> ignore)
  |> ignore

  let duration = 0.15
  let e = 6.0
  let fromVal = 130.0
  let toVal = 0.0
  let sim = ExponentialEaseSimulation(duration, fromVal, toVal, e)

  Assert.Equal(fromVal, sim.Position(-0.05))
  Assert.Equal(fromVal, sim.Position(0.0))
  Assert.Equal(toVal, sim.Position(duration))
  Assert.Equal(0.0, sim.Velocity(-0.01))
  Assert.Equal(0.0, sim.Velocity(duration))
  Assert.False(sim.Done(0.1))
  Assert.True(sim.Done(duration))

  let denom = 1.0 - Math.Exp(-e)
  let h = 1e-6

  for frac in [| 0.1; 0.25; 0.5; 0.75; 0.9 |] do
    let elapsed = frac * duration

    let expected =
      fromVal + (toVal - fromVal) * (1.0 - Math.Exp(-e * frac)) / denom

    Assert.Equal(expected, sim.Position(elapsed), 10)

    let expectedVel =
      (toVal - fromVal) * e * Math.Exp(-e * frac) / (denom * duration)

    Assert.Equal(expectedVel, sim.Velocity(elapsed), 8)

    let numericVel =
      (sim.Position(elapsed + h) - sim.Position(elapsed - h)) / (2.0 * h)

    Assert.True(abs(sim.Velocity(elapsed) - numericVel) < 1e-2)

  // Near-zero exponent falls back cleanly to linear motion.
  let linear = ExponentialEaseSimulation(0.4, 20.0, 60.0, 1e-8)
  Assert.Equal(40.0, linear.Position(0.2), 10)
  Assert.Equal(100.0, linear.Velocity(0.2), 10)

[<Fact>]
let ``PeriodicSimulation wraps phase into 0..1 with constant velocity``() =
  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    PeriodicSimulation(0.0) |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    PeriodicSimulation(-1.0) |> ignore)
  |> ignore

  Assert.Throws<ArgumentOutOfRangeException>(fun () ->
    PeriodicSimulation(Double.Epsilon) |> ignore)
  |> ignore

  let sim = PeriodicSimulation(2.0)
  Assert.True(sim.IsInfinite)
  Assert.False(sim.Done(0.0))
  Assert.False(sim.Done(10000.0))
  Assert.Equal(0.5, sim.Velocity(0.0), 12)
  Assert.Equal(0.5, sim.Velocity(9.5), 12)

  Assert.Equal(0.0, sim.Position(0.0), 12)
  Assert.Equal(0.25, sim.Position(0.5), 12)
  Assert.Equal(0.5, sim.Position(1.0), 12)
  Assert.Equal(0.0, sim.Position(2.0), 12)
  Assert.Equal(0.75, sim.Position(5.5), 12)
  Assert.Equal(0.75, sim.Position(-0.5), 12)

  for sample in [| -10.0; -1e-17; -0.001; 0.0; 1.999999; 2.0; 100.0 |] do
    let p = sim.Position(sample)
    Assert.True(p >= 0.0 && p < 1.0)

[<Fact>]
let ``MetroMotion presets stagger delays and ReducedMotion work correctly``() =
  // Pre-cached specs expose expected durations.
  let checkDuration
    (spec: Func<float, float, float, Simulation>, expectedMs: float)
    =
    let sim = spec.Invoke(0.0, 1.0, 0.0)
    let expectedSec = expectedMs / 1000.0
    Assert.False(sim.Done(expectedSec - 0.001))
    Assert.True(sim.Done(expectedSec))

  checkDuration(MetroMotion.fast, 167.0)
  checkDuration(MetroMotion.normal, 250.0)
  checkDuration(MetroMotion.slow, 333.0)
  checkDuration(MetroMotion.turnstileIn, 500.0)
  checkDuration(MetroMotion.turnstileOut, 400.0)
  checkDuration(MetroMotion.continuum, 150.0)

  // Stagger delays clamp negative indices, match 0..9, and extend smoothly.
  Assert.Equal(0.0, MetroMotion.staggerDelayMs -5)
  Assert.Equal(0.0, MetroMotion.staggerDelayMs -1)

  for i = 0 to MetroTiming.staggerDelaysMs.Count - 1 do
    Assert.Equal(MetroTiming.staggerDelaysMs[i], MetroMotion.staggerDelayMs i)

  let d9 = MetroMotion.staggerDelayMs 9
  let d10 = MetroMotion.staggerDelayMs 10
  let d11 = MetroMotion.staggerDelayMs 11
  let d20 = MetroMotion.staggerDelayMs 20
  Assert.True(d10 > d9 && d11 > d10 && d20 > d11)
  Assert.True((d10 - d9) < (d9 - MetroMotion.staggerDelayMs 8))
  Assert.True((d11 - d10) < (d10 - d9))

  // Staggered wraps with Motion.Delay and holds start value during delay.
  Assert.Same(MetroMotion.fast, MetroMotion.staggered(0, MetroMotion.fast))
  let delayedSpec = MetroMotion.staggered(2, MetroMotion.fast)
  let delayedSim = delayedSpec.Invoke(10.0, 50.0, 0.0)
  Assert.Equal(10.0, delayedSim.Position(0.05))
  Assert.Equal(0.0, delayedSim.Velocity(0.05))
  Assert.False(delayedSim.Done(0.09))
  Assert.True(delayedSim.Position(0.15) > 10.0)
  Assert.True(delayedSim.Done(0.09 + 0.167))

  // ReducedMotion returns a cached zero-duration tween.
  let normalTheme = makeTheme false
  let reducedTheme = makeTheme true

  Assert.Same(
    MetroMotion.normal,
    MetroMotion.forTheme(normalTheme, MetroMotion.normal)
  )

  let reduced1 = MetroMotion.forTheme(reducedTheme, MetroMotion.normal)
  let reduced2 = MetroMotion.forTheme(reducedTheme, MetroMotion.slow)
  Assert.Same(reduced1, reduced2)

  let instantSim = reduced1.Invoke(0.0, 42.0, 0.0)
  Assert.True(instantSim.Done(0.0))
  Assert.Equal(42.0, instantSim.Position(0.0))

let private updateTreeMethod =
  typeof<Window>
    .GetMethod(
      "UpdateTree",
      BindingFlags.Instance ||| BindingFlags.NonPublic,
      null,
      [| typeof<float> |],
      null
    )

let private pumpMotionFrames(window: Window, count: int, dt: float) : unit =
  let args: obj array = [| box dt |]

  for _ = 1 to count do
    updateTreeMethod.Invoke(window, args) |> ignore

[<Fact>]
let ``Continuum measures deltas and ticks Anim in EmbeddedWindowHost``() =
  Assert.Equal(130.0, Continuum.defaultEnterDelta.OffsetX)
  Assert.Equal(-70.0, Continuum.defaultEnterDelta.OffsetY)
  Assert.Equal(225.0, Continuum.defaultExitDelta.OffsetX)
  Assert.Equal(73.0, Continuum.defaultExitDelta.OffsetY)

  let origin = ElementRect(X = 180.0, Y = 50.0, Width = 120.0, Height = 32.0)
  let dest = ElementRect(X = 50.0, Y = 120.0, Width = 260.0, Height = 56.0)
  let measured = Continuum.measureDelta(origin, dest)
  Assert.Equal(130.0, measured.OffsetX)
  Assert.Equal(-70.0, measured.OffsetY)

  use host = createHost(640, 480)
  let probe = ContinuumProbeCell.create()
  let window = Window(Root = probe)
  Metro.attachDefault(window)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let normalTheme = makeTheme false
  Continuum.playEnter(normalTheme, probe.Translate, probe.Opacity, measured)
  Assert.True(probe.Translate.Running)
  Assert.True(probe.Opacity.Running)

  // Mid-flight (48 ms into 150 ms exponential ease).
  pumpMotionFrames(window, 3, 0.016)
  let midPoint = probe.Translate.Value
  let midOpacity = probe.Opacity.Value
  Assert.True(midPoint.X > 0.0 && midPoint.X < measured.OffsetX)
  Assert.True(midPoint.Y < 0.0 && midPoint.Y > measured.OffsetY)
  Assert.True(midOpacity > 0.5 && midOpacity < 1.0)

  // Settle past 150 ms.
  pumpMotionFrames(window, 12, 0.016)
  Assert.False(probe.Translate.Running)
  Assert.False(probe.Opacity.Running)
  Assert.Equal(0.0, probe.Translate.Value.X, 6)
  Assert.Equal(0.0, probe.Translate.Value.Y, 6)
  Assert.Equal(1.0, probe.Opacity.Value, 6)

  // Exit flight to defaultExitDelta.
  Continuum.playExit(
    normalTheme,
    probe.Translate,
    probe.Opacity,
    Continuum.defaultExitDelta
  )

  pumpMotionFrames(window, 3, 0.016)
  Assert.True(probe.Translate.Value.X > 0.0)
  Assert.True(probe.Opacity.Value < 1.0)

  pumpMotionFrames(window, 12, 0.016)
  Assert.False(probe.Translate.Running)
  Assert.False(probe.Opacity.Running)
  Assert.Equal(Continuum.defaultExitDelta.OffsetX, probe.Translate.Value.X, 6)
  Assert.Equal(Continuum.defaultExitDelta.OffsetY, probe.Translate.Value.Y, 6)
  Assert.Equal(0.0, probe.Opacity.Value, 6)

  // ReducedMotion lands immediately on the first tick.
  let reducedTheme = makeTheme true
  Continuum.playEnter(reducedTheme, probe.Translate, probe.Opacity, measured)
  pumpMotionFrames(window, 1, 0.016)
  Assert.False(probe.Translate.Running)
  Assert.False(probe.Opacity.Running)
  Assert.Equal(0.0, probe.Translate.Value.X, 6)
  Assert.Equal(0.0, probe.Translate.Value.Y, 6)
  Assert.Equal(1.0, probe.Opacity.Value, 6)

[<Fact>]
let ``MetroFallbackMotion turnstile2D and swivel2D hit identity and opacity``
  ()
  =
  let identity = PanelTransform()

  let struct (tEnterStart, oEnterStart) =
    MetroFallbackMotion.turnstile2D(0.0, true)

  Assert.Equal(0.0, oEnterStart, 6)
  Assert.True(tEnterStart.ScaleX < 1.0)
  Assert.True(tEnterStart.TranslateX.Magnitude > 0.0)

  let struct (tEnterEnd, oEnterEnd) = MetroFallbackMotion.turnstile2D(1.0, true)
  Assert.Equal(identity, tEnterEnd)
  Assert.Equal(1.0, oEnterEnd, 6)

  let struct (tExitStart, oExitStart) =
    MetroFallbackMotion.turnstile2D(0.0, false)

  Assert.Equal(identity, tExitStart)
  Assert.Equal(1.0, oExitStart, 6)

  let struct (tExitEnd, oExitEnd) = MetroFallbackMotion.turnstile2D(1.0, false)
  Assert.Equal(0.0, oExitEnd, 6)
  Assert.True(tExitEnd.ScaleX < 1.0)
  Assert.True(tExitEnd.TranslateX.Magnitude < 0.0)

  let struct (sEnterStart, soEnterStart) =
    MetroFallbackMotion.swivel2D(0.0, true)

  Assert.Equal(0.0, soEnterStart, 6)
  Assert.True(sEnterStart.ScaleY < 1.0 && sEnterStart.ScaleY > 0.0)
  Assert.True(sEnterStart.TranslateY.Magnitude < 0.0)

  let struct (sEnterEnd, soEnterEnd) = MetroFallbackMotion.swivel2D(1.0, true)
  Assert.Equal(identity, sEnterEnd)
  Assert.Equal(1.0, soEnterEnd, 6)

  let struct (sExitStart, soExitStart) =
    MetroFallbackMotion.swivel2D(0.0, false)

  Assert.Equal(identity, sExitStart)
  Assert.Equal(1.0, soExitStart, 6)

  let struct (sExitEnd, soExitEnd) = MetroFallbackMotion.swivel2D(1.0, false)
  Assert.Equal(0.0, soExitEnd, 6)
  Assert.True(sExitEnd.ScaleY < 1.0 && sExitEnd.ScaleY > 0.0)
  Assert.True(sExitEnd.TranslateY.Magnitude > 0.0)

  // Out-of-range and NaN inputs clamp safely.
  let struct (tNaN, oNaN) = MetroFallbackMotion.turnstile2D(Double.NaN, true)
  Assert.Equal(tEnterStart, tNaN)
  Assert.Equal(0.0, oNaN, 6)

  let struct (sOver, soOver) = MetroFallbackMotion.swivel2D(2.5, true)
  Assert.Equal(identity, sOver)
  Assert.Equal(1.0, soOver, 6)

[<Fact>]
let ``MetroPerspective packs params clamps ranges and falls back cleanly``() =
  Assert.Equal(
    "FunGoo.Metro.Shaders.metro_perspective.goo-effect",
    MetroPerspective.resourceName
  )

  let struct (v0Id, v1Id) =
    MetroPerspective.toParameterVectors MetroPerspective.identity

  Assert.Equal(Vector4(0.0f, 0.0f, 2.0f, 0.5f), v0Id)
  Assert.Equal(Vector4(0.5f, 1.0f, 0.0f, 0.0f), v1Id)

  let tiStart = MetroPerspective.turnstileIn 0.0
  let tiEnd = MetroPerspective.turnstileIn 1.0
  Assert.Equal(10.0f, tiStart.AngleYDeg, 4)
  Assert.Equal(0.0f, tiStart.Opacity, 4)
  Assert.Equal(0.0f, tiEnd.AngleYDeg, 4)
  Assert.Equal(1.0f, tiEnd.Opacity, 4)
  Assert.Equal(0.0f, tiEnd.EdgeFeather, 4)

  let toStart = MetroPerspective.turnstileOut 0.0
  let toEnd = MetroPerspective.turnstileOut 1.0
  Assert.Equal(0.0f, toStart.AngleYDeg, 4)
  Assert.Equal(1.0f, toStart.Opacity, 4)
  Assert.Equal(-15.0f, toEnd.AngleYDeg, 4)
  Assert.Equal(0.0f, toEnd.Opacity, 4)

  let swInStart = MetroPerspective.dialogSwivel(0.0, true)
  let swInEnd = MetroPerspective.dialogSwivel(1.0, true)
  let swOutEnd = MetroPerspective.dialogSwivel(1.0, false)
  Assert.Equal(90.0f, swInStart.AngleXDeg, 4)
  Assert.Equal(0.0f, swInStart.Opacity, 4)
  Assert.Equal(0.0f, swInEnd.AngleXDeg, 4)
  Assert.Equal(1.0f, swInEnd.Opacity, 4)
  Assert.Equal(90.0f, swOutEnd.AngleXDeg, 4)
  Assert.Equal(0.0f, swOutEnd.Opacity, 4)

  let struct (v0Sw, v1Sw) = MetroPerspective.toParameterVectors swInStart
  Assert.Equal(float32(Math.PI * 0.5), v0Sw.X, 5)
  Assert.Equal(0.0f, v0Sw.Y, 5)
  Assert.Equal(0.0f, v1Sw.Y, 5)

  let flipped = MetroPerspective.flipTile 180.0
  let struct (v0Flip, _) = MetroPerspective.toParameterVectors flipped
  Assert.Equal(float32 Math.PI, v0Flip.Y, 5)

  let flatTilt = MetroPerspective.pressTilt(0.0, 0.0, 8.0)
  Assert.Equal(0.0f, flatTilt.AngleXDeg, 4)
  Assert.Equal(0.0f, flatTilt.AngleYDeg, 4)
  Assert.Equal(0.0f, flatTilt.EdgeFeather, 4)

  let cornerTilt = MetroPerspective.pressTilt(2.0, -3.0, 6.0)
  Assert.Equal(6.0f, cornerTilt.AngleXDeg, 4)
  Assert.Equal(6.0f, cornerTilt.AngleYDeg, 4)

  // Non-finite and out-of-range values clamp to safe finite shader inputs.
  let wild: MetroPerspectiveParams = {
    AngleXDeg = Single.NaN
    AngleYDeg = 999.0f
    PerspectiveNormalized = -5.0f
    PivotX = -2.0f
    PivotY = 4.0f
    Opacity = 3.0f
    BackfaceDim = Single.PositiveInfinity
    EdgeFeather = 5.0f
  }

  let struct (v0Wild, v1Wild) = MetroPerspective.toParameterVectors wild
  Assert.Equal(0.0f, v0Wild.X, 5)
  Assert.Equal(float32(2.0 * Math.PI), v0Wild.Y, 5)
  Assert.Equal(0.25f, v0Wild.Z, 5)
  Assert.Equal(0.0f, v0Wild.W, 5)
  Assert.Equal(1.0f, v1Wild.X, 5)
  Assert.Equal(1.0f, v1Wild.Y, 5)
  Assert.Equal(0.0f, v1Wild.Z, 5)
  Assert.Equal(0.5f, v1Wild.W, 5)

  // Embedded .goo-effect resource loads when compiled, or returns ValueNone.
  if MetroPerspective.isEmbeddedShaderAvailable then
    let embedded = MetroPerspective.tryCreateDefault()
    Assert.True(embedded.IsSome)

    embedded
    |> ValueOption.iter(fun effect ->
      MetroPerspective.applyParams(effect, tiStart))
  else
    Assert.True(MetroPerspective.tryCreateDefault().IsNone)

  Assert.True((MetroPerspective.tryCreate ValueNone).IsNone)

  Assert.True(
    (MetroPerspective.tryLoadProgramFromAssembly typeof<int>.Assembly).IsNone
  )

  Assert.True((MetroPerspective.tryLoadProgramFromAssembly null).IsNone)

  Assert.True(
    (MetroPerspective.tryLoadProgramFromFile
      "/nonexistent/metro_perspective.goo-effect")
      .IsNone
  )

  Assert.True((MetroPerspective.tryLoadProgramFromFile "").IsNone)

  // Valid .goo-effect file loads, wraps into ShaderEffect, and accepts params.
  let tempPath =
    Path.Combine(
      Path.GetTempPath(),
      $"metro-perspective-test-{Guid.NewGuid():N}.goo-effect"
    )

  try
    File.WriteAllBytes(tempPath, writeMinimalEffectBytes())
    let loaded = MetroPerspective.tryLoadProgramFromFile tempPath
    Assert.True(loaded.IsSome)
    let created = MetroPerspective.tryCreate loaded
    Assert.True(created.IsSome)

    created
    |> ValueOption.iter(fun effect ->
      MetroPerspective.applyParams(effect, tiStart)
      MetroPerspective.applyParams(effect, MetroPerspective.identity))
  finally
    if File.Exists tempPath then
      File.Delete tempPath

[<Fact>]
let ``Hot-path perspective packing and 2D fallbacks allocate zero bytes``() =
  let program = ShaderEffectProgram(writeMinimalEffectBytes())
  let effect = ShaderEffect(program)
  let mutable acc = 0.0

  // Warm up JIT and static initializers before measuring allocations.
  for i = 0 to 15 do
    let progress = float i / 15.0
    let p = MetroPerspective.turnstileIn progress
    let struct (v0, v1) = MetroPerspective.toParameterVectors p
    MetroPerspective.applyParams(effect, p)
    let struct (t1, o1) = MetroFallbackMotion.turnstile2D(progress, true)
    let struct (t2, o2) = MetroFallbackMotion.swivel2D(progress, false)
    acc <- acc + float v0.Y + float v1.Y + t1.ScaleX + o1 + t2.ScaleY + o2

  let before = GC.GetAllocatedBytesForCurrentThread()

  for i = 0 to 9999 do
    let progress = float(i % 100) / 100.0
    let p = MetroPerspective.turnstileIn progress
    let struct (v0, v1) = MetroPerspective.toParameterVectors p
    MetroPerspective.applyParams(effect, p)
    let struct (t1, o1) = MetroFallbackMotion.turnstile2D(progress, true)
    let struct (t2, o2) = MetroFallbackMotion.swivel2D(progress, false)
    acc <- acc + float v0.Y + float v1.Y + t1.ScaleX + o1 + t2.ScaleY + o2

  let allocated = GC.GetAllocatedBytesForCurrentThread() - before
  Assert.True(acc > 0.0)
  Assert.Equal(0L, allocated)
