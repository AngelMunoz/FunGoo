namespace FunGoo.Metro

open System.Numerics
open System.Reflection
open Goo

/// Parameters driving the Metro 3D perspective ray-plane homography shader.
[<Struct>]
type MetroPerspectiveParams = {
  /// Rotation around the horizontal X axis in degrees.
  AngleXDeg: float32
  /// Rotation around the vertical Y axis in degrees.
  AngleYDeg: float32
  /// Camera perspective depth normalized to quad dimensions (must be positive).
  PerspectiveNormalized: float32
  /// Normalized horizontal rotation pivot in <c>[0, 1]</c>.
  PivotX: float32
  /// Normalized vertical rotation pivot in <c>[0, 1]</c>.
  PivotY: float32
  /// Premultiplied output opacity in <c>[0, 1]</c>.
  Opacity: float32
  /// Subtle angular dimming in <c>[0, 1]</c> as the surface turns away.
  BackfaceDim: float32
  /// Smooth UV edge feather width in <c>[0, 0.5]</c> (<c>0</c> disables it).
  EdgeFeather: float32
}

/// Presets, parameter packing, and optional compiled shader loading for Metro
/// 3D perspective transitions.
[<RequireQualifiedAccess>]
module MetroPerspective =

  /// Embedded manifest resource name for the compiled Metro perspective effect
  /// (<c>"FunGoo.Metro.Shaders.metro_perspective.goo-effect"</c>).
  val resourceName: string

  /// Neutral identity perspective parameters (zero rotation, full opacity).
  val identity: MetroPerspectiveParams

  /// Parameters for a Metro turnstile forward entrance (<c>AngleY</c> from
  /// <c>10°</c> to <c>0°</c>, opacity <c>0</c> to <c>1</c>).
  val turnstileIn: progress: float -> MetroPerspectiveParams

  /// Parameters for a Metro turnstile exit (<c>AngleY</c> from <c>0°</c> to
  /// <c>-15°</c>, opacity <c>1</c> to <c>0</c>).
  val turnstileOut: progress: float -> MetroPerspectiveParams

  /// Parameters for a Metro dialog swivel (<c>AngleX</c> between <c>90°</c> and
  /// <c>0°</c>, opacity between <c>0</c> and <c>1</c>).
  val dialogSwivel: progress: float * entering: bool -> MetroPerspectiveParams

  /// Parameters for a horizontal flip tile rotated by
  /// <paramref name="angleYDeg"/> degrees around its center.
  val flipTile: angleYDeg: float -> MetroPerspectiveParams

  /// Parameters for tactile Metro tile press tilt given centered normalized
  /// coordinates in <c>[-1, 1]</c> and maximum tilt in degrees.
  val pressTilt:
    normalizedX: float * normalizedY: float * maxDegrees: float ->
      MetroPerspectiveParams

  /// Packs <paramref name="p"/> into shader parameter slots 0 and 1, converting
  /// angles from degrees to radians and clamping all fields to finite ranges.
  val toParameterVectors:
    p: MetroPerspectiveParams -> struct (Vector4 * Vector4)

  /// Attempts to load the compiled Metro perspective shader program from the
  /// embedded resources of <paramref name="assembly"/> without throwing when
  /// the resource is absent.
  val tryLoadProgramFromAssembly:
    assembly: Assembly -> ShaderEffectProgram voption

  /// Attempts to load a compiled Metro perspective shader program from
  /// <paramref name="path"/>, returning <c>ValueNone</c> when missing.
  val tryLoadProgramFromFile: path: string -> ShaderEffectProgram voption

  /// Creates a <see cref="ShaderEffect"/> instance when
  /// <paramref name="program"/> is <c>ValueSome</c>.
  val tryCreate: program: ShaderEffectProgram voption -> ShaderEffect voption

  /// Creates a <see cref="ShaderEffect"/> from the assembly-embedded Metro
  /// perspective program when available, or returns <c>ValueNone</c>.
  val tryCreateDefault: unit -> ShaderEffect voption

  /// <c>true</c> when the compiled Metro perspective shader is embedded in
  /// <c>FunGoo.Metro</c>; otherwise <c>false</c>.
  val isEmbeddedShaderAvailable: bool

  /// Writes parameter slots 0 and 1 of <paramref name="effect"/> from
  /// <paramref name="p"/>.
  val applyParams: effect: ShaderEffect * p: MetroPerspectiveParams -> unit
