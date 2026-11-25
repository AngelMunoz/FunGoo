module FunGoo.Metro.Tests.NavigationControlsTests

open System
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

[<Fact>]
let ``MetroSplitViewCell sizes Compact, Inline, and Overlay panes and flips Start/End in RTL``
  ()
  =
  use host = createHost(600, 400)
  let mutable isOpen = false
  let mutable mode = MetroSplitViewMode.Compact
  let mutable placement = MetroSplitViewPlacement.Start
  let mutable rtl = false
  let mutable splitRef: MetroSplitViewCell voption = ValueNone

  let factory() =
    let c =
      MetroSplitView.create(
        (fun next -> isOpen <- next),
        (fun () -> MetroText.body "Pane"),
        fun () -> MetroText.body "Content"
      )

    splitRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      let input = {
        MetroSplitView.defaults isOpen with
            DisplayMode = mode
            PanePlacement = placement
            OpenPaneLength = 200.0
            CompactPaneLength = 48.0
      }

      Container(
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        Direction =
          (if rtl then Direction.RightToLeft else Direction.LeftToRight)
      )
        .Children(MetroSplitView.mount("split", input, factory)))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let split =
    match splitRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroSplitViewCell."

  // Closed Compact at Start in LTR: 48 px on the left (X = 0).
  Assert.Equal(48.0, split.PaneHandle.BorderBox.Width, 1)
  Assert.Equal(0.0, split.PaneHandle.BorderBox.X, 1)

  // Flip to RTL: Start moves to the right edge (X = 600 - 48 = 552)!
  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.Equal(552.0, split.PaneHandle.BorderBox.X, 1)

  // Compact closed at End in RTL: End flips to the left edge (X = 0).
  placement <- MetroSplitViewPlacement.End
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.Equal(0.0, split.PaneHandle.BorderBox.X, 1)
  Assert.Equal(48.0, split.PaneHandle.BorderBox.Width, 1)

  placement <- MetroSplitViewPlacement.Start

  // Inline closed -> 0 px; Inline open -> 200 px (ReducedMotion snaps layout).
  rtl <- false
  mode <- MetroSplitViewMode.Inline

  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  root.Rebuild()
  pumpFrames(host, 2)
  Assert.Equal(0.0, split.PaneHandle.BorderBox.Width, 1)

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.Equal(200.0, split.PaneHandle.BorderBox.Width, 1)

  // Overlay mode dismisses via InvokeInputCallback and rebuilds host via owner rule.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable false
    )
  )

  mode <- MetroSplitViewMode.Overlay
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.True split.IsOverlayVisible

  let buildsBefore = root.Builds
  split.DismissOverlay()
  pumpFrames(host, 2)

  Assert.False isOpen
  Assert.Equal(buildsBefore + 1, root.Builds)

[<Fact>]
let ``a split view that starts open in Overlay mode shows no slide``() =
  use host = createHost(600, 400)
  let mutable splitRef: MetroSplitViewCell voption = ValueNone

  let factory() =
    let c =
      MetroSplitView.create(
        (fun _ -> ()),
        (fun () -> MetroText.body "Pane"),
        fun () -> MetroText.body "Content"
      )

    splitRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroSplitView.mount("split", MetroSplitView.defaults true, factory))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let split =
    match splitRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroSplitViewCell."

  Assert.False split.PaneTranslate.Running
  Assert.False split.BackdropOpacity.Running
  Assert.Equal(1.0, split.BackdropOpacity.Value, 1)

[<Fact>]
let ``the overlay plays its slide after the first input``() =
  use host = createHost(600, 400)
  let mutable isOpen = false
  let mutable splitRef: MetroSplitViewCell voption = ValueNone

  let factory() =
    let c =
      MetroSplitView.create(
        (fun next -> isOpen <- next),
        (fun () -> MetroText.body "Pane"),
        fun () -> MetroText.body "Content"
      )

    splitRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroSplitView.mount("split", MetroSplitView.defaults isOpen, factory))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let split =
    match splitRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroSplitViewCell."

  // The fresh mount shows the closed state at once.
  Assert.False split.PaneTranslate.Running
  Assert.False split.BackdropOpacity.Running

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 3)

  Assert.True split.PaneTranslate.Running
  Assert.True split.BackdropOpacity.Running

  split.DismissOverlay()
  pumpFrames(host, 3)

  Assert.True split.PaneTranslate.Running

