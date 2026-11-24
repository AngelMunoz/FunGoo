module FunGoo.Metro.Tests.TiltAndAnimatedOverlaysTests

open System
open System.IO
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

let darkConfig = {
  MetroConfig.defaultConfig with
      Mode = MetroMode.Dark
      Accent = MetroAccentKind.Blue
}

let writeMinimalEffectBytes() : byte array =
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

let createSyntheticEffect() : ShaderEffect =
  ShaderEffect(ShaderEffectProgram(writeMinimalEffectBytes()))

let pointerAt(x: float, y: float) : PointerEvent =
  PointerEvent(IsPrimary = true, Position = Point(X = x, Y = y))

[<Fact>]
let ``MetroTilt normalizedOffset and fallbackOffset clamp and handle empty bounds``
  ()
  =
  let box = ElementRect(X = 0.0, Y = 0.0, Width = 200.0, Height = 100.0)

  let struct (cx, cy) =
    MetroTilt.normalizedOffset(Point(X = 100.0, Y = 50.0), box)

  Assert.Equal(0.0, cx, 6)
  Assert.Equal(0.0, cy, 6)

  let struct (tlx, tly) =
    MetroTilt.normalizedOffset(Point(X = 0.0, Y = 0.0), box)

  Assert.Equal(-1.0, tlx, 6)
  Assert.Equal(-1.0, tly, 6)

  let struct (brx, bry) =
    MetroTilt.normalizedOffset(Point(X = 250.0, Y = 150.0), box)

  Assert.Equal(1.0, brx, 6)
  Assert.Equal(1.0, bry, 6)

  let empty = ElementRect(X = 0.0, Y = 0.0, Width = 0.0, Height = 0.0)

  let struct (ex, ey) =
    MetroTilt.normalizedOffset(Point(X = 50.0, Y = 50.0), empty)

  Assert.Equal(0.0, ex, 6)
  Assert.Equal(0.0, ey, 6)

  let offset = MetroTilt.fallbackOffset(0.5, -1.0, 6.0)
  Assert.Equal(3.0, offset.X, 6)
  Assert.Equal(-6.0, offset.Y, 6)

[<Fact>]
let ``Tilt and dialog transition simulations run for their full Metro durations``
  ()
  =
  let assertDuration
    (spec: Func<float, float, float, Simulation>, expectedMs: float)
    =
    let sim = spec.Invoke(0.0, 1.0, 0.0)
    let halfSec = (expectedMs * 0.5) / 1000.0
    let fullSec = expectedMs / 1000.0
    Assert.False(sim.Done(0.0))
    Assert.False(sim.Done(halfSec))
    let mid = sim.Position(halfSec)
    Assert.True(mid > 0.0 && mid < 1.0, $"Expected mid in (0, 1), got {mid}.")
    Assert.True(sim.Done(fullSec))
    Assert.Equal(1.0, sim.Position(fullSec), 6)

  assertDuration(MetroTilt.pressSpec, 100.0)
  assertDuration(MetroTilt.releaseSpec, 167.0)
  assertDuration(MetroContentDialog.transitionSpec, 280.0)

