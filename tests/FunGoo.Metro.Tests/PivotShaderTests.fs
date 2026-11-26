module FunGoo.Metro.Tests.PivotShaderTests

open System
open System.IO
open System.Reflection
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private darkConfig = {
  MetroConfig.defaultConfig with
      Mode = MetroMode.Dark
      Accent = MetroAccentKind.Blue
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

let private createSyntheticEffect() : ShaderEffect =
  ShaderEffect(ShaderEffectProgram(writeMinimalEffectBytes()))

let private items = [|
  { Id = "one"; Header = "overview" }
  { Id = "two"; Header = "details" }
|]

let private mountPivot
  (host: HeadlessWindowHost, effect: ShaderEffect voption)
  : CaptureCell * MetroPivotCell =
  let mutable selectedId = "one"
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let pivotFactory() =
    let cell =
      MetroPivot.createWithEffect(
        effect,
        (fun nextId -> selectedId <- nextId),
        fun id -> MetroText.body $"Panel-{id}"
      )

    pivotRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroPivot.mount(
        "pivot-shader",
        MetroPivot.defaults(items, selectedId),
        pivotFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  match pivotRef with
  | ValueSome cell -> root, cell
  | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

let private contentPanel
  (root: CaptureCell, host: HeadlessWindowHost, pivotCell: MetroPivotCell)
  : Container =
  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- pivotCell.View)
  requireBlobByKey(tree, "pivot-content") :?> Container

let private selectTab
  (
    root: CaptureCell,
    host: HeadlessWindowHost,
    pivotCell: MetroPivotCell,
    id: string
  ) : unit =
  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- pivotCell.View)
  let tab = requireBlobByKey(tree, $"pivot-tab-{id}") :?> Button
  tab.OnClick.Invoke()
  pumpFrames(host, 2)

[<Fact>]
let ``Pivot turnstile engages the injected effect and skips the 2D slide``() =
  use host = createHost(800, 600)
  let effect = createSyntheticEffect()
  let root, pivotCell = mountPivot(host, ValueSome effect)

  selectTab(root, host, pivotCell, "two")

  // The 3D path attaches the injected effect to the content panel and drives
  // it through the turnstile progress instead of running the 2D slide.
  Assert.Equal(MetroTiltMode.Perspective3D, pivotCell.Mode)
  Assert.True(pivotCell.IsShaderAttached)
  Assert.True(pivotCell.ProgressAnim.Running)
  Assert.Equal(1.0, pivotCell.ProgressAnim.Target, 3)
  Assert.False(pivotCell.ContentTranslate.Running)
  Assert.False(pivotCell.ContentOpacity.Running)
  Assert.NotNull(contentPanel(root, host, pivotCell))

[<Fact>]
let ``Pivot without an effect keeps the 2D slide``() =
  use host = createHost(800, 600)
  let root, pivotCell = mountPivot(host, ValueNone)

  selectTab(root, host, pivotCell, "two")

  Assert.Equal(MetroTiltMode.Fallback2D, pivotCell.Mode)
  Assert.False(pivotCell.IsShaderAttached)
  Assert.False(pivotCell.ProgressAnim.Running)
  Assert.True(pivotCell.ContentTranslate.Running)
  Assert.True(pivotCell.ContentOpacity.Running)
  Assert.NotNull(contentPanel(root, host, pivotCell))

let private pumpTreeMethod =
  typeof<Window>
    .GetMethod(
      "UpdateTree",
      BindingFlags.Instance ||| BindingFlags.NonPublic,
      null,
      [| typeof<float> |],
      null
    )

let private pumpMotion(window: Window, count: int, dt: float) : unit =
  let args: obj array = [| box dt |]

  for _ = 1 to count do
    pumpTreeMethod.Invoke(window, args) |> ignore

[<Fact>]
let ``Pivot turnstile settles through the public ProgressAnim and detaches exactly once``
  ()
  =
  use host = createHost(800, 600)
  let effect = createSyntheticEffect()
  let mutable selectedId = "one"
  let mutable pivotRef: MetroPivotCell voption = ValueNone
  let mutable renders = 0

  let pivotFactory() =
    let cell =
      MetroPivot.createWithEffect(
        ValueSome effect,
        (fun nextId -> selectedId <- nextId),
        (fun id ->
          renders <- renders + 1
          MetroText.body $"Panel-{id}")
      )

    pivotRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroPivot.mount(
        "pivot-shader",
        MetroPivot.defaults(items, selectedId),
        pivotFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pivotCell =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

  selectTab(root, host, pivotCell, "two")
  Assert.True(pivotCell.ProgressAnim.Running)
  let rendersBeforeSettle = renders

  // A headless RenderFrame does not advance paint animations, so the public
  // ProgressAnim runs to completion through the window motion pump, which is
  // the same path the app frame loop uses.
  let mutable ticks = 0

  while pivotCell.ProgressAnim.Running && ticks < 120 do
    pumpMotion(window, 1, 0.016)
    ticks <- ticks + 1

  Assert.False(pivotCell.ProgressAnim.Running)
  Assert.Equal(1.0, pivotCell.ProgressAnim.Value, 3)

  // The settle handler detaches the effect, resets params to identity, and
  // rebuilds the pivot cell exactly once to drop the effect from the panel.
  // The panel effect property is write-only with no readback, so the
  // detach plus the single rebuild proves that handler ran its reset line.
  pumpMotion(window, 2, 0.016)
  Assert.False(pivotCell.IsShaderAttached)
  Assert.Equal(rendersBeforeSettle + 1, renders)
  Assert.NotNull(contentPanel(root, host, pivotCell))

[<Fact>]
let ``Pivot with an effect snaps to rest under reduced motion``() =
  use host = createHost(800, 600)
  let effect = createSyntheticEffect()
  let root, pivotCell = mountPivot(host, ValueSome effect)

  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 2)
  selectTab(root, host, pivotCell, "two")

  Assert.Equal(MetroTiltMode.Perspective3D, pivotCell.Mode)
  Assert.False(pivotCell.IsShaderAttached)
  Assert.False(pivotCell.ProgressAnim.Running)
  Assert.False(pivotCell.ContentTranslate.Running)
  Assert.False(pivotCell.ContentOpacity.Running)
  Assert.Equal(0.0, pivotCell.ContentTranslate.Value.X, 3)
  Assert.Equal(1.0, pivotCell.ContentOpacity.Value, 3)
  Assert.NotNull(contentPanel(root, host, pivotCell))
