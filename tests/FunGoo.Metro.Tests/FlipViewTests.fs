module FunGoo.Metro.Tests.FlipViewTests

open System
open System.Diagnostics
open System.Threading
open Xunit
open Goo
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private items = [|
  {
    Id = "alpha"
    AccessibilityName = "Alpha"
  }
  {
    Id = "beta"
    AccessibilityName = "Beta"
  }
  { Id = "gamma"; AccessibilityName = "" }
|]

let private darkConfig = {
  MetroConfig.defaultConfig with
      Mode = MetroMode.Dark
      Accent = MetroAccentKind.Blue
}

let private pointer(x: float, y: float) : PointerEvent =
  PointerEvent(IsPrimary = true, Position = Point(X = x, Y = y))

// xunit Assert.Equal has ambiguous overloads for lists, so lists are
// compared with F# structural equality.
let private assertEvents
  (expected: MetroFlipViewEvent list, actual: MetroFlipViewEvent list)
  : unit =
  if expected <> actual then
    failwith $"Expected events {expected}, got {actual}."

[<Fact>]
let ``MetroFlipView view builds one panel edge buttons and indicator dots``() =
  let viewportHandle = ElementHandle()
  let rendered = ResizeArray<string>()
  let events = ResizeArray<MetroFlipViewEvent>()

  let render(id: string) : Blob =
    rendered.Add id
    Container(Key = $"content-{id}")

  let mutable source: MetroFlipViewItem array = items
  let mutable selectedId = "alpha"
  let mutable showNavigation = true
  let mutable showIndicators = true
  let mutable outgoingId: string voption = ValueNone
  let mutable direction = 1.0
  // The capture cell supplies the paint animation after its creation.
  let mutable track: Anim<Point> = null

  let state() : MetroFlipViewState = {
    Input = {
      MetroFlipView.defaults(source, selectedId) with
          ShowNavigation = showNavigation
          ShowIndicators = showIndicators
          AccessibilityName = ValueSome "Highlights"
    }
    ActiveIndex = MetroFlipView.indexOf(source, selectedId)
    OutgoingId = outgoingId
    Direction = direction
    TrackTranslate = track
    ViewportHandle = viewportHandle
  }

  let mutable build: unit -> Blob = fun () -> Container()

  let root = CaptureCell.create(fun () -> build())

  track <- root.Track
  build <- fun () -> MetroFlipView.buildView(state(), render, events.Add)

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // First item: one active panel, two edge buttons, one indicator row.
  let tree = root.Captured :?> Container
  let viewport = childAt(tree, 0) :?> Container
  Assert.Equal(AccessibilityRole.List, viewport.Accessibility.Role)
  Assert.Equal("Highlights", viewport.Accessibility.Name)
  Assert.Equal(4, childCount viewport)

  let active = requireBlobByKey(viewport, "flip-active") :?> Container
  Assert.Equal(AccessibilityRole.ListItem, active.Accessibility.Role)
  Assert.Equal("Alpha", active.Accessibility.Name)
  Assert.Equal(Nullable true, active.Accessibility.Selected)
  Assert.Equal(1, childCount active)

  if [ "alpha" ] <> List.ofSeq rendered then
    failwith $"Expected the active item only, got {List.ofSeq rendered}."

  // The previous button is disabled on the first item and raises no event.
  let previous = requireBlobByKey(viewport, "flip-previous") :?> Button
  let next = requireBlobByKey(viewport, "flip-next") :?> Button
  Assert.Equal(AccessibilityRole.Button, previous.Accessibility.Role)
  Assert.True(previous.Disabled)
  Assert.False(previous.Focusable)
  Assert.Null(previous.OnClick)
  Assert.False(next.Disabled)
  next.OnClick.Invoke()
  assertEvents([ MetroFlipViewEvent.Navigate 1 ], List.ofSeq events)

  // Indicator dots report the item names and raise navigation events.
  let dots = requireBlobByKey(viewport, "flip-indicators") :?> Container
  Assert.Equal(AccessibilityRole.TabList, dots.Accessibility.Role)
  Assert.Equal(3, childCount dots)
  let secondDot = childAt(dots, 1) :?> Button
  let thirdDot = childAt(dots, 2) :?> Button
  Assert.Equal(AccessibilityRole.Tab, secondDot.Accessibility.Role)
  Assert.Equal("Beta", secondDot.Accessibility.Name)
  Assert.Equal(Nullable false, secondDot.Accessibility.Selected)
  Assert.Equal("gamma", thirdDot.Accessibility.Name)
  secondDot.OnClick.Invoke()
  thirdDot.OnClick.Invoke()

  // Keys: Home, End, PageUp, PageDown, Left, Right.
  for key in
    [| Key.Left; Key.Right; Key.PageUp; Key.PageDown; Key.Home; Key.End |] do
    findKeyBinding(tree.KeyBindings, key) |> ignore

  findKeyBinding(tree.KeyBindings, Key.PageDown).OnPress.Invoke()
  findKeyBinding(tree.KeyBindings, Key.End).OnPress.Invoke()
  findKeyBinding(tree.KeyBindings, Key.Home).OnPress.Invoke()
  findKeyBinding(tree.KeyBindings, Key.Left).OnPress.Invoke()

  findKeyBinding(tree.KeyBindings, Key.Right).OnPress.Invoke()

  assertEvents(
    [
      MetroFlipViewEvent.Navigate 1
      MetroFlipViewEvent.Navigate 1
      MetroFlipViewEvent.Navigate 2
      MetroFlipViewEvent.Navigate 1
      MetroFlipViewEvent.Navigate 2
      MetroFlipViewEvent.Navigate 1
    ],
    List.ofSeq events
  )

  // On the first item, Home raises no event. On the last item, Home goes to
  // the first item, Right raises no event, and Left goes back one item.
  selectedId <- "gamma"
  outgoingId <- ValueNone
  direction <- 1.0
  root.Rebuild()
  pumpFrames(host, 1)
  events.Clear()

  let lastTree = root.Captured :?> Container

  findKeyBinding(lastTree.KeyBindings, Key.Home).OnPress.Invoke()
  findKeyBinding(lastTree.KeyBindings, Key.Right).OnPress.Invoke()
  findKeyBinding(lastTree.KeyBindings, Key.Left).OnPress.Invoke()

  assertEvents(
    [ MetroFlipViewEvent.Navigate 0; MetroFlipViewEvent.Navigate 1 ],
    List.ofSeq events
  )

  Assert.True(
    (requireBlobByKey(childAt(lastTree, 0), "flip-next") :?> Button).Disabled
  )

  Assert.False(
    (requireBlobByKey(childAt(lastTree, 0), "flip-previous") :?> Button)
      .Disabled
  )

  // Pointer events on the content area reach the event handler.
  events.Clear()
  viewport.OnPointerDown.Invoke(pointer(200.0, 100.0))
  viewport.OnPointerMove.Invoke(pointer(170.0, 102.0))
  viewport.OnPointerUp.Invoke(pointer(140.0, 104.0))
  viewport.OnPointerCancel.Invoke(pointer(140.0, 104.0))

  let pointerEvents = List.ofSeq events

  Assert.Equal(4, pointerEvents.Length)

  match pointerEvents with
  | [ PointerDown _; PointerMoved _; PointerUp _; PointerCanceled _ ] -> ()
  | other -> failwith $"Expected four pointer events, got {other.Length}."

  // A backward slide puts the incoming panel first and the outgoing panel second.
  selectedId <- "beta"
  outgoingId <- ValueSome "alpha"
  direction <- -1.0
  root.Rebuild()
  pumpFrames(host, 1)

  let backwardViewport = childAt(root.Captured, 0) :?> Container

  let incoming = childAt(backwardViewport, 0) :?> Container

  let outgoing = childAt(backwardViewport, 1) :?> Container
  Assert.Equal("flip-active", incoming.Key)
  Assert.Equal("Beta", incoming.Accessibility.Name)
  Assert.Equal(Nullable true, incoming.Accessibility.Selected)
  Assert.Equal("flip-outgoing", outgoing.Key)
  Assert.Equal("Alpha", outgoing.Accessibility.Name)
  Assert.Equal(Nullable false, outgoing.Accessibility.Selected)

  // Hidden chrome leaves the content panel only.
  showNavigation <- false
  showIndicators <- false
  outgoingId <- ValueNone
  root.Rebuild()
  pumpFrames(host, 1)
  let bareViewport = childAt(root.Captured, 0) :?> Container
  Assert.Equal(1, childCount bareViewport)

  // A single item shows no edge buttons and no dots.
  showNavigation <- true
  showIndicators <- true
  source <- [| items[0] |]
  root.Rebuild()
  pumpFrames(host, 1)
  let singleViewport = childAt(root.Captured, 0) :?> Container
  Assert.Equal(1, childCount singleViewport)
  Assert.True(findBlobByKey(singleViewport, "flip-next").IsNone)

  // No items: an empty viewport with index -1.
  source <- Array.empty
  root.Rebuild()
  pumpFrames(host, 1)
  Assert.Equal(-1, state().ActiveIndex)
  Assert.Equal(0, childCount(childAt(root.Captured, 0)))

