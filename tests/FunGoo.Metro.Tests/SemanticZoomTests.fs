module FunGoo.Metro.Tests.SemanticZoomTests

open System
open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

[<Fact>]
let ``MetroSemanticZoom view shows one view and a zoom toggle``() =
  let events = ResizeArray<MetroSemanticZoomEvent>()
  let rendered = ResizeArray<bool>()

  let render(isZoomedOut: bool) : Blob =
    rendered.Add isZoomedOut
    Container(Key = (if isZoomedOut then "overview" else "detail"))

  let mutable isZoomedOut = false
  let mutable showToggle = true
  let mutable scale: Anim<float> = null
  let mutable alpha: Anim<float> = null

  let state() : MetroSemanticZoomState = {
    Input = {
      MetroSemanticZoom.defaults isZoomedOut with
          ShowToggleButton = showToggle
          AccessibilityName = ValueSome "Apps"
    }
    ScaleAnim = scale
    OpacityAnim = alpha
  }

  let mutable build: unit -> Blob = fun () -> Container()
  let root = CaptureCell.create(fun () -> build())
  scale <- root.Scale
  alpha <- root.Alpha

  build <- fun () -> MetroSemanticZoom.buildView(state(), render, events.Add)

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // Zoomed in: the detail view and a toggle that offers to zoom out.
  let tree = root.Captured :?> Container
  Assert.Equal(AccessibilityRole.Group, tree.Accessibility.Role)
  Assert.Equal("Apps", tree.Accessibility.Name)
  Assert.Equal(2, childCount tree)

  let view = requireBlobByKey(tree, "zoom-view") :?> Container
  Assert.Equal(1, childCount view)
  Assert.Equal("detail", (childAt(view, 0)).Key)
  assertListEqual([ false ], List.ofSeq rendered)

  let toggle = requireBlobByKey(tree, "zoom-toggle") :?> Button
  Assert.Equal(AccessibilityRole.Button, toggle.Accessibility.Role)
  Assert.Equal("Zoom out", toggle.Accessibility.Name)
  toggle.OnClick.Invoke()
  assertListEqual([ MetroSemanticZoomEvent.ZoomOut ], List.ofSeq events)

  // Zoomed out: the overview view and a toggle that offers to zoom in.
  isZoomedOut <- true
  root.Rebuild()
  pumpFrames(host, 1)

  let zoomedTree = root.Captured :?> Container
  let zoomedView = requireBlobByKey(zoomedTree, "zoom-view") :?> Container
  Assert.Equal("overview", (childAt(zoomedView, 0)).Key)
  assertListEqual([ false; true ], List.ofSeq rendered)

  let zoomedInToggle = requireBlobByKey(zoomedTree, "zoom-toggle") :?> Button
  Assert.Equal("Zoom in", zoomedInToggle.Accessibility.Name)
  zoomedInToggle.OnClick.Invoke()

  assertListEqual(
    [ MetroSemanticZoomEvent.ZoomOut; MetroSemanticZoomEvent.ZoomIn ],
    List.ofSeq events
  )

  // A hidden toggle leaves the view only.
  showToggle <- false
  root.Rebuild()
  pumpFrames(host, 1)
  Assert.Equal(1, childCount(root.Captured))

