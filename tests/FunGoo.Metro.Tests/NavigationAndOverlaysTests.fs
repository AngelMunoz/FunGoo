module FunGoo.Metro.Tests.NavigationAndOverlaysTests

open System
open System.Collections
open System.Reflection
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let private flags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

let private entriesMethod = typeof<Style>.GetMethod("Entries", flags)
let private windowNodeField = typeof<Window>.GetField("node", flags)

let private updateTreeMethod =
  typeof<Window>
    .GetMethod(
      "UpdateTree",
      BindingFlags.Instance ||| BindingFlags.NonPublic,
      null,
      [| typeof<float> |],
      null
    )

type private StyleSample = {
  FieldName: string
  A: float32
  B: float32
  C: float32
  D: float32
  Payload: obj
}

let private readEntries(style: Style) : StyleSample array =
  if isNull style then
    Array.empty
  else
    let entries = entriesMethod.Invoke(style, null)

    if isNull entries then
      Array.empty
    else
      let entriesType = entries.GetType()

      let count =
        entriesType.GetProperty("Count", flags).GetValue(entries) :?> int

      let atMethod = entriesType.GetMethod("At", flags)

      Array.init count (fun i ->
        let e = atMethod.Invoke(entries, [| box i |])
        let et = e.GetType()

        {
          FieldName = et.GetField("Field", flags).GetValue(e).ToString()
          A = et.GetField("A", flags).GetValue(e) :?> float32
          B = et.GetField("B", flags).GetValue(e) :?> float32
          C = et.GetField("C", flags).GetValue(e) :?> float32
          D = et.GetField("D", flags).GetValue(e) :?> float32
          Payload = et.GetField("Payload", flags).GetValue(e)
        })

let private tryLastEntry
  (style: Style, fieldName: string)
  : StyleSample voption =
  let entries = readEntries style
  let mutable found = ValueNone

  for i = entries.Length - 1 downto 0 do
    if found.IsNone && entries[i].FieldName = fieldName then
      found <- ValueSome entries[i]

  found

let private requireEntry(style: Style, fieldName: string) : StyleSample =
  match tryLastEntry(style, fieldName) with
  | ValueSome s -> s
  | ValueNone -> failwith $"Expected style entry '{fieldName}' to be present."

let private assertColorEqual(expected: Color, sample: StyleSample) : unit =
  Assert.Equal(expected.R, sample.A, 4)
  Assert.Equal(expected.G, sample.B, 4)
  Assert.Equal(expected.B, sample.C, 4)
  Assert.Equal(expected.A, sample.D, 4)

let private boxShadowCount(style: Style) : int =
  let payload = requireEntry(style, "BoxShadows").Payload

  if isNull payload then
    0
  else
    payload.GetType().GetProperty("Count", flags).GetValue(payload) :?> int

let private childCount(blob: Blob) : int =
  match blob with
  | :? Container as c ->
    let list = c.Children
    list.Count
  | :? Button as b ->
    let list = b.Children
    list.Count
  | :? Portal as p ->
    let list = typeof<Portal>.GetProperty("Children", flags).GetValue(p)

    list.GetType().GetProperty("Count", flags).GetValue(list) :?> int
  | other ->
    failwith $"Blob of type {other.GetType().Name} has no Children collection."

let private childAt(blob: Blob, index: int) : Blob =
  match blob with
  | :? Container as c ->
    let list = c.Children
    list[index]
  | :? Button as b ->
    let list = b.Children
    list[index]
  | :? Portal as p ->
    let list = typeof<Portal>.GetProperty("Children", flags).GetValue(p)

    list.GetType().GetProperty("Item", flags).GetValue(list, [| box index |])
    :?> Blob
  | other ->
    failwith $"Blob of type {other.GetType().Name} has no Children collection."

let private pumpMotion(window: Window, count: int, dt: float) : unit =
  let args: obj array = [| box dt |]

  for _ = 1 to count do
    updateTreeMethod.Invoke(window, args) |> ignore

let private directChildField = typeof<Cell>.GetField("directChild", flags)

let private findMountedCell<'TCell when 'TCell :> Cell>
  (window: Window)
  : 'TCell voption =
  let rootNode = windowNodeField.GetValue(window)

  let rec checkFiberChain(cellObj: obj) : 'TCell voption =
    if isNull cellObj then
      ValueNone
    else
      match cellObj with
      | :? 'TCell as matched -> ValueSome matched
      | _ -> checkFiberChain(directChildField.GetValue(cellObj))

  let rec visit(node: obj) : 'TCell voption =
    if isNull node then
      ValueNone
    else
      let nt = node.GetType()
      let fiber = nt.GetField("<Fiber>k__BackingField", flags).GetValue(node)

      match checkFiberChain fiber with
      | ValueSome matched -> ValueSome matched
      | ValueNone ->
        let children =
          nt.GetField("<Children>k__BackingField", flags).GetValue(node)
          :?> IList

        let mutable found = ValueNone

        if not(isNull children) then
          for i = 0 to children.Count - 1 do
            if found.IsNone then
              found <- visit children[i]

        found

  visit rootNode

let private findKeyBinding(bindings: KeyBinding array, key: Key) : KeyBinding =
  bindings
  |> Array.find(fun kb -> kb.Key = key && kb.Modifiers = KeyModifiers.None)