[<Fact>]
let ``MetroFlipViewCell slides on navigation and honors reduced motion``() =
  let mutable selectedId = "alpha"
  let mutable rootRef: CaptureCell voption = ValueNone
  let mutable flipRef: MetroFlipViewCell voption = ValueNone
  let rendered = ResizeArray<string>()

  let onSelect(id: string) =
    selectedId <- id
    rootRef |> ValueOption.iter(fun root -> root.Rebuild())

  let renderItem(id: string) : Blob =
    rendered.Add id
    Container(Key = $"content-{id}")

  let factory() : MetroFlipViewCell =
    let cell = MetroFlipViewCell(onSelect, renderItem)
    flipRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      MetroFlipView.mount(
        "flip",
        MetroFlipView.defaults(items, selectedId),
        factory
      ))

  rootRef <- ValueSome root

  use host = createHost(800, 600)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let cell = flipRef.Value
  Assert.False(cell.IsSliding)
  Assert.Equal(0, cell.State.ActiveIndex)

  let mutable tree: Blob = null

  let viewport() : Container =
    root.DuringBuild(host, fun () -> tree <- cell.View)
    childAt(tree, 0) :?> Container

  // Slide timers follow the wall clock, so the test waits in real time.
  let stopwatch = Stopwatch.StartNew()

  let waitForSlideEnd() =
    let started = stopwatch.ElapsedMilliseconds

    while cell.IsSliding && stopwatch.ElapsedMilliseconds - started < 2000L do
      Thread.Sleep 10
      host.RenderFrame(0.016)

    if cell.IsSliding then
      let track = cell.TrackTranslate

      failwith
        $"The slide did not end within two seconds. Running={track.Running} \
         Value.X={track.Value.X} Target.X={track.Target.X}"

  // Next: the outgoing item leads and the track runs on paint.
  (requireBlobByKey(viewport(), "flip-next") :?> Button).OnClick.Invoke()
  pumpFrames(host, 1)

  Assert.Equal("beta", selectedId)
  Assert.True(cell.IsSliding)
  Assert.Equal(ValueSome "alpha", cell.OutgoingId)
  Assert.Equal(1.0, cell.State.Direction)
  Assert.True(cell.TrackTranslate.Running)

  let sliding = viewport()
  Assert.Equal("flip-outgoing", (childAt(sliding, 0) :?> Container).Key)
  Assert.Equal("flip-active", (childAt(sliding, 1) :?> Container).Key)

  // Frames pass without a rebuild of the cell or of the item content.
  let buildsDuringSlide = rendered.Count

  for _ = 1 to 6 do
    host.RenderFrame(0.016)

  Assert.Equal(buildsDuringSlide, rendered.Count)
  Assert.True(cell.IsSliding)

  // The slide timer ends the slide and the tree holds the active panel only.
  waitForSlideEnd()
  Assert.False(cell.IsSliding)
  Assert.False(cell.TrackTranslate.Running)
  Assert.Equal(0.0, cell.TrackTranslate.Value.X, 3)

  let settled = viewport()
  let settledPanel = childAt(settled, 0) :?> Container
  Assert.Equal("flip-active", settledPanel.Key)
  Assert.Equal("Beta", settledPanel.Accessibility.Name)
  Assert.Equal(4, childCount settled)

  // Previous: the incoming item leads and the track starts one viewport left.
  (requireBlobByKey(settled, "flip-previous") :?> Button).OnClick.Invoke()
  pumpFrames(host, 1)

  Assert.Equal("alpha", selectedId)
  Assert.Equal(-1.0, cell.State.Direction)
  Assert.Equal(ValueSome "beta", cell.OutgoingId)
  Assert.True(cell.TrackTranslate.Value.X < 0.0)
  waitForSlideEnd()

  // A left swipe shows the next item. A right swipe shows the previous item.
  let atAlpha = viewport()
  atAlpha.OnPointerDown.Invoke(pointer(240.0, 100.0))
  atAlpha.OnPointerMove.Invoke(pointer(200.0, 102.0))
  atAlpha.OnPointerUp.Invoke(pointer(160.0, 104.0))
  pumpFrames(host, 1)
  Assert.Equal("beta", selectedId)
  waitForSlideEnd()

  let atBeta = viewport()
  atBeta.OnPointerDown.Invoke(pointer(120.0, 100.0))
  atBeta.OnPointerMove.Invoke(pointer(160.0, 102.0))
  atBeta.OnPointerUp.Invoke(pointer(200.0, 102.0))
  pumpFrames(host, 1)
  Assert.Equal("alpha", selectedId)
  waitForSlideEnd()

  // A short swipe and a vertical swipe do not navigate.
  let stillAlpha = viewport()
  stillAlpha.OnPointerDown.Invoke(pointer(200.0, 100.0))
  stillAlpha.OnPointerUp.Invoke(pointer(170.0, 102.0))
  stillAlpha.OnPointerDown.Invoke(pointer(200.0, 100.0))
  stillAlpha.OnPointerMove.Invoke(pointer(170.0, 160.0))
  stillAlpha.OnPointerUp.Invoke(pointer(160.0, 220.0))
  pumpFrames(host, 1)
  Assert.Equal("alpha", selectedId)

  // A canceled gesture does not navigate.
  let canceledFrom = viewport()
  canceledFrom.OnPointerDown.Invoke(pointer(240.0, 100.0))
  canceledFrom.OnPointerMove.Invoke(pointer(200.0, 102.0))
  canceledFrom.OnPointerCancel.Invoke(pointer(200.0, 102.0))
  canceledFrom.OnPointerUp.Invoke(pointer(160.0, 104.0))
  pumpFrames(host, 1)
  Assert.Equal("alpha", selectedId)

  // Reduced motion: navigation is instant and the track never runs.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 1)

  (requireBlobByKey(viewport(), "flip-next") :?> Button).OnClick.Invoke()
  pumpFrames(host, 1)

  Assert.Equal("beta", selectedId)
  Assert.False(cell.IsSliding)
  Assert.Equal(ValueNone, cell.OutgoingId)
  Assert.False(cell.TrackTranslate.Running)
  Assert.Equal(0.0, cell.TrackTranslate.Value.X, 3)

  // An unknown identifier activates the first item.
  selectedId <- "unknown"
  root.Rebuild()
  pumpFrames(host, 1)
  Assert.Equal(0, cell.State.ActiveIndex)
  Assert.Equal("unknown", cell.State.Input.SelectedId)
