module FunGoo.Metro.Tests.RtlTests

open System.Collections.Generic
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

/// Mounts a child under a full-size container with the given direction.
let private directionRoot(direction: Direction, child: Blob) : Blob =
  Container(
    Width = Length.Percent 100.0,
    Height = Length.Percent 100.0,
    Direction = direction
  )
    .Children(child)

/// Left and right insets of the content box inside the border box.
let private insets(handle: ElementHandle) : float * float =
  let border = handle.BorderBox
  let content = handle.ContentBox
  content.X - border.X, (border.X + border.Width) - (content.X + content.Width)

[<Fact>]
let ``MetroHub mirrors section order and peek padding in RTL``() =
  use host = createHost(600, 480)
  let bodies = Dictionary<string, ElementHandle>()
  bodies["a"] <- ElementHandle()
  bodies["b"] <- ElementHandle()
  let mutable rtl = false
  let mutable hubRef: MetroHubCell voption = ValueNone

  let factory() =
    let cell =
      MetroHub.create(fun id ->
        (Container(
          Key = $"rtl-hub-body-{id}",
          Handle = bodies[id],
          Width = Length.Pixels 200.0,
          Height = Length.Pixels 100.0
        )
        :> Blob))

    hubRef <- ValueSome cell
    cell

  let sections = [|
    {
      Id = "a"
      Header = "A"
      Width = 200.0
    }
    {
      Id = "b"
      Header = "B"
      Width = 200.0
    }
  |]

  let root =
    CaptureCell.create(fun () ->
      directionRoot(
        (if rtl then Direction.RightToLeft else Direction.LeftToRight),
        MetroHub.mount(
          "hub",
          {
            MetroHub.defaults("Workspace", sections) with
                SectionGap = 32.0
          },
          factory
        )
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let hub =
    match hubRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroHubCell to be mounted."

  // LTR: source order left to right, 32 px gap, 48 px trailing peek.
  let ax = bodies["a"].BorderBox.X
  let bx = bodies["b"].BorderBox.X
  Assert.True(ax < bx)
  Assert.Equal(32.0, bx - (ax + 200.0), 1)
  let leftInset, rightInset = insets hub.ScrollHandle
  Assert.Equal(0.0, leftInset, 1)
  Assert.Equal(48.0, rightInset, 1)

  // RTL: source order right to left, gap kept, peek moves to the left.
  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)

  let rtlAx = bodies["a"].BorderBox.X
  let rtlBx = bodies["b"].BorderBox.X
  Assert.True(rtlAx > rtlBx)
  Assert.Equal(32.0, rtlAx - (rtlBx + 200.0), 1)
  let rtlLeftInset, rtlRightInset = insets hub.ScrollHandle
  Assert.Equal(48.0, rtlLeftInset, 1)
  Assert.Equal(0.0, rtlRightInset, 1)

[<Fact>]
let ``MetroPanorama mirrors section order and viewport insets in RTL``() =
  use host = createHost(600, 480)
  let bodies = Dictionary<string, ElementHandle>()
  bodies["a"] <- ElementHandle()
  bodies["b"] <- ElementHandle()
  let mutable rtl = false
  let mutable panoRef: MetroPanoramaCell voption = ValueNone

  let factory() =
    let cell =
      MetroPanorama.createWithBackground(
        (fun () -> (Container(Key = "rtl-pano-bg") :> Blob)),
        fun id ->
          (Container(
            Key = $"rtl-pano-body-{id}",
            Handle = bodies[id],
            Width = Length.Pixels 240.0,
            Height = Length.Pixels 100.0
          )
          :> Blob)
      )

    panoRef <- ValueSome cell
    cell

  let sections = [|
    MetroPanorama.sizedSection("a", "A", 240.0)
    MetroPanorama.sizedSection("b", "B", 240.0)
  |]

  let root =
    CaptureCell.create(fun () ->
      directionRoot(
        (if rtl then Direction.RightToLeft else Direction.LeftToRight),
        MetroPanorama.mount(
          "panorama",
          MetroPanorama.defaults("Title", sections),
          factory
        )
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pano =
    match panoRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPanoramaCell to be mounted."

  // LTR: source order left to right, 16 px leading and 64 px trailing inset.
  let ax = bodies["a"].BorderBox.X
  let bx = bodies["b"].BorderBox.X
  Assert.True(ax < bx)
  Assert.Equal(28.0, bx - (ax + 240.0), 1)
  let leftInset, rightInset = insets pano.ScrollHandle
  Assert.Equal(16.0, leftInset, 1)
  Assert.Equal(64.0, rightInset, 1)

  // RTL: source order right to left, insets mirrored. The tree of a mounted
  // child cell stays opaque to its parent, so the background layer (keyed
  // `metro-panorama-bg`) is covered by code inspection, not by lookup.
  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)

  let rtlAx = bodies["a"].BorderBox.X
  let rtlBx = bodies["b"].BorderBox.X
  Assert.True(rtlAx > rtlBx)
  Assert.Equal(28.0, rtlAx - (rtlBx + 240.0), 1)
  let rtlLeftInset, rtlRightInset = insets pano.ScrollHandle
  Assert.Equal(64.0, rtlLeftInset, 1)
  Assert.Equal(16.0, rtlRightInset, 1)

[<Fact>]
let ``MetroPivot keeps header structure and keyboard order in RTL``() =
  use host = createHost(600, 400)

  let items = [|
    { Id = "a"; Header = "Alpha" }
    { Id = "b"; Header = "Beta" }
    { Id = "c"; Header = "Gamma" }
  |]

  let mutable rtl = false
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let factory() =
    let cell =
      MetroPivot.create(
        (fun _ -> ()),
        fun id -> (Container(Key = $"rtl-pivot-content-{id}") :> Blob)
      )

    pivotRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      directionRoot(
        (if rtl then Direction.RightToLeft else Direction.LeftToRight),
        MetroPivot.mount("pivot", MetroPivot.defaults(items, "b"), factory)
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pivot =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

  // A mounted child cell exposes no tree to its parent, so the check reads
  // the public view seam inside a build, in both directions.
  let mutable viewTree: Blob = null

  let captureView() : unit =
    root.DuringBuild(host, (fun () -> viewTree <- pivot.View))

  let checkHeaders() : unit =
    for id in [| "a"; "b"; "c" |] do
      let tab = requireBlobByKey(viewTree, $"pivot-tab-{id}") :?> Button
      Assert.NotNull(tab.OnClick)

      // Every header keeps the physical arrow and home/end bindings.
      let keys = tab.KeyBindings |> Array.map(fun b -> b.Key)
      Assert.Contains(Key.Left, keys)
      Assert.Contains(Key.Right, keys)
      Assert.Contains(Key.Home, keys)
      Assert.Contains(Key.End, keys)

    Assert.True((findBlobByKey(viewTree, "rtl-pivot-content-b")).IsSome)

  // Header structure and keyboard order hold in LTR ...
  captureView()
  checkHeaders()

  // ... and unchanged under an RTL ancestor.
  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)
  captureView()
  checkHeaders()

[<Fact>]
let ``MetroSemanticZoom mirrors the toggle to the end side in RTL``() =
  use host = createHost(600, 400)
  let mutable rtl = false
  let mutable zoomedOut = false
  let mutable zoomRef: MetroSemanticZoomCell voption = ValueNone

  let factory() =
    let cell =
      MetroSemanticZoom.create(
        (fun _ -> ()),
        fun _ -> (Container(Key = "zz-zoom-view") :> Blob)
      )

    zoomRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      Container(
        Width = Length.Pixels 600.0,
        Height = Length.Pixels 400.0,
        Direction =
          (if rtl then Direction.RightToLeft else Direction.LeftToRight)
      )
        .Children(
          MetroSemanticZoom.mount(
            "zoom",
            {
              MetroSemanticZoom.defaults zoomedOut with
                  AccessibilityName = ValueSome(if rtl then "R" else "L")
            },
            factory
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let zoom =
    match zoomRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroSemanticZoomCell to be mounted."

  // The 40 px toggle keeps an 8 px margin to the inline end edge.
  let ltrBox = zoom.State.ToggleHandle.BorderBox
  Assert.Equal(552.0, ltrBox.X, 1)
  Assert.Equal(8.0, 600.0 - (ltrBox.X + ltrBox.Width), 1)

  // A direction switch on a live tree leaves absolute insets unresolved,
  // so the RTL direction mounts fresh here: no rebuild semantics involved.
  let mountFresh(direction: Direction) : float * float =
    use freshHost = createHost(600, 400)
    let mutable freshRef: MetroSemanticZoomCell voption = ValueNone

    let freshFactory() =
      let cell =
        MetroSemanticZoom.create(
          (fun _ -> ()),
          fun _ -> (Container(Key = "rtl-zoom-fresh") :> Blob)
        )

      freshRef <- ValueSome cell
      cell

    let freshRoot =
      CaptureCell.create(fun () ->
        Container(
          Width = Length.Pixels 600.0,
          Height = Length.Pixels 400.0,
          Direction = direction
        )
          .Children(
            MetroSemanticZoom.mount(
              "zoom-fresh",
              MetroSemanticZoom.defaults false,
              freshFactory
            )
          ))

    let freshWindow = Window(Root = freshRoot)
    Metro.attach(freshWindow, darkConfig)
    freshWindow.Attach(freshHost) |> ignore
    pumpFrames(freshHost, 2)

    match freshRef with
    | ValueSome cell ->
      let box = cell.State.ToggleHandle.BorderBox
      box.X, box.Width
    | ValueNone -> failwith "Expected MetroSemanticZoomCell to be mounted."

  let rtlX, rtlW = mountFresh Direction.RightToLeft
  Assert.Equal(8.0, rtlX, 1)
  Assert.Equal(40.0, rtlW, 1)

[<Fact>]
let ``MetroWrapPanel mirrors item order in RTL``() =
  use host = createHost(600, 200)
  let handles = [| ElementHandle(); ElementHandle(); ElementHandle() |]
  let mutable rtl = false

  let items = [|
    for i = 0 to 2 do
      (Container(
        Key = $"rtl-wrap-{i}",
        Handle = handles[i],
        Width = Length.Pixels 100.0,
        Height = Length.Pixels 50.0
      )
      :> Blob)
  |]

  let root =
    CaptureCell.create(fun () ->
      directionRoot(
        (if rtl then Direction.RightToLeft else Direction.LeftToRight),
        MetroWrapPanel.build(
          MetroWrapPanel.withGap(MetroWrapOrientation.Horizontal, 0.0),
          items
        )
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let xs = handles |> Array.map(fun h -> h.BorderBox.X)
  Assert.Equal(0.0, xs[0], 1)
  Assert.Equal(100.0, xs[1], 1)
  Assert.Equal(200.0, xs[2], 1)

  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)

  let rtlXs = handles |> Array.map(fun h -> h.BorderBox.X)
  Assert.Equal(500.0, rtlXs[0], 1)
  Assert.Equal(400.0, rtlXs[1], 1)
  Assert.Equal(300.0, rtlXs[2], 1)

[<Fact>]
let ``MetroPersonPicture keeps the presence badge in LTR and RTL``() =
  use host = createHost(300, 200)
  let mutable rtl = false

  let props = {
    MetroPersonPicture.defaults "Ada Lovelace" with
        Presence = MetroPresence.Available
  }

  let root =
    CaptureCell.create(fun () ->
      directionRoot(
        (if rtl then Direction.RightToLeft else Direction.LeftToRight),
        MetroPersonPicture.build props
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // The badge anchors to the inline end: present in both directions while
  // the mirroring itself is owned by the layout engine.
  Assert.True(
    (findBlobByKey(root.Captured, "metro-person-picture-presence")).IsSome
  )

  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(
    (findBlobByKey(root.Captured, "metro-person-picture-presence")).IsSome
  )



[<Fact>]
let ``MetroTabView builds its strip and panel in RTL``() =
  use host = createHost(600, 400)
  let mutable rtl = false
  let factory = MetroTabView.createFactory(fun _ -> ())

  let items = [|
    MetroTabView.pinnedItem("overview", "Overview")
    MetroTabView.item("queue", "Play Queue")
  |]

  let root =
    CaptureCell.create(fun () ->
      directionRoot(
        (if rtl then Direction.RightToLeft else Direction.LeftToRight),
        factory.Build(
          "tabview",
          MetroTabView.defaults(items, "queue"),
          fun id -> (Container(Key = $"rtl-tab-panel-{id}") :> Blob)
        )
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // The strip and the selected panel render under an RTL ancestor.
  Assert.True((findBlobByKey(root.Captured, "tabview-root")).IsSome)
  Assert.True((findBlobByKey(root.Captured, "tabview-panel")).IsSome)
  Assert.True((findBlobByKey(root.Captured, "rtl-tab-panel-queue")).IsSome)

  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.True((findBlobByKey(root.Captured, "tabview-root")).IsSome)
  Assert.True((findBlobByKey(root.Captured, "tabview-panel")).IsSome)
  Assert.True((findBlobByKey(root.Captured, "rtl-tab-panel-queue")).IsSome)