let private invokeBuildRaw<'TInput>(cell: Cell<'TInput>) : Blob =
  let input = typeof<Cell<'TInput>>.GetProperty("Input", flags).GetValue(cell)

  let buildMethod =
    cell.GetType().GetMethod("Build", flags, null, [| typeof<'TInput> |], null)

  buildMethod.Invoke(cell, [| input |]) :?> Blob

let private darkConfig = {
  MetroConfig.defaultConfig with
      Mode = MetroMode.Dark
      Accent = MetroAccentKind.Blue
}

[<Fact>]
let ``MetroPivot handles selection keyboard loop clamp swipe and reduced motion``
  ()
  =
  let items = [|
    { Id = "one"; Header = "overview" }
    { Id = "two"; Header = "details" }
    { Id = "three"; Header = "history" }
  |]

  let mutable selected = "one"
  let mutable loopMode = true
  let mutable lastRenderedId = ""

  // The test keeps the mounted cell: no search of the retained tree.
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let pivotFactory =
    Func<MetroPivotCell>(fun () ->
      // The handler writes the state only. The Pivot gives each selection to
      // the host cell, so the owner rule rebuilds the host.
      let cell =
        MetroPivot.create(
          (fun nextId -> selected <- nextId),
          fun id ->
            lastRenderedId <- id
            MetroText.body $"Panel-{id}"
        )

      pivotRef <- ValueSome cell
      cell)

  use host = createHost(800, 600)

  let rootCell =
    CaptureCell.create(fun () ->
      MetroPivot.mount(
        "main-pivot",
        {
          MetroPivot.defaults(items, selected) with
              Title = ValueSome "METRO APP"
              Loop = loopMode
        },
        pivotFactory
      ))

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pivotCell =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

  Assert.Equal("one", lastRenderedId)

  // Inspect built tree structure: title, header strip (all keyed), content.
  let mutable builtBlob: Blob = null
  rootCell.DuringBuild(host, fun () -> builtBlob <- pivotCell.View)
  let built = builtBlob :?> Container
  Assert.Equal(3, childCount built)
  let titleText = childAt(built, 0) :?> Text
  Assert.Equal("METRO APP", titleText.Content)

  let headerStrip = childAt(built, 1) :?> Container
  Assert.Equal(AccessibilityRole.TabList, headerStrip.Accessibility.Role)
  Assert.Equal(3, childCount headerStrip)

  let tab0 = childAt(headerStrip, 0) :?> Button
  let tab1 = childAt(headerStrip, 1) :?> Button
  let tab2 = childAt(headerStrip, 2) :?> Button
  Assert.Equal(AccessibilityRole.Tab, tab0.Accessibility.Role)
  Assert.Equal(Nullable true, tab0.Accessibility.Selected)
  Assert.Equal(Nullable false, tab1.Accessibility.Selected)
  Assert.Equal("pivot-tab-one", tab0.Key)
  Assert.Equal("pivot-tab-two", tab1.Key)
  // Style values are write-only. The rendered frame of the gallery covers
  // the colors and the font of the tabs.

  let contentPanel = childAt(built, 2) :?> Container
  Assert.Equal(AccessibilityRole.TabPanel, contentPanel.Accessibility.Role)

  // A tab click selects the item and starts the entrance animation. The
  // handler has no Rebuild call: the owner rule rebuilds the host.
  let buildsBefore = rootCell.Builds
  tab1.OnClick.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("two", selected)
  Assert.Equal("two", lastRenderedId)
  Assert.True(rootCell.Builds > buildsBefore)
  Assert.True(pivotCell.ContentTranslate.Running)
  Assert.True(pivotCell.ContentOpacity.Running)
  // A headless test cannot advance an animation, so it asserts the start.

  // Keyboard navigation with Loop = true wraps across boundaries.
  let rightBinding = findKeyBinding(built.KeyBindings, Key.Right)
  let leftBinding = findKeyBinding(built.KeyBindings, Key.Left)
  let homeBinding = findKeyBinding(built.KeyBindings, Key.Home)
  let endBinding = findKeyBinding(built.KeyBindings, Key.End)

  rightBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("three", selected)

  rightBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  leftBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("three", selected)

  homeBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  endBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("three", selected)

  // With Loop = false, navigation clamps at both ends.
  loopMode <- false
  rootCell.Rebuild()
  pumpFrames(host, 1)
  Assert.Equal("three", selected)

  rightBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("three", selected)

  homeBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  leftBinding.OnPress.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  // Horizontal swipe gesture (>= 48px) navigates; short or vertical does not.
  let makePointerEvent(x: float, y: float) : PointerEvent =
    PointerEvent(IsPrimary = true, Position = Point(X = x, Y = y))

  let swipe(x0: float, y0: float, x1: float, y1: float) =
    contentPanel.OnPointerDown.Invoke(makePointerEvent(x0, y0))

    contentPanel.OnPointerMove.Invoke(
      makePointerEvent((x0 + x1) * 0.5, (y0 + y1) * 0.5)
    )

    contentPanel.OnPointerUp.Invoke(makePointerEvent(x1, y1))

  // Short swipe ignored.
  swipe(200.0, 100.0, 170.0, 102.0)
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  // Vertical swipe ignored.
  swipe(200.0, 100.0, 140.0, 220.0)
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  // Left swipe advances to "two".
  swipe(240.0, 100.0, 160.0, 104.0)
  pumpFrames(host, 1)
  Assert.Equal("two", selected)

  // Right swipe returns to "one".
  swipe(120.0, 100.0, 200.0, 102.0)
  pumpFrames(host, 1)
  Assert.Equal("one", selected)

  // ReducedMotion: no animation starts.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true
    )
  )

  pumpFrames(host, 1)
  tab2.OnClick.Invoke()
  pumpFrames(host, 1)
  Assert.Equal("three", selected)
  Assert.False(pivotCell.ContentTranslate.Running)
  Assert.False(pivotCell.ContentOpacity.Running)
  Assert.Equal(0.0, pivotCell.ContentTranslate.Value.X, 3)
  Assert.Equal(1.0, pivotCell.ContentOpacity.Value, 3)

