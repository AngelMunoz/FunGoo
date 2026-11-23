namespace FunGoo.Metro

open System
open Goo

/// The arguments of a cubic Bezier simulation.
[<NoComparison; NoEquality>]
type CubicBezierArgs = {
  /// Duration of the simulation in seconds.
  DurationSeconds: float
  /// The position at the start.
  FromValue: float
  /// The position at the end.
  ToValue: float
  /// The x of the first interior control point, from <c>0.0</c> to <c>1.0</c>.
  X1: float
  /// The y of the first interior control point.
  Y1: float
  /// The x of the second interior control point, from <c>0.0</c> to <c>1.0</c>.
  X2: float
  /// The y of the second interior control point.
  Y2: float
}

/// Closed-form cubic Bezier easing simulation with endpoints <c>(0, 0)</c> and
/// <c>(1, 1)</c> and interior control points <c>(x1, y1)</c> and
/// <c>(x2, y2)</c>.
[<RequireQualifiedAccess>]
module CubicBezierSimulation =

  /// Creates a cubic Bezier simulation from <c>FromValue</c> to <c>ToValue</c>
  /// over <c>DurationSeconds</c>. <c>Position</c> evaluates the scalar position
  /// in seconds, <c>Velocity</c> the analytical velocity in units per second,
  /// and <c>Done</c> is <c>true</c> once the elapsed seconds reach the
  /// duration. An argument that is not finite, a negative duration, or an x
  /// outside <c>0.0 .. 1.0</c> throws <c>ArgumentOutOfRangeException</c>.
  val create: args: CubicBezierArgs -> Simulation

/// The arguments of an exponential ease-out simulation.
[<NoComparison; NoEquality>]
type ExponentialEaseArgs = {
  /// Duration of the simulation in seconds.
  DurationSeconds: float
  /// The position at the start.
  FromValue: float
  /// The position at the end.
  ToValue: float
  /// The exponent of the ease.
  Exponent: float
}

/// Closed-form exponential ease-out simulation following
/// <c>(1 - exp(-E * t)) / (1 - exp(-E))</c>, falling back to linear motion when
/// <c>abs Exponent &lt; 1e-6</c>.
[<RequireQualifiedAccess>]
module ExponentialEaseSimulation =

  /// Creates an exponential ease simulation from <c>FromValue</c> to
  /// <c>ToValue</c> over <c>DurationSeconds</c>. <c>Position</c> evaluates the
  /// scalar position, <c>Velocity</c> the analytical velocity in units per
  /// second, and <c>Done</c> is <c>true</c> once the elapsed seconds reach the
  /// duration. An argument that is not finite or a negative duration throws
  /// <c>ArgumentOutOfRangeException</c>.
  val create: args: ExponentialEaseArgs -> Simulation

/// Infinite periodic phase simulation that wraps the elapsed time modulo its
/// period into <c>[0.0, 1.0)</c>. <c>IsInfinite</c> is always <c>true</c> and
/// <c>Done</c> is always <c>false</c>.
[<RequireQualifiedAccess>]
module PeriodicSimulation =

  /// Creates an infinite periodic simulation with the given strictly positive
  /// period in seconds. Another period throws <c>ArgumentOutOfRangeException</c>.
  val create: periodSeconds: float -> Simulation

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
