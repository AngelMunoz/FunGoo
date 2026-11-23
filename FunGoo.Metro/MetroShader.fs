namespace FunGoo.Metro

open System
open System.IO
open System.Numerics
open System.Reflection
open Goo

[<Struct>]
type MetroPerspectiveParams = {
  AngleXDeg: float32
  AngleYDeg: float32
  PerspectiveNormalized: float32
  PivotX: float32
  PivotY: float32
  Opacity: float32
  BackfaceDim: float32
  EdgeFeather: float32
}

[<RequireQualifiedAccess>]
module MetroPerspective =

  let private degToRad = float32(Math.PI / 180.0)
  let private resourceAssetName = "FunGoo.Metro.Shaders.metro_perspective"
  let resourceName = resourceAssetName + ".goo-effect"

  let private clamp01(value: float) : float =
    if not(Double.IsFinite value) || value <= 0.0 then 0.0
    elif value >= 1.0 then 1.0
    else value

  let private clampRange (minVal: float) (maxVal: float) (value: float) =
    if not(Double.IsFinite value) then 0.0
    elif value <= minVal then minVal
    elif value >= maxVal then maxVal
    else value

  let private clampSingle
    (fallback: float32)
    (minVal: float32)
    (maxVal: float32)
    (value: float32)
    : float32 =
    if not(Single.IsFinite value) then fallback
    elif value <= minVal then minVal
    elif value >= maxVal then maxVal
    else value

  let identity: MetroPerspectiveParams = {
    AngleXDeg = 0.0f
    AngleYDeg = 0.0f
    PerspectiveNormalized = 2.0f
    PivotX = 0.5f
    PivotY = 0.5f
    Opacity = 1.0f
    BackfaceDim = 0.0f
    EdgeFeather = 0.0f
  }

  let turnstileIn(progress: float) : MetroPerspectiveParams =
    let p = clamp01 progress

    {
      AngleXDeg = 0.0f
      AngleYDeg = float32(10.0 * (1.0 - p))
      PerspectiveNormalized = 2.0f
      PivotX = 0.0f
      PivotY = 0.5f
      Opacity = float32 p
      BackfaceDim = 0.15f
      EdgeFeather = if p >= 1.0 then 0.0f else 0.002f
    }

  let turnstileOut(progress: float) : MetroPerspectiveParams =
    let p = clamp01 progress

    {
      AngleXDeg = 0.0f
      AngleYDeg = float32(-15.0 * p)
      PerspectiveNormalized = 2.0f
      PivotX = 0.0f
      PivotY = 0.5f
      Opacity = float32(1.0 - p)
      BackfaceDim = 0.15f
      EdgeFeather = if p <= 0.0 then 0.0f else 0.002f
    }

  let dialogSwivel(progress: float, entering: bool) : MetroPerspectiveParams =
    let p = clamp01 progress

    if entering then
      {
        AngleXDeg = float32(90.0 * (1.0 - p))
        AngleYDeg = 0.0f
        PerspectiveNormalized = 2.0f
        PivotX = 0.5f
        PivotY = 0.5f
        Opacity = float32 p
        BackfaceDim = 0.2f
        EdgeFeather = if p >= 1.0 then 0.0f else 0.002f
      }
    else
      {
        AngleXDeg = float32(90.0 * p)
        AngleYDeg = 0.0f
        PerspectiveNormalized = 2.0f
        PivotX = 0.5f
        PivotY = 0.5f
        Opacity = float32(1.0 - p)
        BackfaceDim = 0.2f
        EdgeFeather = if p <= 0.0 then 0.0f else 0.002f
      }

  let flipTile(angleYDeg: float) : MetroPerspectiveParams =
    let angle = if Double.IsFinite angleYDeg then angleYDeg else 0.0

    {
      AngleXDeg = 0.0f
      AngleYDeg = float32 angle
      PerspectiveNormalized = 2.0f
      PivotX = 0.5f
      PivotY = 0.5f
      Opacity = 1.0f
      BackfaceDim = 0.25f
      EdgeFeather = if abs angle < 1e-4 then 0.0f else 0.002f
    }

  let pressTilt
    (normalizedX: float, normalizedY: float, maxDegrees: float)
    : MetroPerspectiveParams =
    let nx = clampRange -1.0 1.0 normalizedX
    let ny = clampRange -1.0 1.0 normalizedY

    let maxDeg =
      if Double.IsFinite maxDegrees && maxDegrees > 0.0 then
        maxDegrees
      else
        0.0

    let angleX = -ny * maxDeg
    let angleY = nx * maxDeg
    let flat = abs angleX < 1e-4 && abs angleY < 1e-4

    {
      AngleXDeg = float32 angleX
      AngleYDeg = float32 angleY
      PerspectiveNormalized = 2.0f
      PivotX = 0.5f
      PivotY = 0.5f
      Opacity = 1.0f
      BackfaceDim = 0.1f
      EdgeFeather = if flat then 0.0f else 0.002f
    }

  let toParameterVectors
    (p: MetroPerspectiveParams)
    : struct (Vector4 * Vector4) =
    let angleXRad = clampSingle 0.0f -360.0f 360.0f p.AngleXDeg * degToRad

    let angleYRad = clampSingle 0.0f -360.0f 360.0f p.AngleYDeg * degToRad

    let depth = clampSingle 2.0f 0.25f 100.0f p.PerspectiveNormalized
    let pivotX = clampSingle 0.5f 0.0f 1.0f p.PivotX
    let pivotY = clampSingle 0.5f 0.0f 1.0f p.PivotY
    let opacity = clampSingle 1.0f 0.0f 1.0f p.Opacity
    let backfaceDim = clampSingle 0.0f 0.0f 1.0f p.BackfaceDim
    let edgeFeather = clampSingle 0.0f 0.0f 0.5f p.EdgeFeather

    struct (Vector4(angleXRad, angleYRad, depth, pivotX),
            Vector4(pivotY, opacity, backfaceDim, edgeFeather))

  let tryLoadProgramFromAssembly
    (assembly: Assembly)
    : ShaderEffectProgram voption =
    if isNull assembly then
      ValueNone
    else
      use stream = assembly.GetManifestResourceStream(resourceName)

      if isNull stream then
        ValueNone
      else
        try
          ValueSome(ShaderEffectProgram.Load(assembly, resourceAssetName))
        with :? FileNotFoundException ->
          ValueNone

  let tryLoadProgramFromFile(path: string) : ShaderEffectProgram voption =
    if String.IsNullOrWhiteSpace path || not(File.Exists path) then
      ValueNone
    else
      try
        ValueSome(ShaderEffectProgram.Load path)
      with
      | :? FileNotFoundException
      | :? DirectoryNotFoundException -> ValueNone

  let tryCreate(program: ShaderEffectProgram voption) : ShaderEffect voption =
    program |> ValueOption.map(fun p -> ShaderEffect(p))

  let private embeddedProgram: ShaderEffectProgram voption =
    tryLoadProgramFromAssembly typeof<MetroPerspectiveParams>.Assembly

  let isEmbeddedShaderAvailable: bool = embeddedProgram.IsSome

  let tryCreateDefault() : ShaderEffect voption = tryCreate embeddedProgram

  let applyParams(effect: ShaderEffect, p: MetroPerspectiveParams) : unit =
    if isNull effect then
      raise(ArgumentNullException(nameof effect))

    let struct (v0, v1) = toParameterVectors p
    effect.SetParameter(0, v0) |> ignore
    effect.SetParameter(1, v1) |> ignore