[<Fact>]
let ``A host of the Pivot rebuilds through the owner rule``() =
  let items = [|
    { Id = "one"; Header = "one" }
    { Id = "two"; Header = "two" }
  |]

  let mutable selected = "one"
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let pivotFactory =
    Func<MetroPivotCell>(fun () ->
      // The handler writes the state only. It has no Rebuild call.
      let cell =
        MetroPivot.create(
          (fun nextId -> selected <- nextId),
          fun id -> MetroText.body id
        )

      pivotRef <- ValueSome cell
      cell)

  use host = createHost(640, 480)

  // The host cell counts its builds.
  let rootCell =
    CaptureCell.create(fun () ->
      MetroPivot.mount(
        "pivot",
        MetroPivot.defaults(items, selected),
        pivotFactory
      ))

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pivotCell =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

  let mutable tree: Blob = null
  rootCell.DuringBuild(host, fun () -> tree <- pivotCell.View)

  let tab =
    match findBlobByKey(tree, "pivot-tab-two") with
    | ValueSome blob -> blob :?> Button
    | ValueNone -> failwith "Expected the tab button of 'two'."

  let buildsBefore = rootCell.Builds
  tab.OnClick.Invoke()
  pumpFrames(host, 2)

  Assert.Equal("two", selected)
  Assert.Equal(buildsBefore + 1, rootCell.Builds)

[<Fact>]
let ``MetroHub drives header parallax without cell rebuilds and honors reduced motion``
  ()
  =
  let sections = [|
    {
      Id = "start"
      Header = "start"
      Width = 360.0
    }
    {
      Id = "recent"
      Header = "recent"
      Width = 360.0
    }
    {
      Id = "tools"
      Header = "tools"
      Width = 360.0
    }
  |]

  let mutable renderCalls = 0
  let mutable mountHub = true

  // The test keeps the mounted cell: no search of the retained tree.
  let mutable hubRef: MetroHubCell voption = ValueNone

  let hubFactory =
    Func<MetroHubCell>(fun () ->
      let cell =
        MetroHub.create(fun id ->
          renderCalls <- renderCalls + 1
          MetroText.body $"Section {id}")

      hubRef <- ValueSome cell
      cell)

  use host = createHost(600, 480)

  let rootCell = {
    new Cell() with
      override _.Build() : Blob =
        if mountHub then
          MetroHub.mount(
            "hub",
            {
              MetroHub.defaults("workspace", sections) with
                  ParallaxFactor = 0.4
                  SectionGap = 32.0
            },
            hubFactory
          )
        else
          Container()
  }

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let hubCell =
    match hubRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroHubCell to be mounted."

  Assert.True(renderCalls >= 3)
  Assert.True(hubCell.ScrollHandle.ScrollRange.X > 0.0)
  Assert.Equal(0.0, hubCell.HeaderTranslate.Value.X, 3)

  // Scrolling updates header parallax directly with ZERO cell rebuilds.
  let callsBeforeScroll = renderCalls
  let jumped = hubCell.ScrollHandle.JumpTo(Nullable 200.0, Nullable())
  Assert.True(jumped)
  pumpFrames(host, 2)
  Assert.Equal(-80.0, hubCell.HeaderTranslate.Value.X, 2)
  Assert.Equal(callsBeforeScroll, renderCalls)

  // ReducedMotion zeroes parallax immediately and on subsequent scroll.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 1)
  Assert.Equal(0.0, hubCell.HeaderTranslate.Value.X, 3)

  hubCell.ScrollHandle.JumpTo(Nullable 120.0, Nullable()) |> ignore
  pumpFrames(host, 1)
  Assert.Equal(0.0, hubCell.HeaderTranslate.Value.X, 3)

  // Unmounting unsubscribes MetricsChanged cleanly and resets translate.
  mountHub <- false
  rootCell.Rebuild()
  pumpFrames(host, 1)
  Assert.Equal(0.0, hubCell.HeaderTranslate.Value.X, 3)