[<Fact>]
let ``MetroSemanticZoomCell zooms on request and honors reduced motion``() =
  let mutable zoomedOut = false
  let mutable rootRef: CaptureCell voption = ValueNone
  let mutable zoomRef: MetroSemanticZoomCell voption = ValueNone
  let events = ResizeArray<MetroSemanticZoomEvent>()

  let onEvent(event: MetroSemanticZoomEvent) =
    events.Add event
    zoomedOut <- (event = MetroSemanticZoomEvent.ZoomOut)
    rootRef |> ValueOption.iter(fun root -> root.Rebuild())

  let render(isZoomedOut: bool) : Blob =
    Container(Key = (if isZoomedOut then "overview" else "detail"))

  let factory() : MetroSemanticZoomCell =
    let cell = MetroSemanticZoomCell(onEvent, render)
    zoomRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroSemanticZoom.mount(
        "zoom",
        MetroSemanticZoom.defaults zoomedOut,
        factory
      ))

  rootRef <- ValueSome root

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let cell = zoomRef.Value
  Assert.False(cell.IsZoomedOut)
  Assert.Equal(1.0, cell.ScaleAnim.Value, 3)
  Assert.False(cell.ScaleAnim.Running)

  let mutable tree: Blob = null

  let toggle() : Button =
    root.DuringBuild(host, fun () -> tree <- cell.View)
    requireBlobByKey(tree, "zoom-toggle") :?> Button

  // The toggle zooms out, and the view transition starts on paint.
  toggle().OnClick.Invoke()
  pumpFrames(host, 1)

  Assert.True(cell.IsZoomedOut)
  Assert.True(cell.ScaleAnim.Running)
  Assert.True(cell.OpacityAnim.Running)
  assertListEqual([ MetroSemanticZoomEvent.ZoomOut ], List.ofSeq events)

  // A request for the view that is already active raises nothing.
  cell.Handle(MetroSemanticZoomEvent.ZoomOut)
  assertListEqual([ MetroSemanticZoomEvent.ZoomOut ], List.ofSeq events)

  cell.Handle(MetroSemanticZoomEvent.ZoomIn)
  pumpFrames(host, 1)
  Assert.False(cell.IsZoomedOut)

  assertListEqual(
    [ MetroSemanticZoomEvent.ZoomOut; MetroSemanticZoomEvent.ZoomIn ],
    List.ofSeq events
  )

  // Reduced motion: the view changes at once and no animation runs.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 1)
  toggle().OnClick.Invoke()
  pumpFrames(host, 1)

  Assert.True(cell.IsZoomedOut)
  Assert.False(cell.ScaleAnim.Running)
  Assert.False(cell.OpacityAnim.Running)
  Assert.Equal(1.0, cell.ScaleAnim.Value, 3)
  Assert.Equal(1.0, cell.OpacityAnim.Value, 3)

[<Fact>]
let ``A tile of a zoom view belongs to the zoom cell so the host rebuilds itself``
  ()
  =
  // The host cell owns the zoom state. Its render callback builds one tile
  // whose click asks for the detail view.
  let mutable hostZoomedOut = false
  let mutable hostRebuildsItself = false
  let mutable hostRef: CaptureCell voption = ValueNone
  let mutable zoomRef: MetroSemanticZoomCell voption = ValueNone
  let mutable lastTile: Blob = null

  let onEvent(event: MetroSemanticZoomEvent) =
    hostZoomedOut <- (event = MetroSemanticZoomEvent.ZoomOut)
    hostRef |> ValueOption.iter(fun host -> host.Rebuild())

  // The zoom cell calls this callback inside its own Build.
  let render(_isZoomedOut: bool) : Blob =
    let tile =
      Button(
        Key = "pick",
        OnClick =
          (fun () ->
            hostZoomedOut <- false

            if hostRebuildsItself then
              hostRef |> ValueOption.iter(fun host -> host.Rebuild()))
      )

    lastTile <- tile
    tile

  let factory() : MetroSemanticZoomCell =
    let cell = MetroSemanticZoomCell(onEvent, render)
    zoomRef <- ValueSome cell
    cell

  let host =
    CaptureCell.create(fun () ->
      MetroSemanticZoom.mount(
        "zoom",
        MetroSemanticZoom.defaults hostZoomedOut,
        factory
      ))

  hostRef <- ValueSome host

  use windowHost = createHost(800, 600)
  let window = Window(Root = host)
  Metro.attach(window, darkConfig)
  window.Attach(windowHost) |> ignore
  pumpFrames(windowHost, 2)

  let cell = zoomRef.Value
  cell.Handle(MetroSemanticZoomEvent.ZoomOut)
  pumpFrames(windowHost, 1)
  Assert.True(cell.IsZoomedOut)

  // The zoom cell creates the tile, so it owns the callback. Without a
  // rebuild of the host, the zoom cell keeps its input.
  (lastTile :?> Button).OnClick.Invoke()
  pumpFrames(windowHost, 1)
  Assert.False(hostZoomedOut)
  Assert.True(cell.IsZoomedOut)

  // The host follows the contract and rebuilds itself. The zoom cell follows.
  hostRebuildsItself <- true
  (lastTile :?> Button).OnClick.Invoke()
  pumpFrames(windowHost, 1)
  Assert.False(cell.IsZoomedOut)
