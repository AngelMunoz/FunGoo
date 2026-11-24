module FunGoo.Metro.Tests.NotificationsTests

open System
open System.Reflection
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

[<Fact>]
let ``MetroInfoBar builds logical start stripe in LTR and RTL, collapses when closed, and fires action and close``
  ()
  =
  use host = createHost(600, 400)
  let barHandle = ElementHandle()
  let events = ResizeArray<MetroInfoBarEvent>()
  let mutable infoOpen = true
  let mutable rtl = false

  let root =
    CaptureCell.create(fun () ->
      let bar =
        MetroInfoBar.build(
          {
            MetroInfoBar.warning("Low Disk", "Free space is below 10%.") with
                ActionText = ValueSome "Clean Up"
                Open = infoOpen
          },
          fun ev ->
            events.Add ev

            if ev = MetroInfoBarEvent.Closed then
              infoOpen <- false
        )

      bar.Handle <- barHandle

      Container(
        Width = Length.Percent 100.0,
        Direction =
          (if rtl then Direction.RightToLeft else Direction.LeftToRight)
      )
        .Children(bar))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // In LTR: 4 px start border on the left + 16 px horizontal padding = 20 px left inset,
  // while right inset is 16 px padding only.
  let ltrLeftInset = barHandle.ContentBox.X - barHandle.BorderBox.X

  let ltrRightInset =
    (barHandle.BorderBox.X + barHandle.BorderBox.Width)
    - (barHandle.ContentBox.X + barHandle.ContentBox.Width)

  Assert.Equal(20.0, ltrLeftInset, 1)
  Assert.Equal(16.0, ltrRightInset, 1)

  // Flip to RTL: logical BorderStartWidth automatically moves the 4 px stripe to the right!
  rtl <- true
  root.Rebuild()
  pumpFrames(host, 2)

  let rtlLeftInset = barHandle.ContentBox.X - barHandle.BorderBox.X

  let rtlRightInset =
    (barHandle.BorderBox.X + barHandle.BorderBox.Width)
    - (barHandle.ContentBox.X + barHandle.ContentBox.Width)

  Assert.Equal(16.0, rtlLeftInset, 1)
  Assert.Equal(20.0, rtlRightInset, 1)

  let closeBtn =
    requireBlobByKey(root.Captured, "metro-infobar-close") :?> Button

  closeBtn.OnClick.Invoke()
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.Contains(MetroInfoBarEvent.Closed, events)
  Assert.True(findBlobByKey(root.Captured, "metro-infobar-close").IsNone)
  Assert.Equal(0.0, barHandle.BorderBox.Width)
  Assert.Equal(0.0, barHandle.BorderBox.Height)