[<Fact>]
let ``TileGridLayout packs mixed tiles without overlap and exact gutters``() =
  Assert.Equal(struct (1, 1), MetroTileMetrics.cellSpans MetroTileSize.Small)
  Assert.Equal(struct (2, 2), MetroTileMetrics.cellSpans MetroTileSize.Medium)
  Assert.Equal(struct (4, 2), MetroTileMetrics.cellSpans MetroTileSize.Wide)
  Assert.Equal(struct (4, 4), MetroTileMetrics.cellSpans MetroTileSize.Large)

  Assert.Equal(
    struct (72.0, 72.0),
    MetroTileMetrics.pixelSize(72.0, 8.0, MetroTileSize.Small)
  )

  Assert.Equal(
    struct (152.0, 152.0),
    MetroTileMetrics.pixelSize(72.0, 8.0, MetroTileSize.Medium)
  )

  Assert.Equal(
    struct (312.0, 152.0),
    MetroTileMetrics.pixelSize(72.0, 8.0, MetroTileSize.Wide)
  )

  Assert.Equal(
    struct (312.0, 312.0),
    MetroTileMetrics.pixelSize(72.0, 8.0, MetroTileSize.Large)
  )

  let sizes = [|
    MetroTileSize.Wide
    MetroTileSize.Small
    MetroTileSize.Small
    MetroTileSize.Medium
    MetroTileSize.Large
    MetroTileSize.Small
  |]

  // 7th child exceeds sizes.Length and defaults to Medium.
  let expectedSizes = Array.append sizes [| MetroTileSize.Medium |]
  let handles = Array.init expectedSizes.Length (fun _ -> ElementHandle())

  let layout =
    TileGridLayout.create {
      CellSize = 72.0
      Gap = 8.0
      Columns = 4
      Sizes = sizes
    }

  use host = createHost(800, 900)

  let rootCell = {
    new Cell() with
      override _.Build() : Blob =
        let grid = Container(Layout = layout)

        for i = 0 to handles.Length - 1 do
          grid.Add(Container(Handle = handles[i]))

        grid
  }

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let boxes = handles |> Array.map(fun h -> h.BorderBox)

  for i = 0 to boxes.Length - 1 do
    let b = boxes[i]

    let struct (ew, eh) =
      MetroTileMetrics.pixelSize(72.0, 8.0, expectedSizes[i])

    Assert.True(Double.IsFinite b.X && b.X >= 0.0)
    Assert.True(Double.IsFinite b.Y && b.Y >= 0.0)
    Assert.Equal(ew, b.Width, 2)
    Assert.Equal(eh, b.Height, 2)

  // Verify zero overlap across all pairs.
  for i = 0 to boxes.Length - 1 do
    for j = i + 1 to boxes.Length - 1 do
      let a = boxes[i]
      let b = boxes[j]

      let separated =
        a.X + a.Width <= b.X + 0.01
        || b.X + b.Width <= a.X + 0.01
        || a.Y + a.Height <= b.Y + 0.01
        || b.Y + b.Height <= a.Y + 0.01

      Assert.True(separated, $"Tiles {i} and {j} overlap.")

  // Verify exact 8px gutters on known placements.
  Assert.Equal(0.0, boxes[0].X, 2)
  Assert.Equal(0.0, boxes[0].Y, 2)
  Assert.Equal(0.0, boxes[1].X, 2)
  Assert.Equal(160.0, boxes[1].Y, 2)
  Assert.Equal(8.0, boxes[1].Y - (boxes[0].Y + boxes[0].Height), 2)
  Assert.Equal(80.0, boxes[2].X, 2)
  Assert.Equal(8.0, boxes[2].X - (boxes[1].X + boxes[1].Width), 2)
  Assert.Equal(160.0, boxes[3].X, 2)
  Assert.Equal(8.0, boxes[3].X - (boxes[2].X + boxes[2].Width), 2)

[<Fact>]
let ``MetroIconicTile and MetroLiveTile enforce Metro visuals cycling and cleanup``
  ()
  =
  let mutable iconicClicks = 0
  let mutable liveClicks = 0
  let mutable mountLive = true
  let customColor = Color.Rgb(250, 104, 0)

  let faces = [|
    {
      Title = "Weather"
      Subtitle = ValueSome "Mostly sunny"
      Metric = ValueSome "72°"
      AccentOverride = ValueNone
    }
    {
      Title = "Weather"
      Subtitle = ValueSome "Evening breeze"
      Metric = ValueSome "64°"
      AccentOverride = ValueSome customColor
    }
  |]

  let liveFactory =
    MetroLiveTile.createFactory(fun () -> liveClicks <- liveClicks + 1)

  use host = createHost(800, 600)

  let capture =
    CaptureCell.create(fun () ->
      let root = Container()

      root.Add(
        MetroIconicTile.build(
          {
            MetroIconicTile.defaults("Mail", "mail") with
                Count = ValueSome 14
                AccentOverride = ValueSome customColor
          },
          fun () -> iconicClicks <- iconicClicks + 1
        )
      )

      if mountLive then
        root.Add(
          Container()
            .Children(
              MetroLiveTile.mount(
                "live-weather",
                {
                  MetroLiveTile.defaults faces with
                      IntervalMs = 200.0
                },
                liveFactory
              )
            )
        )

      root)

  let window = Window(Root = capture)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // Verify iconic tile visuals and interaction.
  let root = capture.Captured :?> Container
  let iconicBtn = childAt(root, 0) :?> Button
  Assert.Equal(0.0f, requireEntry(iconicBtn, "BorderRadius").A)
  Assert.Equal(0, boxShadowCount iconicBtn)
  assertColorEqual(customColor, requireEntry(iconicBtn, "BackgroundColor"))

  Assert.Equal(
    0.97f,
    requireEntry(iconicBtn.ActiveStyle, "TransformScale").A,
    3
  )

  Assert.Equal(AccessibilityRole.Button, iconicBtn.Accessibility.Role)
  Assert.Equal("Mail, 14", iconicBtn.Accessibility.Name)
  iconicBtn.OnClick.Invoke()
  iconicBtn.KeyBindings[0].OnPress.Invoke()
  Assert.Equal(2, iconicClicks)

  // Verify live tile mounting, active timer, face content, and click.
  let liveCell =
    match findMountedCell<MetroLiveTileCell> window with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroLiveTileCell to be mounted."

  Assert.Equal(0, liveCell.ActiveFaceIndex)
  Assert.True(liveCell.IsTimerActive)
  let mutable liveBlob: Blob = null
  capture.DuringBuild(host, fun () -> liveBlob <- invokeBuildRaw liveCell)
  let liveBtn = liveBlob :?> Button
  Assert.Equal(0.0f, requireEntry(liveBtn, "BorderRadius").A)
  Assert.Equal(0, boxShadowCount liveBtn)
  Assert.Equal(AccessibilityRole.Button, liveBtn.Accessibility.Role)
  liveBtn.OnClick.Invoke()
  Assert.Equal(1, liveClicks)

  // ReducedMotion disables timer cycling immediately.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable true,
      HighContrast = Nullable true
    )
  )

  pumpFrames(host, 2)
  Assert.False(liveCell.IsTimerActive)

  let hcIconic = childAt(capture.Captured :?> Container, 0) :?> Button
  Assert.Equal(2.0f, requireEntry(hcIconic, "BorderTopWidth").A)
  Assert.True((tryLastEntry(hcIconic.ActiveStyle, "TransformScale")).IsNone)

  // Restoring standard motion resumes timer; unmounting cancels it cleanly.
  host.UpdatePreferences(
    PlatformPreferences(
      Theme = SystemTheme.Dark,
      ReducedMotion = Nullable false
    )
  )

  pumpFrames(host, 2)
  Assert.True(liveCell.IsTimerActive)

  mountLive <- false
  capture.Rebuild()
  pumpFrames(host, 1)
  Assert.False(liveCell.IsTimerActive)

