namespace FunGoo.Metro

open System
open Goo

/// Rendering path selected automatically by a Metro tilt or perspective
/// control.
[<Struct>]
type MetroTiltMode =
  /// Uses the compiled Slang 3D perspective shader.
  | Perspective3D
  /// Uses paint-bound 2D scale and directional translate animations.
  | Fallback2D

/// Data-only configuration for Metro tactile press tilt.
[<Struct; NoComparison>]
type MetroTiltConfig = {
  /// Maximum 3D tilt angle in degrees along each axis (defaults to <c>5.0</c>).
  MaxDegrees: float
  /// Target uniform scale during press in 3D mode (defaults to <c>0.985</c>).
  PressScale3D: float
  /// Target uniform scale during press in 2D fallback mode (defaults to
  /// <c>0.95</c>).
  FallbackScale: float
  /// Maximum directional shift in logical pixels in 2D fallback mode (defaults
  /// to <c>6.0</c>).
  FallbackOffsetPx: float
  /// Disables pointer tilt when <c>true</c>.
  Disabled: bool
}

/// Data-only input snapshot for <see cref="MetroTiltCell"/>.
[<Struct; NoComparison>]
type MetroTiltInput = {
  /// Child element wrapped by the tilt container.
  Child: Blob
  /// Tilt behavior configuration.
  Config: MetroTiltConfig
}

/// Snapshot of live press-tilt state and animation handles, exposed for
/// headless tests.
[<Struct; NoComparison>]
type MetroTiltState = {
  /// <c>true</c> while a pointer press is active on the control.
  IsPressed: bool
  /// Active rendering mode (<see cref="MetroTiltMode.Perspective3D"/> when the
  /// perspective shader is available; otherwise
  /// <see cref="MetroTiltMode.Fallback2D"/>).
  Mode: MetroTiltMode
  /// <c>true</c> while a <see cref="ShaderEffect"/> is currently attached to
  /// the element.
  IsShaderAttached: bool
  /// Centered normalized press X coordinate in <c>[-1.0, 1.0]</c>.
  NormalizedX: float
  /// Centered normalized press Y coordinate in <c>[-1.0, 1.0]</c>.
  NormalizedY: float
  /// Paint-bound uniform scale animation.
  ScaleAnim: Anim<float>
  /// Paint-bound directional translation animation used by the 2D fallback.
  TranslateAnim: Anim<Point>
  /// Paint-bound normalized tilt vector animation driving the 3D shader.
  TiltAnim: Anim<Point>
}

/// Stateful wrapper cell that applies Metro tactile press-tilt feedback to a
/// child element, using the embedded 3D perspective shader when available and
/// falling back to 2D scale and directional nudge automatically.
[<AbstractClass>]
type MetroTiltCell =
  inherit Cell<MetroTiltInput>

  /// Current tilt state and animation handles. A test reads it.
  abstract State: MetroTiltState

  /// The tree of the current input. A test reads it, and only inside a build,
  /// where the Metro theme is readable.
  abstract View: Blob

/// Internal reusable press-tilt state engine shared by
/// <see cref="MetroTiltCell"/> and <see cref="MetroLiveTileCell"/>.
type internal TiltEngine = {
  Handle: ElementHandle
  Effect: ShaderEffect voption
  mutable Config: MetroTiltConfig
  mutable IsPressed: bool
  mutable ShaderAttached: bool
  mutable NormalizedX: float
  mutable NormalizedY: float
  mutable ReducedMotion: bool
  mutable Scale: Anim<float>
  mutable Translate: Anim<Point>
  mutable Tilt: Anim<Point>
  PointerDown: Action<PointerEvent>
  PointerUp: Action<PointerEvent>
  PointerLeave: Action<PointerEvent>
  PointerCancel: Action<PointerEvent>
}