[<Fact>]
let ``MetroToastHostCell schedules auto-dismiss timers in open window, dismisses via InvokeInputCallback, and clears timers``
  ()
  =
  use host = createHost(600, 400)

  let mutable items: MetroToastItem array = [|
    {
      MetroToast.titledItem("t1", "Saved", "Layout saved.") with
          Severity = MetroInfoBarSeverity.Success
          ActionText = ValueSome "Undo"
          DurationMs = ValueSome 500.0
    }
    {
      MetroToast.item("t2", "Persistent") with
          DurationMs = ValueNone
    }
  |]

  let events = ResizeArray<MetroToastEvent>()
  let mutable hostRef: MetroToastHostCell voption = ValueNone

  let factory() =
    let c =
      MetroToast.createHost(fun ev ->
        events.Add ev

        match ev with
        | MetroToastEvent.Dismissed id
        | MetroToastEvent.ActionInvoked id ->
          items <- items |> Array.filter(fun t -> t.Id <> id))

    hostRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroToast.mountHost(
        "toast-host",
        MetroToast.hostDefaults items,
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let toastHost =
    match hostRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroToastHostCell."

  Assert.Equal(2, toastHost.ActiveCount)
  Assert.Equal(1, toastHost.ActiveTimerCount)

  let buildsBefore = root.Builds
  toastHost.TriggerAction "t2"
  pumpFrames(host, 2)

  Assert.Contains(MetroToastEvent.ActionInvoked "t2", events)
  Assert.Equal(1, toastHost.ActiveCount)
  Assert.Equal(buildsBefore + 1, root.Builds)

  // Window timers run on the wall clock; wait past 500 ms then pump frames.
  System.Threading.Thread.Sleep(700)
  pumpFrames(host, 3)

  Assert.Contains(MetroToastEvent.Dismissed "t1", events)
  Assert.Equal(0, toastHost.ActiveCount)
  Assert.Equal(0, toastHost.ActiveTimerCount)

[<Fact>]
let ``MetroTeachingTipCell animates open, handles action and light dismiss via InvokeInputCallback``
  ()
  =
  use host = createHost(600, 400)
  let anchor = ElementHandle()
  let mutable isOpen = false
  let events = ResizeArray<MetroTeachingTipEvent>()
  let mutable tipRef: MetroTeachingTipCell voption = ValueNone

  let factory() =
    let c =
      MetroTeachingTip.create(fun ev ->
        events.Add ev
        isOpen <- false)

    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      let target = MetroButton.standard("Target", ignore)
      target.Handle <- anchor

      let props = {
        MetroTeachingTip.anchored(anchor, "Pin Favourite", isOpen) with
            Subtitle = ValueSome "Start screen tip"
            Body = ValueSome "Right-click any item to pin it."
            ActionText = ValueSome "Got It"
      }

      Container()
        .Children(target, MetroTeachingTip.mount("tip", props, factory)))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTeachingTipCell."

  Assert.False tip.IsPresented

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True tip.IsPresented
  Assert.True tip.PanelOpacity.Running

  let mutable portal: Portal = null
  root.DuringBuild(host, fun () -> portal <- tip.View :?> Portal)
  let buildsBefore = root.Builds
  portal.OnDismiss.Invoke(PopupDismissReason.Escape)
  pumpFrames(host, 2)

  Assert.Contains(MetroTeachingTipEvent.Closed, events)
  Assert.False isOpen
  Assert.Equal(buildsBefore + 1, root.Builds)

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)
  tip.InvokeAction()
  pumpFrames(host, 2)

  Assert.Contains(MetroTeachingTipEvent.ActionInvoked, events)
  Assert.False isOpen

let private portalFlags =
  BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance

/// First content child of a teaching-tip <c>Portal</c> (the panel surface).
/// <c>Portal</c> exposes no public children, so this mirrors the reflection
/// access used for overlay portals in <c>NavigationAndOverlaysTests</c>.
let private portalPanel(portal: Portal) : Container =
  let children =
    typeof<Portal>.GetProperty("Children", portalFlags).GetValue(portal)

  children
    .GetType()
    .GetProperty("Item", portalFlags)
    .GetValue(children, [| box 0 |])
  :?> Container

[<Fact>]
let ``MetroTeachingTipCell sends Closed once for Escape and dismiss during close animation``
  ()
  =
  use host = createHost(600, 400)
  let anchor = ElementHandle()
  let mutable isOpen = true
  let events = ResizeArray<MetroTeachingTipEvent>()
  let mutable tipRef: MetroTeachingTipCell voption = ValueNone

  let factory() =
    let c = MetroTeachingTip.create(events.Add)
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      let target = MetroButton.standard("Target", ignore)
      target.Handle <- anchor

      let props = MetroTeachingTip.anchored(anchor, "Tip", isOpen)

      Container()
        .Children(target, MetroTeachingTip.mount("tip", props, factory)))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTeachingTipCell."

  Assert.True tip.IsPresented

  let closedCount() =
    events
    |> Seq.filter(fun ev -> ev = MetroTeachingTipEvent.Closed)
    |> Seq.length

  let mutable portal: Portal = null
  root.DuringBuild(host, fun () -> portal <- tip.View :?> Portal)
  let panel = portalPanel portal
  Assert.Equal("metro-teaching-tip-panel", panel.Key)

  // One Escape reaches both the panel key binding and Portal.OnDismiss.
  findKeyBinding(panel.KeyBindings, Key.Escape).OnPress.Invoke()
  portal.OnDismiss.Invoke(PopupDismissReason.Escape)
  pumpFrames(host, 1)

  Assert.Equal(1, closedCount())

  // The tip stays mounted during the close animation, so a second
  // dismiss (press outside) must not send Closed again.
  portal.OnDismiss.Invoke(PopupDismissReason.OutsideInteraction)
  pumpFrames(host, 1)

  Assert.Equal(1, closedCount())

  // Reopening the tip arms the one-shot guard again.
  isOpen <- false
  root.Rebuild()
  pumpFrames(host, 2)
  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)

  root.DuringBuild(host, fun () -> portal <- tip.View :?> Portal)
  portal.OnDismiss.Invoke(PopupDismissReason.Escape)
  pumpFrames(host, 1)

  Assert.Equal(2, closedCount())