[<Fact>]
let ``MetroAppBar MetroSettingsFlyout and MetroContentDialog wire commands portals and dismiss``
  ()
  =
  let mutable lastCommand = ""
  let mutable toggled = 0
  let mutable expanded = false
  let mutable flyoutOpen = false
  let mutable flyoutDismissed = 0
  let mutable dialogOpen = false
  let mutable dialogDismissible = true
  let mutable primaryClicks = 0
  let mutable secondaryClicks = 0
  let mutable dialogDismissed = 0

  let primaryCmds = [|
    {
      Id = "theme"
      Label = "Theme"
      IconName = "brightness_6"
      Disabled = false
    }
    {
      Id = "settings"
      Label = "Settings"
      IconName = "settings"
      Disabled = false
    }
  |]

  let secondaryCmds = [|
    {
      Id = "about"
      Label = "About Metro"
      IconName = "info"
      Disabled = false
    }
  |]

  use host = createHost(800, 600)

  let capture =
    CaptureCell.create(fun () ->
      Container(Width = Length.Percent 100.0, Height = Length.Percent 100.0)
        .Children(
          MetroAppBar.build(
            {
              MetroAppBar.defaults primaryCmds with
                  Secondary = secondaryCmds
                  Expanded = expanded
            },
            (fun id -> lastCommand <- id),
            fun () -> toggled <- toggled + 1
          ),
          MetroSettingsFlyout.build(
            MetroSettingsFlyout.defaults("Settings", flyoutOpen),
            (fun () -> flyoutDismissed <- flyoutDismissed + 1),
            fun () -> MetroText.body "Flyout body"
          ),
          MetroContentDialog.build(
            {
              MetroContentDialog.defaults("Confirm Action", dialogOpen) with
                  PrimaryText = ValueSome "Proceed"
                  SecondaryText = ValueSome "Cancel"
                  Dismissible = dialogDismissible
            },
            (fun () -> primaryClicks <- primaryClicks + 1),
            (fun () -> secondaryClicks <- secondaryClicks + 1),
            (fun () -> dialogDismissed <- dialogDismissed + 1),
            fun () -> MetroText.body "Dialog body"
          )
        ))

  let window = Window(Root = capture)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // 1. AppBar collapsed: primary commands + ellipsis toggle; overlays closed.
  let root0 = capture.Captured :?> Container
  let bar0 = childAt(root0, 0) :?> Container
  Assert.Equal(AccessibilityRole.Toolbar, bar0.Accessibility.Role)
  Assert.Equal(1, childCount bar0)

  let primaryRow = childAt(bar0, 0) :?> Container
  let primaryStrip = childAt(primaryRow, 0) :?> Container
  let moreBtn = childAt(primaryRow, 1) :?> Button
  let firstCmdBtn = childAt(primaryStrip, 0) :?> Button
  let circle = childAt(firstCmdBtn, 0) :?> Container

  Assert.Equal(40.0f, requireEntry(circle, "Width").A)
  Assert.Equal(40.0f, requireEntry(circle, "Height").A)
  Assert.Equal(20.0f, requireEntry(circle, "BorderRadius").A)
  Assert.Equal(2.0f, requireEntry(circle, "BorderTopWidth").A)

  firstCmdBtn.OnClick.Invoke()
  Assert.Equal("theme", lastCommand)
  moreBtn.OnClick.Invoke()
  Assert.Equal(1, toggled)

  // Closed overlays render Display.None containers.
  let closedFlyout = childAt(root0, 1) :?> Container
  let closedDialog = childAt(root0, 2) :?> Container

  Assert.Equal(
    float32(int Display.None),
    requireEntry(closedFlyout, "Display").A
  )

  Assert.Equal(
    float32(int Display.None),
    requireEntry(closedDialog, "Display").A
  )

  // 2. Expand AppBar + open Flyout + open Dialog.
  expanded <- true
  flyoutOpen <- true
  dialogOpen <- true
  capture.Rebuild()
  pumpFrames(host, 2)

  let root1 = capture.Captured :?> Container
  let bar1 = childAt(root1, 0) :?> Container
  Assert.Equal(2, childCount bar1)
  let secList = childAt(bar1, 1) :?> Container
  let secCmd = childAt(secList, 0) :?> Button
  secCmd.OnClick.Invoke()
  Assert.Equal("about", lastCommand)

  // SettingsFlyout: Portal + modal FocusScope + 346px right-docked panel.
  let flyoutPortal = childAt(root1, 1) :?> Portal
  Assert.NotNull(flyoutPortal.FocusScope)
  Assert.True(flyoutPortal.FocusScope.Modal)
  Assert.True(flyoutPortal.Scrim.HasValue)
  Assert.NotNull(flyoutPortal.OnDismiss)

  let flyoutPanel = childAt(flyoutPortal, 0) :?> Container
  Assert.Equal(346.0f, requireEntry(flyoutPanel, "Width").A)
  Assert.Equal(0.0f, requireEntry(flyoutPanel, "BorderRadius").A)
  Assert.Equal(0, boxShadowCount flyoutPanel)
  Assert.Equal(AccessibilityRole.Dialog, flyoutPanel.Accessibility.Role)

  let headerBand = childAt(flyoutPanel, 0) :?> Container
  let backBtn = childAt(headerBand, 0) :?> Button
  backBtn.OnClick.Invoke()
  flyoutPortal.OnDismiss.Invoke(PopupDismissReason.Escape)
  Assert.Equal(2, flyoutDismissed)

  // ContentDialog: Portal + modal FocusScope + primary/secondary/dismiss.
  let dialogPortal = childAt(root1, 2) :?> Portal
  Assert.NotNull(dialogPortal.FocusScope)
  Assert.True(dialogPortal.FocusScope.Modal)
  Assert.True(dialogPortal.Scrim.HasValue)
  Assert.NotNull(dialogPortal.OnDismiss)

  let dialogBand = childAt(dialogPortal, 0) :?> Container
  Assert.Equal(0.0f, requireEntry(dialogBand, "BorderRadius").A)
  Assert.Equal(2.0f, requireEntry(dialogBand, "BorderTopWidth").A)
  Assert.Equal(2.0f, requireEntry(dialogBand, "BorderBottomWidth").A)
  Assert.Equal(0, boxShadowCount dialogBand)
  Assert.Equal(AccessibilityRole.Dialog, dialogBand.Accessibility.Role)

  let innerContent = childAt(dialogBand, 0) :?> Container
  let actionsRow = childAt(innerContent, 2) :?> Container
  let primaryBtn = childAt(actionsRow, 0) :?> Button
  let secondaryBtn = childAt(actionsRow, 1) :?> Button
  primaryBtn.OnClick.Invoke()
  secondaryBtn.OnClick.Invoke()
  dialogPortal.OnDismiss.Invoke(PopupDismissReason.OutsideInteraction)
  Assert.Equal(1, primaryClicks)
  Assert.Equal(1, secondaryClicks)
  Assert.Equal(1, dialogDismissed)

  // Non-dismissible dialog omits Portal.OnDismiss.
  dialogDismissible <- false
  capture.Rebuild()
  pumpFrames(host, 1)
  let nonDismissPortal = childAt(capture.Captured :?> Container, 2) :?> Portal
  Assert.Null(nonDismissPortal.OnDismiss)

  // HighContrast enforces 2px Foreground borders on AppBar, Flyout, and Dialog.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, HighContrast = Nullable true)
  )

  pumpFrames(host, 2)
  let hcRoot = capture.Captured :?> Container
  let hcBar = childAt(hcRoot, 0) :?> Container
  let hcFlyoutPanel = childAt(childAt(hcRoot, 1) :?> Portal, 0) :?> Container
  let hcDialogBand = childAt(childAt(hcRoot, 2) :?> Portal, 0) :?> Container
  Assert.Equal(2.0f, requireEntry(hcBar, "BorderTopWidth").A)

  assertColorEqual(
    MetroPalette.dark.Foreground,
    requireEntry(hcBar, "BorderTopColor")
  )

  Assert.Equal(2.0f, requireEntry(hcFlyoutPanel, "BorderLeftWidth").A)

  assertColorEqual(
    MetroPalette.dark.Foreground,
    requireEntry(hcDialogBand, "BorderTopColor")
  )

