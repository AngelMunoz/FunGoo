namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct>]
type MetroTiltMode =
  | Perspective3D
  | Fallback2D

[<Struct; NoComparison>]
type MetroTiltConfig = {
  MaxDegrees: float
  PressScale3D: float
  FallbackScale: float
  FallbackOffsetPx: float
  Disabled: bool
}

[<Struct; NoComparison>]
type MetroTiltInput = { Child: Blob; Config: MetroTiltConfig }

[<Struct; NoComparison>]
type MetroTiltState = {
  IsPressed: bool
  Mode: MetroTiltMode
  IsShaderAttached: bool
  NormalizedX: float
  NormalizedY: float
  ScaleAnim: Anim<float>
  TranslateAnim: Anim<Point>
  TiltAnim: Anim<Point>
}

[<AbstractClass>]
type MetroTiltCell() =
  inherit Cell<MetroTiltInput>()

  abstract State: MetroTiltState

  abstract View: Blob

type TiltEngine = {
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

module TiltInternals =

  let zeroPoint = Point(X = 0.0, Y = 0.0)

  let defaultConfig: MetroTiltConfig = {
    MaxDegrees = 5.0
    PressScale3D = 0.985
    FallbackScale = 0.95
    FallbackOffsetPx = 6.0
    Disabled = false
  }

  let pressSpec = MetroMotion.decelerate 100.0
  let releaseSpec = MetroMotion.fast

  let clampUnit(value: float) : float =
    if not(Double.IsFinite value) then 0.0
    elif value < -1.0 then -1.0
    elif value > 1.0 then 1.0
    else value

  let normalizedOffset
    (position: Point, box: ElementRect)
    : struct (float * float) =
    if
      not(Double.IsFinite box.Width)
      || not(Double.IsFinite box.Height)
      || box.Width <= 0.0
      || box.Height <= 0.0
    then
      struct (0.0, 0.0)
    else
      let cx = box.Width * 0.5
      let cy = box.Height * 0.5
      let nx = clampUnit((position.X - cx) / cx)
      let ny = clampUnit((position.Y - cy) / cy)
      struct (nx, ny)

  let fallbackOffset(nx: float, ny: float, maxOffsetPx: float) : Point =
    let offset =
      if Double.IsFinite maxOffsetPx && maxOffsetPx > 0.0 then
        maxOffsetPx
      else
        0.0

    Point(X = clampUnit nx * offset, Y = clampUnit ny * offset)

  let resolveMaxDegrees(value: float) : float =
    if Double.IsFinite value && value > 0.0 then value else 0.0

  let resolveScale(value: float, fallback: float) : float =
    if Double.IsFinite value && value > 0.0 && value <= 1.0 then
      value
    else
      fallback

module TiltEngine =

  let snapToRest(engine: TiltEngine) : unit =
    engine.IsPressed <- false
    engine.ShaderAttached <- false
    engine.NormalizedX <- 0.0
    engine.NormalizedY <- 0.0

    if not(isNull engine.Scale) then
      engine.Scale.Snap(1.0)

    if not(isNull engine.Translate) then
      engine.Translate.Snap(TiltInternals.zeroPoint)

    if not(isNull engine.Tilt) then
      engine.Tilt.Snap(TiltInternals.zeroPoint)

    engine.Effect
    |> ValueOption.iter(fun eff ->
      MetroPerspective.applyParams(eff, MetroPerspective.identity))

  let press(engine: TiltEngine, e: PointerEvent) : unit =
    if engine.Config.Disabled || engine.ReducedMotion then
      snapToRest engine
    else
      let struct (nx, ny) =
        TiltInternals.normalizedOffset(e.Position, engine.Handle.BorderBox)

      engine.IsPressed <- true
      engine.NormalizedX <- nx
      engine.NormalizedY <- ny

      match engine.Effect with
      | ValueSome _ ->
        engine.ShaderAttached <- true

        let targetScale =
          TiltInternals.resolveScale(engine.Config.PressScale3D, 0.985)

        engine.Tilt.To(Point(X = nx, Y = ny), TiltInternals.pressSpec)
        engine.Scale.To(targetScale, TiltInternals.pressSpec)
      | ValueNone ->
        let targetScale =
          TiltInternals.resolveScale(engine.Config.FallbackScale, 0.95)

        let targetOffset =
          TiltInternals.fallbackOffset(nx, ny, engine.Config.FallbackOffsetPx)

        engine.Tilt.To(Point(X = nx, Y = ny), TiltInternals.pressSpec)
        engine.Scale.To(targetScale, TiltInternals.pressSpec)
        engine.Translate.To(targetOffset, TiltInternals.pressSpec)

  let release(engine: TiltEngine) : unit =
    if engine.IsPressed then
      engine.IsPressed <- false
      engine.NormalizedX <- 0.0
      engine.NormalizedY <- 0.0

      if engine.ReducedMotion then
        snapToRest engine
      else
        engine.Tilt.To(TiltInternals.zeroPoint, TiltInternals.releaseSpec)
        engine.Scale.To(1.0, TiltInternals.releaseSpec)

        if engine.Effect.IsNone then
          engine.Translate.To(
            TiltInternals.zeroPoint,
            TiltInternals.releaseSpec
          )
        elif not engine.Tilt.Running && engine.ShaderAttached then
          engine.ShaderAttached <- false

          engine.Effect
          |> ValueOption.iter(fun eff ->
            MetroPerspective.applyParams(eff, MetroPerspective.identity))

  let create(effect: ShaderEffect voption) : TiltEngine =
    let mutable engineSlot: TiltEngine voption = ValueNone

    let onDown =
      Action<PointerEvent>(fun e ->
        engineSlot |> ValueOption.iter(fun eng -> press(eng, e)))

    let onUp =
      Action<PointerEvent>(fun _ -> engineSlot |> ValueOption.iter release)

    let onLeave =
      Action<PointerEvent>(fun _ -> engineSlot |> ValueOption.iter release)

    let onCancel =
      Action<PointerEvent>(fun _ -> engineSlot |> ValueOption.iter release)

    let engine: TiltEngine = {
      Handle = ElementHandle()
      Effect = effect
      Config = TiltInternals.defaultConfig
      IsPressed = false
      ShaderAttached = false
      NormalizedX = 0.0
      NormalizedY = 0.0
      ReducedMotion = false
      Scale = Unchecked.defaultof<Anim<float>>
      Translate = Unchecked.defaultof<Anim<Point>>
      Tilt = Unchecked.defaultof<Anim<Point>>
      PointerDown = onDown
      PointerUp = onUp
      PointerLeave = onLeave
      PointerCancel = onCancel
    }

    engineSlot <- ValueSome engine
    engine

  let attachWithCallback
    (engine: TiltEngine, cell: Cell, onShaderDetached: unit -> unit)
    : unit =
    engine.Scale <- cell.AnimateValue(1.0)
    engine.Translate <- cell.AnimateValue(TiltInternals.zeroPoint)

    match engine.Effect with
    | ValueSome eff ->
      MetroPerspective.applyParams(eff, MetroPerspective.identity)

      let tiltAnim =
        cell.Animate(
          TiltInternals.zeroPoint,
          fun pt ->
            let maxDeg =
              TiltInternals.resolveMaxDegrees engine.Config.MaxDegrees

            MetroPerspective.applyParams(
              eff,
              MetroPerspective.pressTilt(pt.X, pt.Y, maxDeg)
            )
        )

      tiltAnim.add_Completed(
        Action<MotionCompletionReason>(fun _ ->
          if not engine.IsPressed && engine.ShaderAttached then
            engine.ShaderAttached <- false
            MetroPerspective.applyParams(eff, MetroPerspective.identity)
            onShaderDetached())
      )

      engine.Tilt <- tiltAnim
    | ValueNone -> engine.Tilt <- cell.AnimateValue(TiltInternals.zeroPoint)

  let attach(engine: TiltEngine, cell: Cell) : unit =
    // Detaching ShaderEffect after the release animation settles runs from the
    // motion pump, not a Blob callback, so the cell rebuilds itself once.
    attachWithCallback(engine, cell, fun () -> cell.Rebuild())

  let sync
    (engine: TiltEngine, config: MetroTiltConfig, reducedMotion: bool)
    : unit =
    engine.Config <- config
    engine.ReducedMotion <- reducedMotion

    if config.Disabled || reducedMotion then
      snapToRest engine

  let applyToBlob(engine: TiltEngine, blob: Blob) : unit =
    blob.Handle <- engine.Handle
    blob.ScaleAnim <- engine.Scale
    blob.TranslateAnim <- engine.Translate

    if not engine.Config.Disabled then
      blob.OnPointerDown <- engine.PointerDown
      blob.OnPointerUp <- engine.PointerUp
      blob.OnPointerLeave <- engine.PointerLeave
      blob.OnPointerCancel <- engine.PointerCancel

    if engine.ShaderAttached then
      engine.Effect |> ValueOption.iter(fun eff -> blob.ShaderEffect <- eff)

  let snapshot(engine: TiltEngine) : MetroTiltState = {
    IsPressed = engine.IsPressed
    Mode =
      if engine.Effect.IsSome then
        MetroTiltMode.Perspective3D
      else
        MetroTiltMode.Fallback2D
    IsShaderAttached = engine.ShaderAttached
    NormalizedX = engine.NormalizedX
    NormalizedY = engine.NormalizedY
    ScaleAnim = engine.Scale
    TranslateAnim = engine.Translate
    TiltAnim = engine.Tilt
  }

[<RequireQualifiedAccess>]
module MetroTilt =

  let defaultConfig = TiltInternals.defaultConfig
  let pressSpec = TiltInternals.pressSpec
  let releaseSpec = TiltInternals.releaseSpec

  let defaults(child: Blob) : MetroTiltInput = {
    Child = child
    Config = defaultConfig
  }

  let normalizedOffset(position: Point, box: ElementRect) =
    TiltInternals.normalizedOffset(position, box)

  let fallbackOffset(nx: float, ny: float, maxOffsetPx: float) =
    TiltInternals.fallbackOffset(nx, ny, maxOffsetPx)

  let effectiveConfig(input: MetroTiltInput) : MetroTiltConfig =
    if not(isNull input.Child) && input.Child.Disabled then
      { input.Config with Disabled = true }
    else
      input.Config

  let buildView(engine: TiltEngine, input: MetroTiltInput) : Blob =
    let wrapper = Container(FlexShrink = 0.0)
    TiltEngine.applyToBlob(engine, wrapper)

    if not(isNull input.Child) then
      wrapper.Add(input.Child)

    wrapper

  let build(engine: TiltEngine, input: MetroTiltInput) : Blob =
    let theme = Metro.read()
    TiltEngine.sync(engine, effectiveConfig input, theme.ReducedMotion)
    buildView(engine, input)

  let createWithEffect(effect: ShaderEffect voption) : MetroTiltCell =
    let mutable lastInput: MetroTiltInput = defaults null
    let engine = TiltEngine.create effect

    let cell = {
      new MetroTiltCell() with
        override _.State = TiltEngine.snapshot engine

        override _.View = buildView(engine, lastInput)

        override _.InputChanged(input) =
          lastInput <- input
          let cfg = effectiveConfig input
          engine.Config <- cfg

          if cfg.Disabled then
            TiltEngine.snapToRest engine

        override _.Unmounting() = TiltEngine.snapToRest engine

        override _.Build(input) : Blob =
          lastInput <- input
          build(engine, input)
    }

    TiltEngine.attach(engine, cell)
    cell

  let create() : MetroTiltCell =
    createWithEffect(MetroPerspective.tryCreateDefault())

  let defaultFactory() : MetroTiltCell = create()

  let mountInput(key: string, input: MetroTiltInput) : Blob =
    Cell.Mount<MetroTiltInput, MetroTiltCell>(key, input, defaultFactory)

  let mount(key: string, child: Blob) : Blob = mountInput(key, defaults child)

  let mountWith
    (key: string, input: MetroTiltInput, factory: unit -> MetroTiltCell)
    : Blob =
    Cell.Mount<MetroTiltInput, MetroTiltCell>(key, input, factory)