[<Fact>]
let ``MetroInfoBar and MetroToast card builders stay within steady-state allocation budget``
  ()
  =
  use host = createHost(400, 300)
  let infoProps = MetroInfoBar.informational("Sync", "Up to date.")
  let toastItem = MetroToast.titledItem("t1", "Sync", "Up to date.")
  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  // Warm up MaterialIcons cache and JIT.
  for _ = 1 to 16 do
    root.DuringBuild(
      host,
      fun () ->
        MetroInfoBar.build(infoProps, ignore) |> ignore
        MetroToast.buildCard(toastItem, ignore) |> ignore
    )

  let before = GC.GetAllocatedBytesForCurrentThread()

  root.DuringBuild(
    host,
    fun () ->
      for _ = 1 to 100 do
        MetroInfoBar.build(infoProps, ignore) |> ignore
        MetroToast.buildCard(toastItem, ignore) |> ignore
  )

  let perPair = float(GC.GetAllocatedBytesForCurrentThread() - before) / 100.0

  Assert.True(
    perPair < 24576.0,
    $"Expected < 24 KB per pair, got {perPair:F1} B"
  )

[<Fact>]
let ``light dismiss and closable work on their own``() =
  use host = createHost(600, 400)
  let anchor = ElementHandle()
  let mutable lightDismiss = true
  let mutable closable = true
  let mutable portal: Portal = null
  let mutable panel: Container = null

  let props() = {
    MetroTeachingTip.anchored(anchor, "Tip", true) with
        Closable = closable
        LightDismiss = lightDismiss
  }

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  for dismiss in [ true; false ] do
    for close in [ true; false ] do
      lightDismiss <- dismiss
      closable <- close

      root.DuringBuild(
        host,
        fun () ->
          portal <- MetroTeachingTip.build(props(), ignore) :?> Portal

          panel <- MetroTeachingTip.buildSurface(props(), ignore) :?> Container
      )

      let hasEscape =
        (if isNull panel.KeyBindings then
           Array.empty
         else
           panel.KeyBindings)
        |> Array.exists(fun binding ->
          binding.Key = Key.Escape && binding.Modifiers = KeyModifiers.None)

      Assert.Equal(dismiss, hasEscape)
      Assert.Equal(dismiss, not(isNull portal.OnDismiss))

      Assert.Equal(
        close,
        findBlobByKey(panel, "metro-teaching-tip-close").IsSome
      )