[<Fact>]
let ``MetroPivot Unmounting snaps animations and resets selection state``() =
  let items = [|
    { Id = "a"; Header = "alpha" }
    { Id = "b"; Header = "beta" }
  |]

  let mutable selected = "a"
  let mutable mountPivot = true

  // The test keeps the mounted cell: no search of the retained tree.
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let factory =
    Func<MetroPivotCell>(fun () ->
      let cell =
        MetroPivot.create(
          (fun nextId -> selected <- nextId),
          fun id -> MetroText.body id
        )

      pivotRef <- ValueSome cell
      cell)

  use host = createHost(640, 480)

  let rootCell = {
    new Cell() with
      override _.Build() : Blob =
        if mountPivot then
          MetroPivot.mount("p", MetroPivot.defaults(items, selected), factory)
        else
          Container()
  }

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let pivotCell =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

  // The host changes the selection and rebuilds itself.
  selected <- "b"
  rootCell.Rebuild()
  pumpFrames(host, 1)
  Assert.True(pivotCell.ContentTranslate.Running)
  Assert.True(pivotCell.ContentOpacity.Running)

  mountPivot <- false
  rootCell.Rebuild()
  pumpFrames(host, 1)
  Assert.False(pivotCell.ContentTranslate.Running)
  Assert.False(pivotCell.ContentOpacity.Running)
  Assert.Equal(0.0, pivotCell.ContentTranslate.Value.X, 3)
  Assert.Equal(1.0, pivotCell.ContentOpacity.Value, 3)