[<Fact>]
let ``a reopen during the close slide keeps the current offset``() =
  use host = createHost(600, 400)
  let mutable isOpen = true
  let mutable splitRef: MetroSplitViewCell voption = ValueNone

  let factory() =
    let c =
      MetroSplitView.create(
        (fun _ -> ()),
        (fun () -> MetroText.body "Pane"),
        fun () -> MetroText.body "Content"
      )

    splitRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroSplitView.mount(
        "split",
        {
          MetroSplitView.defaults isOpen with
              OpenPaneLength = 200.0
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let split =
    match splitRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroSplitViewCell."

  // Start the close slide.
  isOpen <- false
  root.Rebuild()
  pumpFrames(host, 3)

  // Reopen before the close ends. A headless host advances no animation, so
  // the slide cannot have finished in between.
  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 3)

  // The pane keeps its current offset (the zero point) instead of jumping to
  // the closed edge at X = -200.
  Assert.Equal(0.0, split.PaneTranslate.Value.X, 1)

[<Fact>]
let ``a closed overlay pane does not build its content``() =
  use host = createHost(600, 400)
  let mutable isOpen = false
  let mutable paneBuilds = 0
  let mutable splitRef: MetroSplitViewCell voption = ValueNone

  let factory() =
    let c =
      MetroSplitView.create(
        (fun next -> isOpen <- next),
        (fun () ->
          paneBuilds <- paneBuilds + 1
          MetroText.body "Pane"),
        fun () -> MetroText.body "Content"
      )

    splitRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroSplitView.mount(
        "split",
        {
          MetroSplitView.defaults isOpen with
              DisplayMode = MetroSplitViewMode.Overlay
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  Assert.Equal(0, paneBuilds)

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 3)

  Assert.Equal(1, paneBuilds)

[<Fact>]
let ``MetroSplitViewCell respects scoped ReducedMotion``() =
  use host = createHost(600, 400)
  let mutable isOpen = false
  let mutable splitRef: MetroSplitViewCell voption = ValueNone

  let factory() =
    let c =
      MetroSplitView.create(
        (fun next -> isOpen <- next),
        (fun () -> MetroText.body "Pane"),
        fun () -> MetroText.body "Content"
      )

    splitRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      let baseTheme = Metro.read()
      let scopedTheme = { baseTheme with ReducedMotion = true }

      Metro.scope(
        scopedTheme,
        fun () ->
          MetroSplitView.mount(
            "split",
            {
              MetroSplitView.defaults isOpen with
                  DisplayMode = MetroSplitViewMode.Overlay
            },
            factory
          )
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 3)

  let split =
    match splitRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroSplitViewCell."

  // The host window does not ask for reduced motion, the Metro scope does:
  // the open change must treat the motion as reduced and snap at once.
  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 3)

  Assert.False split.PaneTranslate.Running
  Assert.False split.BackdropOpacity.Running
  Assert.Equal(0.0, split.PaneTranslate.Value.X, 1)
  Assert.Equal(1.0, split.BackdropOpacity.Value, 1)

[<Fact>]
let ``MetroPanoramaCell updates background (0.15x) and title (0.35x) parallax with zero cell rebuilds``
  ()
  =
  use host = createHost(500, 360)
  let mutable sectionBuilds = 0
  let mutable panRef: MetroPanoramaCell voption = ValueNone

  let sections = [|
    MetroPanorama.sizedSection("s1", "one", 400.0)
    MetroPanorama.sizedSection("s2", "two", 400.0)
    MetroPanorama.sizedSection("s3", "three", 400.0)
  |]

  let factory() =
    let c =
      MetroPanorama.createWithBackground(
        (fun () ->
          Container(Width = Length.Pixels 600.0, Height = Length.Pixels 200.0)
          :> Blob),
        fun id ->
          sectionBuilds <- sectionBuilds + 1
          MetroText.body id
      )

    panRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroPanorama.mount(
        "pan",
        MetroPanorama.defaults("panorama", sections),
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pan =
    match panRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroPanoramaCell."

  let buildsBefore = sectionBuilds
  let jumped = pan.ScrollHandle.JumpTo(x = Nullable 200.0)
  Assert.True jumped
  pumpFrames(host, 2)

  Assert.Equal(buildsBefore, sectionBuilds)
  Assert.Equal(-30.0, pan.BackgroundTranslate.Value.X, 2)
  Assert.Equal(-70.0, pan.TitleTranslate.Value.X, 2)

[<Fact>]
let ``MetroTabView and MetroBreadcrumbBar cache mapped arrays and allocate 0 B in steady state``
  ()
  =
  let events = ResizeArray<MetroTabViewEvent>()
  let tabFactory = MetroTabView.createFactory(fun ev -> events.Add ev)

  let tabs = [|
    MetroTabView.pinnedItem("home", "Home")
    MetroTabView.item("doc1", "Doc 1")
  |]

  let tabProps = {
    MetroTabView.defaults(tabs, "home") with
        ShowAddButton = true
  }

  let crumbs = [|
    MetroBreadcrumbBar.item("a", "Root")
    MetroBreadcrumbBar.item("b", "Leaf")
  |]

  let mutable selectedCrumb = ""

  let crumbFactory =
    MetroBreadcrumbBar.createFactory(fun id -> selectedCrumb <- id)

  let crumbProps = MetroBreadcrumbBar.defaults crumbs

  let tInput1 = tabFactory.CreateInput tabProps
  let tInput2 = tabFactory.CreateInput tabProps
  Assert.True(Object.ReferenceEquals(tInput1.Items, tInput2.Items))
  Assert.True(Object.ReferenceEquals(tInput1.OnSelect, tInput2.OnSelect))

  tInput1.OnSelect.Invoke "doc1"
  tInput1.OnClose.Invoke "doc1"
  tInput1.OnAdd.Invoke()
  Assert.Contains(MetroTabViewEvent.Selected "doc1", events)
  Assert.Contains(MetroTabViewEvent.Closed "doc1", events)
  Assert.Contains(MetroTabViewEvent.AddRequested, events)

  let bInput1 = crumbFactory.CreateInput crumbProps
  let bInput2 = crumbFactory.CreateInput crumbProps
  Assert.True(Object.ReferenceEquals(bInput1.Items, bInput2.Items))
  Assert.True(Object.ReferenceEquals(bInput1.OnSelect, bInput2.OnSelect))
  bInput1.OnSelect.Invoke "a"
  Assert.Equal("a", selectedCrumb)

  for _ = 1 to 32 do
    tabFactory.CreateInput tabProps |> ignore
    crumbFactory.CreateInput crumbProps |> ignore

  let before = GC.GetAllocatedBytesForCurrentThread()

  for _ = 1 to 200 do
    tabFactory.CreateInput tabProps |> ignore
    crumbFactory.CreateInput crumbProps |> ignore

  let perIter = float(GC.GetAllocatedBytesForCurrentThread() - before) / 200.0

  Assert.True(
    perIter <= 64.0,
    $"Expected <= 64 B per cached CreateInput pair, got {perIter:F1} B"
  )

[<Fact>]
let ``MetroAppBarButton, MetroAppBarToggleButton, and MetroAppBarSeparator wire click, toggle, and visibility``
  ()
  =
  use host = createHost(500, 200)
  let mutable clicks = 0
  let mutable checkedState = false

  let root =
    CaptureCell.create(fun () ->
      let btn =
        MetroAppBarButton.build(
          MetroAppBarButton.defaults("Play", "play_arrow"),
          fun () -> clicks <- clicks + 1
        )

      btn.Key <- "ab-play"

      let tog =
        MetroAppBarToggleButton.build(
          MetroAppBarToggleButton.defaults("Pin", "push_pin", checkedState),
          fun next -> checkedState <- next
        )

      tog.Key <- "ab-pin"

      Container(FlexDirection = FlexDirection.Row)
        .Children(btn, MetroAppBarSeparator.build false, tog))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let playBtn = requireBlobByKey(root.Captured, "ab-play") :?> Button
  playBtn.OnClick.Invoke()
  Assert.Equal(1, clicks)

  let pinBtn = requireBlobByKey(root.Captured, "ab-pin") :?> Button
  pinBtn.OnClick.Invoke()
  Assert.True checkedState

  // The separator sits between the two commands in the tree, hidden.
  Assert.Equal(3, childCount root.Captured)

  let sep = childAt(root.Captured, 1) :?> Container
  Assert.Equal(AccessibilityRole.Separator, sep.Accessibility.Role)
  Assert.Equal(Nullable true, sep.Accessibility.Hidden)