[<Fact>]
let ``a duration change reschedules the toast timer``() =
  use host = createHost(600, 400)

  let mutable items: MetroToastItem array = [|
    {
      MetroToast.item("t1", "Rescheduled.") with
          DurationMs = ValueSome 5000.0
    }
  |]

  let events = ResizeArray<MetroToastEvent>()
  let mutable hostRef: MetroToastHostCell voption = ValueNone

  let factory() =
    let c = MetroToast.createHost(fun ev -> events.Add ev)

    hostRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroToast.mountHost(
        "toast-host",
        MetroToast.hostDefaults items,
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let toastHost =
    match hostRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroToastHostCell."

  Assert.Equal(1, toastHost.ActiveTimerCount)

  items <- [|
    {
      MetroToast.item("t1", "Rescheduled.") with
          DurationMs = ValueSome 50.0
    }
  |]

  root.Rebuild()
  pumpFrames(host, 2)

  Assert.Equal(1, toastHost.ActiveTimerCount)

  // Window timers run on the wall clock; wait past 50 ms then pump frames.
  System.Threading.Thread.Sleep(700)
  pumpFrames(host, 3)

  Assert.Contains(MetroToastEvent.Dismissed "t1", events)
  Assert.Equal(0, toastHost.ActiveTimerCount)

[<Fact>]
let ``a host that keeps a dismissed toast does not break the next build``() =
  use host = createHost(600, 400)

  let items: MetroToastItem array = [|
    {
      MetroToast.item("t1", "Kept after dismissal.") with
          DurationMs = ValueSome 50.0
    }
  |]

  let events = ResizeArray<MetroToastEvent>()
  let mutable hostRef: MetroToastHostCell voption = ValueNone

  // The host keeps the dismissed toast in its input on purpose.
  let factory() =
    let c = MetroToast.createHost(fun ev -> events.Add ev)
    hostRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroToast.mountHost(
        "toast-host",
        MetroToast.hostDefaults items,
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let toastHost =
    match hostRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroToastHostCell."

  Assert.Equal(1, toastHost.ActiveTimerCount)

  System.Threading.Thread.Sleep(200)
  pumpFrames(host, 4)

  Assert.Contains(MetroToastEvent.Dismissed "t1", events)
  Assert.Equal(0, toastHost.ActiveTimerCount)

[<Fact>]
let ``a blank title gives the message as the accessible name``() =
  use host = createHost(600, 400)
  let infoProps = MetroInfoBar.informational("  ", "Disk almost full.")
  let toastItem = MetroToast.titledItem("t1", "  ", "Disk almost full.")
  let mutable infoName = ""
  let mutable toastName = ""

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      let bar = MetroInfoBar.build(infoProps, ignore) :?> Container
      let card = MetroToast.buildCard(toastItem, ignore) :?> Container
      infoName <- bar.AccessibilityName
      toastName <- card.AccessibilityName
  )

  Assert.Equal("Disk almost full.", infoName)
  Assert.Equal("Disk almost full.", toastName)

[<Fact>]
let ``an error toast is assertive and other toasts are polite``() =
  use host = createHost(600, 400)

  let errorItem = {
    MetroToast.item("t1", "Upload failed.") with
        Severity = MetroInfoBarSeverity.Error
  }

  let okItem = {
    MetroToast.item("t2", "Upload finished.") with
        Severity = MetroInfoBarSeverity.Success
  }

  let mutable errorLive = AccessibilityLive.Off
  let mutable okLive = AccessibilityLive.Off

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      let errorCard = MetroToast.buildCard(errorItem, ignore) :?> Container
      let okCard = MetroToast.buildCard(okItem, ignore) :?> Container
      errorLive <- errorCard.Accessibility.Live
      okLive <- okCard.Accessibility.Live
  )

  Assert.Equal(AccessibilityLive.Assertive, errorLive)
  Assert.Equal(AccessibilityLive.Polite, okLive)

[<Fact>]
let ``the teaching tip portal restores the focus``() =
  use host = createHost(600, 400)
  let mutable portal: Portal = null

  let props = {
    MetroTeachingTip.defaults("Tip", true) with
        LightDismiss = false
  }

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () -> portal <- MetroTeachingTip.build(props, ignore) :?> Portal
  )

  Assert.NotNull(portal.FocusScope)