[<Fact>]
let ``MetroTiltCell 2D fallback compresses scale and nudges on press then recovers``
  ()
  =
  use host = createHost(400, 300)
  let mutable tiltRef: MetroTiltCell voption = ValueNone
  let mutable clicks = 0

  let tiltFactory() =
    let cell = MetroTilt.createWithEffect ValueNone
    tiltRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      let child =
        MetroIconicTile.build(
          MetroIconicTile.defaults("Mail", "mail"),
          fun () -> clicks <- clicks + 1
        )

      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.FlexStart
      )
        .Children(
          MetroTilt.mountWith("tilt-2d", MetroTilt.defaults child, tiltFactory)
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tiltCell =
    match tiltRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroTiltCell to mount."

  Assert.Equal(MetroTiltMode.Fallback2D, tiltCell.State.Mode)
  Assert.False(tiltCell.State.IsPressed)
  Assert.False(tiltCell.State.IsShaderAttached)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tiltCell.View)
  let wrapper = tree :?> Container

  // Press near the bottom-right corner of the 152x152 medium tile.
  wrapper.OnPointerDown.Invoke(pointerAt(152.0, 152.0))
  pumpFrames(host, 1)

  let pressed = tiltCell.State
  Assert.True(pressed.IsPressed)
  Assert.False(pressed.IsShaderAttached)
  Assert.True(pressed.NormalizedX > 0.5)
  Assert.True(pressed.NormalizedY > 0.5)
  Assert.True(pressed.ScaleAnim.Running)

  Assert.Equal(
    MetroTilt.defaultConfig.FallbackScale,
    pressed.ScaleAnim.Target,
    3
  )

  Assert.True(pressed.TranslateAnim.Running)
  Assert.True(pressed.TranslateAnim.Target.X > 1.5)
  Assert.True(pressed.TranslateAnim.Target.Y > 1.5)

  // Release recovers scale to 1.0 and translate to (0, 0).
  wrapper.OnPointerUp.Invoke(pointerAt(152.0, 152.0))
  pumpFrames(host, 1)

  let released = tiltCell.State
  Assert.False(released.IsPressed)
  Assert.Equal(1.0, released.ScaleAnim.Target, 3)
  Assert.Equal(0.0, released.TranslateAnim.Target.X, 3)
  Assert.Equal(0.0, released.TranslateAnim.Target.Y, 3)

  // Reduced motion suppresses press tilt completely.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 2)
  wrapper.OnPointerDown.Invoke(pointerAt(10.0, 10.0))
  pumpFrames(host, 1)
  Assert.False(tiltCell.State.IsPressed)
  Assert.False(tiltCell.State.ScaleAnim.Running)
  Assert.Equal(1.0, tiltCell.State.ScaleAnim.Value, 3)

[<Fact>]
let ``MetroTiltCell 3D shader mode attaches ShaderEffect on press and respects reduced motion``
  ()
  =
  use host = createHost(400, 300)
  let effect = createSyntheticEffect()
  let mutable tiltRef: MetroTiltCell voption = ValueNone

  let tiltFactory() =
    let cell = MetroTilt.createWithEffect(ValueSome effect)
    tiltRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      let child =
        MetroIconicTile.build(
          MetroIconicTile.defaults("Calendar", "calendar_today"),
          ignore
        )

      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.FlexStart
      )
        .Children(
          MetroTilt.mountWith("tilt-3d", MetroTilt.defaults child, tiltFactory)
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tiltCell =
    match tiltRef with
    | ValueSome cell -> cell
    | ValueNone -> failwith "Expected MetroTiltCell to mount."

  Assert.Equal(MetroTiltMode.Perspective3D, tiltCell.State.Mode)
  Assert.False(tiltCell.State.IsShaderAttached)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tiltCell.View)
  let wrapper = tree :?> Container

  wrapper.OnPointerDown.Invoke(pointerAt(0.0, 0.0))
  pumpFrames(host, 1)

  let pressed = tiltCell.State
  Assert.True(pressed.IsPressed)
  Assert.True(pressed.IsShaderAttached)
  Assert.True(pressed.NormalizedX < -0.5)
  Assert.True(pressed.NormalizedY < -0.5)
  Assert.True(pressed.TiltAnim.Running)

  Assert.Equal(
    MetroTilt.defaultConfig.PressScale3D,
    pressed.ScaleAnim.Target,
    3
  )

  // Under reduced motion, shader detaches and tilt snaps flat.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 2)
  Assert.False(tiltCell.State.IsPressed)
  Assert.False(tiltCell.State.IsShaderAttached)