/// Internal helpers for attaching and driving a <see cref="TiltEngine"/> inside
/// a cell object expression.
module internal TiltEngine =

  /// Creates an unattached <see cref="TiltEngine"/> using
  /// <paramref name="effect"/>.
  val create: effect: ShaderEffect voption -> TiltEngine

  /// Attaches the animations of <paramref name="engine"/> to
  /// <paramref name="cell"/> after the cell object expression exists.
  val attach: engine: TiltEngine * cell: Cell -> unit

  /// Updates the configuration and reduced-motion flag of
  /// <paramref name="engine"/> during <c>Build</c>, snapping to rest when
  /// disabled or under reduced motion.
  val sync:
    engine: TiltEngine * config: MetroTiltConfig * reducedMotion: bool -> unit

  /// Snaps all animations of <paramref name="engine"/> to rest and clears
  /// press state.
  val snapToRest: engine: TiltEngine -> unit

  /// Applies the handle, pointer callbacks, paint-bound animations, and active
  /// 3D shader of <paramref name="engine"/> to <paramref name="blob"/>.
  val applyToBlob: engine: TiltEngine * blob: Blob -> unit

  /// Returns a <see cref="MetroTiltState"/> snapshot of
  /// <paramref name="engine"/>.
  val snapshot: engine: TiltEngine -> MetroTiltState

/// Tactile Metro press-tilt math, animation presets, and wrapper cell
/// constructors.
[<RequireQualifiedAccess>]
module MetroTilt =

  /// Default tilt configuration matching Metrino (<c>5.0°</c> max tilt,
  /// <c>0.985</c> 3D depress scale, <c>0.95</c> 2D fallback scale, <c>6.0</c>
  /// px 2D directional nudge).
  val defaultConfig: MetroTiltConfig

  /// Pre-cached 100 ms Metro cubic-bezier simulation factory for press-down
  /// tilt.
  val pressSpec: Func<float, float, float, Simulation>

  /// Pre-cached 167 ms Metro cubic-bezier simulation factory for release
  /// recovery.
  val releaseSpec: Func<float, float, float, Simulation>

  /// Creates default <see cref="MetroTiltInput"/> wrapping
  /// <paramref name="child"/>.
  val defaults: child: Blob -> MetroTiltInput

  /// Computes centered normalized pointer coordinates in <c>[-1.0, 1.0]</c>
  /// from element-local <paramref name="position"/> and <paramref name="box"/>.
  val normalizedOffset:
    position: Point * box: ElementRect -> struct (float * float)

  /// Computes the 2D fallback translation offset in logical pixels for
  /// normalized press coordinates <paramref name="nx"/> and
  /// <paramref name="ny"/>.
  val fallbackOffset: nx: float * ny: float * maxOffsetPx: float -> Point

  /// Creates a <see cref="MetroTiltCell"/> that selects 3D perspective when
  /// the compiled shader is embedded, or 2D fallback automatically.
  val create: unit -> MetroTiltCell

  /// Creates a <see cref="MetroTiltCell"/> with an explicit optional
  /// <see cref="ShaderEffect"/>. Tests use this to exercise both 3D and 2D
  /// paths.
  val createWithEffect: effect: ShaderEffect voption -> MetroTiltCell

  /// Mounts a <see cref="MetroTiltCell"/> under stable <paramref name="key"/>
  /// wrapping <paramref name="child"/> with <see cref="defaultConfig"/>.
  val mount: key: string * child: Blob -> Blob

  /// Mounts a <see cref="MetroTiltCell"/> under stable <paramref name="key"/>
  /// with explicit <paramref name="input"/>.
  val mountInput: key: string * input: MetroTiltInput -> Blob

  /// Mounts a <see cref="MetroTiltCell"/> created by <paramref name="factory"/>
  /// under stable <paramref name="key"/>. A test passes a factory to keep the
  /// cell.
  val mountWith:
    key: string * input: MetroTiltInput * factory: (unit -> MetroTiltCell) ->
      Blob