[<Fact>]
let ``a blank accessibility name gives the fallback name``() =
  use host = createHost(600, 400)

  let infoBlank = {
    MetroInfoBar.informational("Disk", "Almost full.") with
        AccessibilityName = ValueSome "  "
  }

  let infoNull = {
    MetroInfoBar.informational("Disk", "Almost full.") with
        AccessibilityName = ValueSome null
  }

  let tipBlank = {
    MetroTeachingTip.defaults("Tip", true) with
        AccessibilityName = ValueSome "  "
  }

  let tipNull = {
    MetroTeachingTip.defaults("Tip", true) with
        AccessibilityName = ValueSome null
  }

  let mutable infoBlankName = ""
  let mutable infoNullName = ""
  let mutable tipBlankName = ""
  let mutable tipNullName = ""

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      let bar = MetroInfoBar.build(infoBlank, ignore) :?> Container
      infoBlankName <- bar.AccessibilityName

      let bar = MetroInfoBar.build(infoNull, ignore) :?> Container
      infoNullName <- bar.AccessibilityName

      let tip = MetroTeachingTip.buildSurface(tipBlank, ignore) :?> Container
      tipBlankName <- tip.AccessibilityName

      let tip = MetroTeachingTip.buildSurface(tipNull, ignore) :?> Container
      tipNullName <- tip.AccessibilityName
  )

  Assert.False(String.IsNullOrWhiteSpace infoBlankName)
  Assert.False(String.IsNullOrWhiteSpace infoNullName)
  Assert.False(String.IsNullOrWhiteSpace tipBlankName)
  Assert.False(String.IsNullOrWhiteSpace tipNullName)

[<Fact>]
let ``a null anchor gives the centered tip``() =
  use host = createHost(600, 400)
  let mutable portal: Portal = null

  let props = {
    MetroTeachingTip.defaults("Tip", true) with
        Anchor = ValueSome null
  }

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () -> portal <- MetroTeachingTip.build(props, ignore) :?> Portal
  )

  Assert.Null(portal.Anchor)

[<Fact>]
let ``a warm build of the tip surface and of a toast card with an action allocates below the ceiling``
  ()
  =
  use host = createHost(400, 300)

  let tipActionProps = {
    MetroTeachingTip.defaults("Tip", true) with
        ActionText = ValueSome "Got It"
  }

  let tipPlainProps = MetroTeachingTip.defaults("Tip", true)

  let toastItem = {
    MetroToast.item("t1", "Saved.") with
        ActionText = ValueSome "Undo"
  }

  let mutable tipActionAllocated = 0L
  let mutable tipPlainAllocated = 0L
  let mutable toastAllocated = 0L

  let root = CaptureCell.create(fun () -> Container() :> Blob)
  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  root.DuringBuild(
    host,
    fun () ->
      for _ = 1 to 10 do
        MetroTeachingTip.buildSurface(tipActionProps, ignore) |> ignore

      let before = GC.GetAllocatedBytesForCurrentThread()
      MetroTeachingTip.buildSurface(tipActionProps, ignore) |> ignore
      tipActionAllocated <- GC.GetAllocatedBytesForCurrentThread() - before
  )

  root.DuringBuild(
    host,
    fun () ->
      for _ = 1 to 10 do
        MetroTeachingTip.buildSurface(tipPlainProps, ignore) |> ignore

      let before = GC.GetAllocatedBytesForCurrentThread()
      MetroTeachingTip.buildSurface(tipPlainProps, ignore) |> ignore
      tipPlainAllocated <- GC.GetAllocatedBytesForCurrentThread() - before
  )

  root.DuringBuild(
    host,
    fun () ->
      for _ = 1 to 10 do
        MetroToast.buildCard(toastItem, ignore) |> ignore

      let before = GC.GetAllocatedBytesForCurrentThread()
      MetroToast.buildCard(toastItem, ignore) |> ignore
      toastAllocated <- GC.GetAllocatedBytesForCurrentThread() - before
  )

  // Measured 12464 B in Release, 13960 B in Debug. Ceiling: 2x the larger,
  // rounded up to a multiple of 1024.
  Assert.True(
    tipActionAllocated <= 28672L,
    $"Tip surface with action allocated {tipActionAllocated} B."
  )

  // Measured 6592 B in Release, 8064 B in Debug. Ceiling: 2x the larger,
  // rounded up to a multiple of 1024.
  Assert.True(
    tipPlainAllocated <= 16384L,
    $"Tip surface without action allocated {tipPlainAllocated} B."
  )

  // Measured 16544 B in Release, 17280 B in Debug. Ceiling: 2x the larger,
  // rounded up to a multiple of 1024.
  Assert.True(
    toastAllocated <= 34816L,
    $"Toast card with action allocated {toastAllocated} B."
  )