[<Fact>]
let ``MetroLiveTileCell wires press tilt in 3D and 2D modes and rebuilds host on click``
  ()
  =
  use host = createHost(600, 400)
  let effect = createSyntheticEffect()
  let mutable clicks = 0
  let mutable hostBuilds = 0
  let mutable tile3dRef: MetroLiveTileCell voption = ValueNone
  let mutable tile2dRef: MetroLiveTileCell voption = ValueNone

  let faces = [|
    {
      Title = "Mail"
      Subtitle = ValueSome "3 unread"
      Metric = ValueSome "3"
      AccentOverride = ValueNone
    }
  |]

  let factory3d() =
    let cell =
      MetroLiveTile.createWithEffect(
        ValueSome effect,
        fun () -> clicks <- clicks + 1
      )

    tile3dRef <- ValueSome cell
    cell

  let factory2d() =
    let cell =
      MetroLiveTile.createWithEffect(ValueNone, fun () -> clicks <- clicks + 1)

    tile2dRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      hostBuilds <- hostBuilds + 1

      Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 16.0)
        .Children(
          MetroLiveTile.mount(
            "live-3d",
            MetroLiveTile.defaults faces,
            factory3d
          ),
          MetroLiveTile.mount(
            "live-2d",
            MetroLiveTile.defaults faces,
            factory2d
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tile3d = tile3dRef.Value
  let tile2d = tile2dRef.Value
  Assert.Equal(MetroTiltMode.Perspective3D, tile3d.TiltState.Mode)
  Assert.Equal(MetroTiltMode.Fallback2D, tile2d.TiltState.Mode)

  let mutable btn3d: Button = null
  let mutable btn2d: Button = null

  root.DuringBuild(
    host,
    fun () ->
      btn3d <- tile3d.View :?> Button
      btn2d <- tile2d.View :?> Button
  )

  btn3d.OnPointerDown.Invoke(pointerAt(10.0, 10.0))
  btn2d.OnPointerDown.Invoke(pointerAt(10.0, 10.0))
  pumpFrames(host, 1)

  Assert.True(tile3d.TiltState.IsPressed)
  Assert.True(tile3d.TiltState.IsShaderAttached)
  Assert.True(tile2d.TiltState.IsPressed)
  Assert.False(tile2d.TiltState.IsShaderAttached)

  // Clicking still delivers the callback to the host cell via InvokeInputCallback.
  let buildsBefore = hostBuilds
  btn3d.OnClick.Invoke()
  pumpFrames(host, 1)
  Assert.Equal(1, clicks)
  Assert.Equal(buildsBefore + 1, hostBuilds)

[<Fact>]
let ``MetroSettingsFlyoutCell animates open and close and delivers dismiss to host``
  ()
  =
  use host = createHost(900, 600)
  let mutable isOpen = false
  let mutable dismisses = 0
  let mutable hostBuilds = 0
  let mutable flyoutRef: MetroSettingsFlyoutCell voption = ValueNone

  let flyoutFactory() =
    let cell =
      MetroSettingsFlyout.create(
        (fun () ->
          dismisses <- dismisses + 1
          isOpen <- false),
        fun () -> MetroText.body "Theme controls"
      )

    flyoutRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      hostBuilds <- hostBuilds + 1

      MetroSettingsFlyout.mount(
        "test-flyout",
        MetroSettingsFlyout.defaults("Settings", isOpen),
        flyoutFactory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let flyout = flyoutRef.Value
  Assert.False(flyout.IsVisible)
  Assert.False(flyout.IsAnimating)

  // Open starts the 333 ms right-edge slide and backdrop fade.
  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(flyout.IsVisible)
  Assert.True(flyout.IsAnimating)
  Assert.Equal(0.0, flyout.PanelTranslate.Target.X, 3)
  Assert.Equal(1.0, flyout.BackdropOpacity.Target, 3)

  // Dismiss through Portal.OnDismiss rebuilds host without Rebuild() in handler.
  let mutable portal: Portal = null
  root.DuringBuild(host, fun () -> portal <- flyout.View :?> Portal)
  let buildsBefore = hostBuilds
  portal.OnDismiss.Invoke(PopupDismissReason.Escape)
  pumpFrames(host, 2)

  Assert.Equal(1, dismisses)
  Assert.False(isOpen)
  Assert.Equal(buildsBefore + 1, hostBuilds)
  Assert.True(flyout.IsVisible)
  Assert.True(flyout.IsAnimating)
  Assert.Equal(346.0, flyout.PanelTranslate.Target.X, 3)
  Assert.Equal(0.0, flyout.BackdropOpacity.Target, 3)

  // Reduced motion closes immediately with no lingering visibility.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  pumpFrames(host, 2)
  Assert.False(flyout.IsVisible)
  Assert.False(flyout.IsAnimating)

[<Fact>]
let ``MetroContentDialogCell animates in 3D and 2D modes and delivers actions to host``
  ()
  =
  use host = createHost(900, 600)
  let effect = createSyntheticEffect()
  let mutable open3d = false
  let mutable open2d = false
  let mutable hostBuilds = 0
  let mutable lastAction: MetroContentDialogAction voption = ValueNone
  let mutable dialog3dRef: MetroContentDialogCell voption = ValueNone
  let mutable dialog2dRef: MetroContentDialogCell voption = ValueNone

  let factory3d() =
    let cell =
      MetroContentDialog.createWithEffect(
        ValueSome effect,
        (fun action ->
          lastAction <- ValueSome action
          open3d <- false),
        fun () -> MetroText.body "3D body"
      )

    dialog3dRef <- ValueSome cell
    cell

  let factory2d() =
    let cell =
      MetroContentDialog.createWithEffect(
        ValueNone,
        (fun action ->
          lastAction <- ValueSome action
          open2d <- false),
        fun () -> MetroText.body "2D body"
      )

    dialog2dRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      hostBuilds <- hostBuilds + 1

      Container(Width = Length.Percent 100.0, Height = Length.Percent 100.0)
        .Children(
          MetroContentDialog.mount(
            "dlg-3d",
            {
              MetroContentDialog.defaults("3D Dialog", open3d) with
                  PrimaryText = ValueSome "Confirm"
                  SecondaryText = ValueSome "Cancel"
            },
            factory3d
          ),
          MetroContentDialog.mount(
            "dlg-2d",
            {
              MetroContentDialog.defaults("2D Dialog", open2d) with
                  PrimaryText = ValueSome "OK"
            },
            factory2d
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dlg3d = dialog3dRef.Value
  let dlg2d = dialog2dRef.Value
  Assert.Equal(MetroTiltMode.Perspective3D, dlg3d.Mode)
  Assert.Equal(MetroTiltMode.Fallback2D, dlg2d.Mode)
  Assert.False(dlg3d.IsVisible)
  Assert.False(dlg2d.IsVisible)

  // Open both dialogs: 3D attaches shader, 2D runs without shader.
  open3d <- true
  open2d <- true
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(dlg3d.IsVisible)
  Assert.True(dlg3d.IsAnimating)
  Assert.True(dlg3d.IsShaderAttached)
  Assert.Equal(1.0, dlg3d.ProgressAnim.Target, 3)
  Assert.Equal(1.0, dlg3d.BackdropOpacity.Target, 3)

  Assert.True(dlg2d.IsVisible)
  Assert.True(dlg2d.IsAnimating)
  Assert.False(dlg2d.IsShaderAttached)
  Assert.Equal(1.0, dlg2d.ProgressAnim.Target, 3)
  Assert.Equal(1.0, dlg2d.BackdropOpacity.Target, 3)

  // Activating the primary action rebuilds the host without Rebuild() in handler.
  let buildsBefore = hostBuilds
  dlg3d.Handle MetroContentDialogAction.Primary
  pumpFrames(host, 2)

  Assert.Equal(ValueSome MetroContentDialogAction.Primary, lastAction)
  Assert.False(open3d)
  Assert.Equal(buildsBefore + 1, hostBuilds)
  Assert.True(dlg3d.IsVisible)
  Assert.True(dlg3d.IsAnimating)
  Assert.True(dlg3d.IsShaderAttached)
  Assert.Equal(0.0, dlg3d.BackdropOpacity.Target, 3)

  // Dismissing the 2D dialog through Portal.OnDismiss also rebuilds the host.
  let mutable portal2d: Portal = null
  root.DuringBuild(host, fun () -> portal2d <- dlg2d.View :?> Portal)
  let buildsBefore2d = hostBuilds
  portal2d.OnDismiss.Invoke(PopupDismissReason.Escape)
  pumpFrames(host, 2)
  Assert.Equal(ValueSome MetroContentDialogAction.Dismiss, lastAction)
  Assert.False(open2d)
  Assert.Equal(buildsBefore2d + 1, hostBuilds)
  Assert.True(dlg2d.IsVisible)
  Assert.True(dlg2d.IsAnimating)

  // Reduced motion snaps closed immediately and detaches the 3D shader.
  host.UpdatePreferences(
    PlatformPreferences(Theme = SystemTheme.Dark, ReducedMotion = Nullable true)
  )

  root.Rebuild()
  pumpFrames(host, 2)
  Assert.False(dlg3d.IsVisible)
  Assert.False(dlg3d.IsShaderAttached)
  Assert.False(dlg2d.IsVisible)

[<Fact>]
let ``MetroTilt isolates press tilt per tile and suppresses tilt for disabled children``
  ()
  =
  use host = createHost(900, 600)
  let mutable tapCount = 0
  let mutable photosRef: MetroTiltCell voption = ValueNone
  let mutable settingsRef: MetroTiltCell voption = ValueNone
  let mutable disabledRef: MetroTiltCell voption = ValueNone

  let photosFactory() =
    let cell = MetroTilt.createWithEffect ValueNone
    photosRef <- ValueSome cell
    cell

  let settingsFactory() =
    let cell = MetroTilt.createWithEffect ValueNone
    settingsRef <- ValueSome cell
    cell

  let disabledFactory() =
    let cell = MetroTilt.createWithEffect ValueNone
    disabledRef <- ValueSome cell
    cell

  let root =
    CaptureCell.create(fun () ->
      Container(FlexDirection = FlexDirection.Row, Gap = Length.Pixels 8.0)
        .Children(
          MetroTilt.mountWith(
            "iso-photos",
            MetroTilt.defaults(
              MetroIconicTile.build(
                {
                  MetroIconicTile.defaults("Photos", "photo") with
                      Size = MetroTileSize.Small
                      AccentOverride = ValueSome(Color.Rgb(162, 0, 255))
                },
                fun () -> tapCount <- tapCount + 1
              )
            ),
            photosFactory
          ),
          MetroTilt.mountWith(
            "iso-settings",
            MetroTilt.defaults(
              MetroIconicTile.build(
                {
                  MetroIconicTile.defaults("Settings", "settings") with
                      Size = MetroTileSize.Small
                      AccentOverride = ValueSome(Color.Rgb(100, 118, 135))
                },
                fun () -> tapCount <- tapCount + 1
              )
            ),
            settingsFactory
          ),
          MetroTilt.mountWith(
            "iso-disabled",
            MetroTilt.defaults(
              MetroButton.build(
                {
                  MetroButton.defaults "Disabled" with
                      Disabled = true
                },
                ignore
              )
            ),
            disabledFactory
          )
        ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let photosCell = photosRef.Value
  let settingsCell = settingsRef.Value
  let disabledCell = disabledRef.Value

  let mutable photosWrap: Container = null
  let mutable disabledWrap: Container = null

  root.DuringBuild(
    host,
    fun () ->
      photosWrap <- photosCell.View :?> Container
      disabledWrap <- disabledCell.View :?> Container
  )

  // Pressing Photos tilts only Photos and does not affect Settings.
  photosWrap.OnPointerDown.Invoke(pointerAt(4.0, 4.0))
  pumpFrames(host, 1)
  Assert.True(photosCell.State.IsPressed)
  Assert.False(settingsCell.State.IsPressed)

  // Wrapping a disabled MetroButton suppresses pointer handlers automatically.
  Assert.Null(disabledWrap.OnPointerDown)
  Assert.False(disabledCell.State.IsPressed)