[<Fact>]
let ``TileGridLayout handles empty children and clamped column counts``() =
  let emptyHandle = ElementHandle()

  let emptyLayout =
    TileGridLayout.create {
      CellSize = 72.0
      Gap = 8.0
      Columns = 4
      Sizes = Array.empty
    }

  // A column count below 1 gives 4.
  let fallback =
    TileGridLayout.resolve {
      CellSize = 72.0
      Gap = 8.0
      Columns = 0
      Sizes = Array.empty
    }

  Assert.Equal(4, fallback.Columns)

  let narrowSizes = [|
    MetroTileSize.Wide
    MetroTileSize.Large
    MetroTileSize.Small
    MetroTileSize.Medium
  |]

  let narrowHandles = Array.init narrowSizes.Length (fun _ -> ElementHandle())

  let narrowArgs: TileGridArgs = {
    CellSize = 72.0
    Gap = 8.0
    Columns = 2
    Sizes = narrowSizes
  }

  Assert.Equal(2, (TileGridLayout.resolve narrowArgs).Columns)
  let narrowLayout = TileGridLayout.create narrowArgs

  use host = createHost(800, 900)

  let rootCell = {
    new Cell() with
      override _.Build() : Blob =
        let narrowGrid = Container(Layout = narrowLayout)

        for i = 0 to narrowHandles.Length - 1 do
          narrowGrid.Add(Container(Handle = narrowHandles[i]))

        Container(
          FlexDirection = FlexDirection.Column,
          AlignItems = AlignItems.FlexStart
        )
          .Children(
            Container(Handle = emptyHandle, Layout = emptyLayout),
            narrowGrid
          )
  }

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  Assert.Equal(0.0, emptyHandle.BorderBox.Width, 2)
  Assert.Equal(0.0, emptyHandle.BorderBox.Height, 2)

  let boxes = narrowHandles |> Array.map(fun h -> h.BorderBox)
  let maxAllowedWidth = 2.0 * 72.0 + 8.0

  for b in boxes do
    Assert.True(Double.IsFinite b.X && b.X >= 0.0)
    Assert.True(Double.IsFinite b.Y && b.Y >= 0.0)
    Assert.True(b.Width > 0.0 && b.Width <= maxAllowedWidth + 0.01)
    Assert.True(b.Height > 0.0)

  for i = 0 to boxes.Length - 1 do
    for j = i + 1 to boxes.Length - 1 do
      let a = boxes[i]
      let b = boxes[j]

      let separated =
        a.X + a.Width <= b.X + 0.01
        || b.X + b.Width <= a.X + 0.01
        || a.Y + a.Height <= b.Y + 0.01
        || b.Y + b.Height <= a.Y + 0.01

      Assert.True(separated, $"Clamped tiles {i} and {j} overlap.")

[<Fact>]
let ``MetroAppBar disabled commands avoid double-dimming and honor HighContrast``
  ()
  =
  let primary = [|
    {
      Id = "disabled-primary"
      Label = "Save"
      IconName = "save"
      Disabled = true
    }
  |]

  let secondary = [|
    {
      Id = "disabled-secondary"
      Label = "Export"
      IconName = "download"
      Disabled = true
    }
  |]

  use host = createHost(800, 600)

  let capture =
    CaptureCell.create(fun () ->
      MetroAppBar.build(
        {
          MetroAppBar.defaults primary with
              Secondary = secondary
              Expanded = true
        },
        ignore,
        ignore
      ))

  let window = Window(Root = capture)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let bar = capture.Captured :?> Container
  let primaryStrip = childAt(childAt(bar, 0), 0) :?> Container
  let pBtn = childAt(primaryStrip, 0) :?> Button
  let sBtn = childAt(childAt(bar, 1), 0) :?> Button
  let p = MetroPalette.dark

  Assert.True(pBtn.Disabled)
  Assert.Null(pBtn.OnClick)
  Assert.Null(pBtn.KeyBindings)
  assertColorEqual(p.Foreground, requireEntry(pBtn, "Color"))
  assertColorEqual(p.Foreground, requireEntry(pBtn.DisabledStyle, "Color"))
  Assert.Equal(0.4f, requireEntry(pBtn.DisabledStyle, "Opacity").A, 3)

  Assert.True(sBtn.Disabled)
  Assert.Null(sBtn.OnClick)
  Assert.Null(sBtn.KeyBindings)
  assertColorEqual(p.Foreground, requireEntry(sBtn, "Color"))
  assertColorEqual(p.Foreground, requireEntry(sBtn.DisabledStyle, "Color"))
  Assert.Equal(0.4f, requireEntry(sBtn.DisabledStyle, "Opacity").A, 3)

  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, HighContrast = Nullable true)
  )

  pumpFrames(host, 2)
  let hcBar = capture.Captured :?> Container
  let hcPBtn = childAt(childAt(childAt(hcBar, 0), 0), 0) :?> Button
  let hcSBtn = childAt(childAt(hcBar, 1), 0) :?> Button
  assertColorEqual(p.Foreground, requireEntry(hcPBtn, "Color"))
  Assert.Equal(1.0f, requireEntry(hcPBtn.DisabledStyle, "Opacity").A, 3)
  assertColorEqual(p.Foreground, requireEntry(hcSBtn, "Color"))
  Assert.Equal(1.0f, requireEntry(hcSBtn.DisabledStyle, "Opacity").A, 3)

