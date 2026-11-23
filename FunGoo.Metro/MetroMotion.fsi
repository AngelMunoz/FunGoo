namespace FunGoo.Metro

open System
open Goo

/// Closed-form cubic Bezier easing simulation with endpoints <c>(0, 0)</c> and
/// <c>(1, 1)</c> and interior control points <c>(x1, y1)</c> and
/// <c>(x2, y2)</c>.
[<Sealed>]
type CubicBezierSimulation =
  inherit Simulation

  /// Creates a cubic Bezier simulation from <paramref name="fromValue"/> to
  /// <paramref name="toValue"/> over <paramref name="durationSeconds"/>.
  new:
    durationSeconds: float *
    fromValue: float *
    toValue: float *
    x1: float *
    y1: float *
    x2: float *
    y2: float ->
      CubicBezierSimulation

  /// Evaluates the scalar position at <paramref name="elapsed"/> seconds.
  override Position: elapsed: float -> float

  /// Evaluates the analytical velocity in units per second at
  /// <paramref name="elapsed"/> seconds.
  override Velocity: elapsed: float -> float

  /// Returns <c>true</c> once <paramref name="elapsed"/> reaches or exceeds
  /// the configured duration.
  override Done: elapsed: float -> bool

/// Closed-form exponential ease-out simulation following
/// <c>(1 - exp(-E * t)) / (1 - exp(-E))</c>, falling back to linear motion when
/// <c>abs exponent &lt; 1e-6</c>.
[<Sealed>]
type ExponentialEaseSimulation =
  inherit Simulation

  /// Creates an exponential ease simulation from <paramref name="fromValue"/>
  /// to <paramref name="toValue"/> over <paramref name="durationSeconds"/>.
  new:
    durationSeconds: float * fromValue: float * toValue: float * exponent: float ->
      ExponentialEaseSimulation

  /// Evaluates the scalar position at <paramref name="elapsed"/> seconds.
  override Position: elapsed: float -> float

  /// Evaluates the analytical velocity in units per second at
  /// <paramref name="elapsed"/> seconds.
  override Velocity: elapsed: float -> float

  /// Returns <c>true</c> once <paramref name="elapsed"/> reaches or exceeds
  /// the configured duration.
  override Done: elapsed: float -> bool

/// Infinite periodic phase simulation wrapping elapsed time modulo
/// <paramref name="periodSeconds"/> into <c>[0.0, 1.0)</c>.
[<Sealed>]
type PeriodicSimulation =
  inherit Simulation

  /// Creates an infinite periodic simulation with the given strictly positive
  /// period in seconds.
  new: periodSeconds: float -> PeriodicSimulation

  /// Always returns <c>true</c>.
  override IsInfinite: bool

  /// Evaluates the wrapped phase in <c>[0.0, 1.0)</c> at
  /// <paramref name="elapsed"/> seconds.
  override Position: elapsed: float -> float

  /// Evaluates the constant phase velocity <c>1.0 / periodSeconds</c>.
  override Velocity: elapsed: float -> float

  /// Always returns <c>false</c>.
  override Done: elapsed: float -> bool

/// Metro easing simulation factories, pre-cached motion specs, stagger helpers,
/// and reduced-motion adaptation.
[<RequireQualifiedAccess>]
module MetroMotion =

  /// Creates a reusable cubic Bezier simulation factory for the given duration
  /// in milliseconds and control points.
  val cubicBezier:
    durationMs: float * x1: float * y1: float * x2: float * y2: float ->
      Func<float, float, float, Simulation>

  /// Creates a reusable exponential ease-out simulation factory for the given
  /// duration in milliseconds and exponent.
  val exponentialOut:
    durationMs: float * exponent: float -> Func<float, float, float, Simulation>

  /// Creates a Metro decelerate simulation factory using cubic Bezier
  /// <c>(0.1, 0.9, 0.2, 1.0)</c>.
  val decelerate: durationMs: float -> Func<float, float, float, Simulation>

  /// Creates a Metro page-glide simulation factory using cubic Bezier
  /// <c>(0.25, 0.46, 0.45, 0.94)</c>.
  val pageGlide: durationMs: float -> Func<float, float, float, Simulation>

  /// Pre-cached 167 ms decelerate simulation factory.
  val fast: Func<float, float, float, Simulation>

  /// Pre-cached 250 ms decelerate simulation factory.
  val normal: Func<float, float, float, Simulation>

  /// Pre-cached 333 ms decelerate simulation factory.
  val slow: Func<float, float, float, Simulation>

  /// Pre-cached 500 ms page-glide turnstile entrance simulation factory.
  val turnstileIn: Func<float, float, float, Simulation>

  /// Pre-cached 400 ms page-glide turnstile exit simulation factory.
  val turnstileOut: Func<float, float, float, Simulation>

  /// Pre-cached 150 ms exponential-out (<c>exponent = 6.0</c>) continuum
  /// simulation factory.
  val continuum: Func<float, float, float, Simulation>

  /// Computes the sub-linear entrance stagger delay in milliseconds for item
  /// <paramref name="index"/>, clamping negative indices to <c>0.0</c>.
  val staggerDelayMs: index: int -> float

  /// Wraps <paramref name="spec"/> with the stagger delay for item
  /// <paramref name="index"/> via <see cref="Motion.Delay"/>.
  val staggered:
    index: int * spec: Func<float, float, float, Simulation> ->
      Func<float, float, float, Simulation>

  /// Returns a cached zero-duration tween when
  /// <see cref="MetroTheme.ReducedMotion"/> is active; otherwise returns
  /// <paramref name="spec"/> unchanged.
  val forTheme:
    theme: MetroTheme * spec: Func<float, float, float, Simulation> ->
      Func<float, float, float, Simulation>

/// Pixel translation vector for a Metro Continuum shared-element transition.
[<Struct>]
type ContinuumDelta = {
  /// Horizontal offset in logical pixels.
  OffsetX: float
  /// Vertical offset in logical pixels.
  OffsetY: float
}

/// Metro Continuum shared-element flight helpers.
[<RequireQualifiedAccess>]
module Continuum =

  /// Canonical Continuum entrance offset (<c>130 px, -70 px</c>).
  val defaultEnterDelta: ContinuumDelta

  /// Canonical Continuum exit offset (<c>225 px, 73 px</c>).
  val defaultExitDelta: ContinuumDelta

  /// Computes the translation offset from <paramref name="destination"/> to
  /// <paramref name="origin"/> (<c>origin - destination</c>).
  val measureDelta:
    origin: ElementRect * destination: ElementRect -> ContinuumDelta

  /// Plays a Continuum entrance animation on <paramref name="translate"/> and
  /// <paramref name="opacity"/>, respecting
  /// <see cref="MetroTheme.ReducedMotion"/>.
  val playEnter:
    theme: MetroTheme *
    translate: Anim<Point> *
    opacity: Anim<float> *
    delta: ContinuumDelta ->
      unit

  /// Plays a Continuum exit animation on <paramref name="translate"/> and
  /// <paramref name="opacity"/>, respecting
  /// <see cref="MetroTheme.ReducedMotion"/>.
  val playExit:
    theme: MetroTheme *
    translate: Anim<Point> *
    opacity: Anim<float> *
    delta: ContinuumDelta ->
      unit

/// 2D transform and opacity fallback approximations for Metro 3D transitions
/// when the perspective shader effect is unavailable.
[<RequireQualifiedAccess>]
module MetroFallbackMotion =

  /// Computes a 2D horizontal scale, translation, and opacity approximation of
  /// a Metro turnstile transition at normalized <paramref name="progress"/>.
  val turnstile2D:
    progress: float * entering: bool -> struct (PanelTransform * float)

  /// Computes a 2D vertical scale, translation, and opacity approximation of a
  /// Metro swivel transition at normalized <paramref name="progress"/>.
  val swivel2D:
    progress: float * entering: bool -> struct (PanelTransform * float)