[<Fact>]
let ``MetroHub and MetroLiveTile respect scoped ReducedMotion``() =
  let sections = [|
    {
      Id = "a"
      Header = "section a"
      Width = 420.0
    }
    {
      Id = "b"
      Header = "section b"
      Width = 420.0
    }
  |]

  let faces = [|
    {
      Title = "One"
      Subtitle = ValueNone
      Metric = ValueSome "1"
      AccentOverride = ValueNone
    }
    {
      Title = "Two"
      Subtitle = ValueNone
      Metric = ValueSome "2"
      AccentOverride = ValueNone
    }
  |]

  // The test keeps the mounted hub cell: no search of the retained tree.
  let mutable hubRef: MetroHubCell voption = ValueNone

  let hubFactory =
    Func<MetroHubCell>(fun () ->
      let cell = MetroHub.create(fun id -> MetroText.body id)
      hubRef <- ValueSome cell
      cell)

  let liveFactory = MetroLiveTile.createFactory ignore

  use host = createHost(600, 480)

  let rootCell = {
    new Cell() with
      override _.Build() : Blob =
        let baseTheme = Metro.read()
        let scopedTheme = { baseTheme with ReducedMotion = true }

        Metro.scope(
          scopedTheme,
          fun () ->
            Container(FlexDirection = FlexDirection.Column)
              .Children(
                Container(Height = Length.Pixels 240.0)
                  .Children(
                    MetroHub.mount(
                      "scoped-hub",
                      MetroHub.defaults("scoped", sections),
                      hubFactory
                    )
                  ),
                Container()
                  .Children(
                    MetroLiveTile.mount(
                      "scoped-live",
                      {
                        MetroLiveTile.defaults faces with
                            IntervalMs = 200.0
                      },
                      liveFactory
                    )
                  )
              )
        )
  }

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let hubCell =
    match hubRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected scoped MetroHubCell."

  let liveCell =
    match findMountedCell<MetroLiveTileCell> window with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected scoped MetroLiveTileCell."

  Assert.False(liveCell.IsTimerActive)
  hubCell.ScrollHandle.JumpTo(Nullable 150.0, Nullable()) |> ignore
  pumpFrames(host, 2)
  Assert.Equal(0.0, hubCell.HeaderTranslate.Value.X, 3)

[<Fact>]
let ``MetroPivot remounts child cells cleanly across repeated tab visits``() =
  use host = HeadlessWindowHost.create()
  host.Resize(900, 600, 900, 600)

  let mutable mountCount = 0
  let mutable selectedId = "first"

  let items = [|
    { Id = "first"; Header = "first" }
    { Id = "second"; Header = "second" }
  |]

  let childFactory =
    Func<Cell>(fun () ->
      mountCount <- mountCount + 1
      let instanceNumber = mountCount

      {
        new Cell() with
          override _.Build() : Blob =
            MetroText.body $"Mounted instance {instanceNumber}"
      })

  // The test keeps the mounted cell: no search of the retained tree.
  let mutable pivotRef: MetroPivotCell voption = ValueNone

  let pivotFactory =
    Func<MetroPivotCell>(fun () ->
      let cell =
        MetroPivot.create(
          (fun id -> selectedId <- id),
          fun id ->
            match id with
            | "first" -> Cell.Mount<Cell>("first-child", childFactory)
            | _ -> MetroText.body "Second tab"
        )

      pivotRef <- ValueSome cell
      cell)

  let rootCell =
    CaptureCell.create(fun () ->
      MetroPivot.mount(
        "revisit-pivot",
        MetroPivot.defaults(items, selectedId),
        pivotFactory
      ))

  let window = Window(Root = rootCell)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)
  Assert.Equal(1, mountCount)

  let pivotCell =
    match pivotRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroPivotCell to be mounted."

  // Visit second tab, then return to first tab twice, through the tab
  // buttons of the tree. The handler has no Rebuild call.
  for target in [| "second"; "first"; "second"; "first" |] do
    let mutable tree: Blob = null
    rootCell.DuringBuild(host, fun () -> tree <- pivotCell.View)

    let tab =
      match findBlobByKey(tree, $"pivot-tab-{target}") with
      | ValueSome blob -> blob :?> Button
      | ValueNone -> failwith $"Expected the tab button of '{target}'."

    tab.OnClick.Invoke()
    pumpFrames(host, 2)
    Assert.Equal(target, selectedId)

  Assert.Equal(3, mountCount)
