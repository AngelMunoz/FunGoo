module FunGoo.Metro.Tests.DialogsAndMenusTests

open System
open Xunit
open Goo
open FunGoo.Children
open FunGoo.Metro
open FunGoo.Metro.Tests.TestHost
open FunGoo.Metro.Tests.TestSupport

[<Fact>]
let ``MetroFlyoutCell animates open and close and rebuilds host via owner rule``
  ()
  =
  use host = createHost(500, 400)
  let anchor = ElementHandle()
  let mutable isOpen = false
  let mutable dismissedCount = 0
  let mutable flyoutRef: MetroFlyoutCell voption = ValueNone

  let factory() =
    let c =
      MetroFlyout.create(
        (fun () ->
          isOpen <- false
          dismissedCount <- dismissedCount + 1),
        fun () -> MetroText.body "Flyout content"
      )

    flyoutRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      let btn = Button(Handle = anchor)

      let fly =
        MetroFlyout.mount("fly", MetroFlyout.defaults(anchor, isOpen), factory)

      Container().Children(btn, fly))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let flyout =
    match flyoutRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroFlyoutCell."

  Assert.False(flyout.IsVisible)

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(flyout.IsVisible)
  Assert.True(flyout.IsAnimating)

  let buildsBefore = root.Builds
  flyout.Dismiss()
  pumpFrames(host, 2)

  Assert.False(isOpen)
  Assert.Equal(1, dismissedCount)
  Assert.Equal(buildsBefore + 1, root.Builds)
  // Remains mounted while close fade animates.
  Assert.True(flyout.IsVisible)
  Assert.True(flyout.IsAnimating)

[<Fact>]
let ``MetroMessageDialogCell invokes command results via owner rule``() =
  use host = createHost(600, 400)
  let mutable isOpen = true
  let mutable lastResult: MetroMessageDialogResult voption = ValueNone
  let mutable dialogRef: MetroMessageDialogCell voption = ValueNone

  let cmds = [|
    MetroMessageDialog.primaryCommand("delete", "Delete")
    MetroMessageDialog.command("cancel", "Cancel")
  |]

  let factory() =
    let c =
      MetroMessageDialog.create(fun res ->
        lastResult <- ValueSome res
        isOpen <- false)

    dialogRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroMessageDialog.mount(
        "msg",
        {
          MetroMessageDialog.defaults(
            ValueSome "Confirm",
            "Delete item?",
            isOpen
          ) with
              Commands = cmds
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dialog =
    match dialogRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroMessageDialogCell."

  Assert.True(dialog.IsVisible)

  let mutable portal: Portal = null
  root.DuringBuild(host, fun () -> portal <- dialog.View :?> Portal)
  let buildsBefore = root.Builds
  portal.OnDismiss.Invoke(PopupDismissReason.Escape)
  pumpFrames(host, 2)

  Assert.Equal(ValueSome MetroMessageDialogResult.Dismissed, lastResult)

  Assert.False(isOpen)
  Assert.Equal(buildsBefore + 1, root.Builds)

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)

  let buildsBeforeCmd = root.Builds
  dialog.Handle(MetroMessageDialogResult.CommandInvoked "delete")
  pumpFrames(host, 2)

  Assert.Equal(
    ValueSome(MetroMessageDialogResult.CommandInvoked "delete"),
    lastResult
  )

  Assert.False(isOpen)
  Assert.Equal(buildsBeforeCmd + 1, root.Builds)

[<Fact>]
let ``MetroMenuFlyout and MetroContextMenu cache mapped entries and delegates with zero churn``
  ()
  =
  let entries = [|
    MetroMenuFlyout.iconItem("cut", "Cut", "content_cut")
    MetroMenuFlyout.separator "s1"
    MetroMenuFlyout.submenu(
      "more",
      "More",
      [| MetroMenuFlyout.item("about", "About") |]
    )
  |]

  let mapped = MetroMenuFlyout.mapEntries entries
  Assert.Equal(3, mapped.Length)
  Assert.True(mapped[1].Separator)
  Assert.Equal(1, mapped[2].Children.Length)

  let anchor = ElementHandle()
  let mutable clicked = ""
  let mutable requestedAt = Point(X = 0.0, Y = 0.0)

  let menuFactory =
    MetroMenuFlyout.createFactory(function
      | MetroMenuEvent.ItemClicked id -> clicked <- id
      | MetroMenuEvent.OpenChanged _ -> ())

  let ctxFactory =
    MetroContextMenu.createFactory(function
      | MetroMenuEvent.ItemClicked id -> clicked <- id
      | MetroMenuEvent.OpenChanged _ -> ())

  let menuProps = MetroMenuFlyout.defaults(anchor, entries, false)
  let m1 = menuFactory.CreateInput menuProps
  let m2 = menuFactory.CreateInput menuProps

  Assert.True(obj.ReferenceEquals(m1.Items, m2.Items))
  Assert.True(obj.ReferenceEquals(m1.OnClick, m2.OnClick))
  Assert.True(obj.ReferenceEquals(m1.ConfigurePanel, m2.ConfigurePanel))

  for _ = 1 to 10 do
    menuFactory.CreateInput menuProps |> ignore

  let beforeBytes = GC.GetAllocatedBytesForCurrentThread()
  let _ = menuFactory.CreateInput menuProps
  let allocated = GC.GetAllocatedBytesForCurrentThread() - beforeBytes
  Assert.True(allocated <= 64L, $"Expected <= 64 B, allocated {allocated} B.")

  let ctxProps =
    MetroContextMenu.defaults(Point(X = 40.0, Y = 80.0), entries, true)

  let c1 = ctxFactory.CreateInput ctxProps
  Assert.Null(c1.Anchor)
  Assert.Equal(Nullable(Point(X = 40.0, Y = 80.0)), c1.WindowPoint)

  c1.OnClick.Invoke("cut")
  Assert.Equal("cut", clicked)

  let surface =
    MetroContextMenu.target(Container(), fun pt -> requestedAt <- pt)
    :?> Container

  surface.OnPointerDown.Invoke(
    PointerEvent(
      Button = PointerButton.Secondary,
      WindowPosition = Point(X = 120.0, Y = 240.0)
    )
  )

  Assert.Equal(120.0, requestedAt.X, 3)
  Assert.Equal(240.0, requestedAt.Y, 3)

[<Fact>]
let ``MetroTooltipCell supports controlled visibility and focus show or hide``
  ()
  =
  use host = createHost(400, 300)
  let mutable controlledOpen: bool voption = ValueNone
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              Open = controlledOpen
              DelayMs = 0.0
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  Assert.False(tip.IsVisible)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let targetWrapper =
    requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  targetWrapper.OnFocus.Invoke(FocusEvent())
  pumpFrames(host, 2)
  Assert.True(tip.IsVisible)

  targetWrapper.OnBlur.Invoke(FocusEvent())
  pumpFrames(host, 2)
  Assert.False(tip.IsVisible)

  controlledOpen <- ValueSome true
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.True(tip.IsVisible)

[<Fact>]
let ``a press on the backdrop dismisses a dismissible message dialog``() =
  use host = createHost(600, 400)
  let mutable lastResult: MetroMessageDialogResult voption = ValueNone

  let mutable backdrop: Blob = null

  let root =
    CaptureCell.create(fun () ->
      let struct (bd, _) =
        MetroMessageDialog.buildContent(
          {
            Props =
              MetroMessageDialog.defaults(
                ValueSome "Confirm",
                "Delete item?",
                true
              )
            Scale = ValueNone
            Backdrop = ValueNone
          },
          fun result -> lastResult <- ValueSome result
        )

      backdrop <- bd
      bd)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let backdropBox = backdrop :?> Container
  Assert.NotNull(backdropBox.OnPointerDown)

  backdropBox.OnPointerDown.Invoke(PointerEvent())

  Assert.Equal(ValueSome MetroMessageDialogResult.Dismissed, lastResult)

[<Fact>]
let ``the band is not inside the dimmed backdrop``() =
  use host = createHost(600, 400)
  let mutable backdrop: Blob = null
  let mutable band: Blob = null

  let root =
    CaptureCell.create(fun () ->
      let struct (bd, b) =
        MetroMessageDialog.buildContent(
          {
            Props =
              MetroMessageDialog.defaults(
                ValueSome "Confirm",
                "Delete item?",
                true
              )
            Scale = ValueNone
            Backdrop = ValueNone
          },
          ignore
        )

      backdrop <- bd
      band <- b
      bd)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let backdropBox = backdrop :?> Container
  Assert.Equal(0, childCount backdropBox)
  Assert.NotNull(backdropBox.OnPointerDown)
  Assert.True(childCount band > 0)

[<Fact>]
let ``a press on a command sends the command and no dismissal``() =
  use host = createHost(600, 400)
  let mutable results: MetroMessageDialogResult list = []

  let cmds = [|
    MetroMessageDialog.primaryCommand("delete", "Delete")
    MetroMessageDialog.command("cancel", "Cancel")
  |]

  let mutable band: Blob = null

  let root =
    CaptureCell.create(fun () ->
      let struct (_, b) =
        MetroMessageDialog.buildContent(
          {
            Props = {
              MetroMessageDialog.defaults(
                ValueSome "Confirm",
                "Delete item?",
                true
              ) with
                  Commands = cmds
            }
            Scale = ValueNone
            Backdrop = ValueNone
          },
          fun result -> results <- result :: results
        )

      band <- b
      b)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let deleteButton = requireBlobByKey(band, "msg-dialog-cmd-delete") :?> Button

  deleteButton.OnClick.Invoke()

  assertListEqual([ MetroMessageDialogResult.CommandInvoked "delete" ], results)

[<Fact>]
let ``the message dialog band is modal``() =
  use host = createHost(600, 400)
  let mutable band: Blob = null

  let root =
    CaptureCell.create(fun () ->
      let struct (_, b) =
        MetroMessageDialog.buildContent(
          {
            Props =
              MetroMessageDialog.defaults(
                ValueSome "Confirm",
                "Delete item?",
                true
              )
            Scale = ValueNone
            Backdrop = ValueNone
          },
          ignore
        )

      band <- b
      b)

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let bandBox = band :?> Container
  Assert.Equal(Nullable true, bandBox.Accessibility.Modal)

[<Fact>]
let ``the message dialog sends one result during the close animation``() =
  use host = createHost(600, 400)
  let mutable isOpen = true
  let mutable resultCount = 0
  let mutable dialogRef: MetroMessageDialogCell voption = ValueNone

  let factory() =
    let c =
      MetroMessageDialog.create(fun _ ->
        resultCount <- resultCount + 1
        isOpen <- false)

    dialogRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroMessageDialog.mount(
        "msg",
        MetroMessageDialog.defaults(
          ValueSome "Confirm",
          "Delete item?",
          isOpen
        ),
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dialog =
    match dialogRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroMessageDialogCell."

  Assert.True(dialog.IsVisible)

  dialog.Handle(MetroMessageDialogResult.CommandInvoked "delete")
  pumpFrames(host, 2)

  Assert.Equal(1, resultCount)
  Assert.False(isOpen)
  // The dialog stays mounted while its close animation runs.
  Assert.True(dialog.IsVisible)
  Assert.True(dialog.IsAnimating)

  dialog.Handle(MetroMessageDialogResult.CommandInvoked "delete")
  dialog.Handle(MetroMessageDialogResult.Dismissed)
  pumpFrames(host, 2)

  Assert.Equal(1, resultCount)

[<Fact>]
let ``a reopen during the close animation keeps the current opacity``() =
  use host = createHost(600, 400)
  let mutable isOpen = true
  let mutable dialogRef: MetroMessageDialogCell voption = ValueNone

  let factory() =
    let c = MetroMessageDialog.create(fun _ -> isOpen <- false)
    dialogRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroMessageDialog.mount(
        "msg",
        MetroMessageDialog.defaults(
          ValueSome "Confirm",
          "Delete item?",
          isOpen
        ),
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dialog =
    match dialogRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroMessageDialogCell."

  isOpen <- false
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(dialog.IsVisible)
  let valueDuringClose = dialog.BackdropOpacity.Value

  isOpen <- true
  root.Rebuild()
  pumpFrames(host, 2)

  Assert.True(dialog.IsVisible)
  Assert.NotEqual(0.0, dialog.BackdropOpacity.Value)
  Assert.NotEqual(0.0, valueDuringClose)

[<Fact>]
let ``hover and focus keep the tooltip open on their own``() =
  use host = createHost(400, 300)
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              DelayMs = 0.0
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnPointerEnter.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

  target.OnFocus.Invoke(FocusEvent())
  Assert.True(tip.IsVisible)

  target.OnPointerLeave.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

  target.OnBlur.Invoke(FocusEvent())
  Assert.False(tip.IsVisible)

  target.OnFocus.Invoke(FocusEvent())
  Assert.True(tip.IsVisible)

  target.OnPointerEnter.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

  target.OnBlur.Invoke(FocusEvent())
  Assert.True(tip.IsVisible)

  target.OnPointerLeave.Invoke(PointerEvent())
  Assert.False(tip.IsVisible)

[<Fact>]
let ``the tooltip stays hidden until the pointer returns after it is enabled again``
  ()
  =
  use host = createHost(400, 300)
  let mutable disabled = false
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              DelayMs = 0.0
              Disabled = disabled
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  Assert.False(tip.IsVisible)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnPointerEnter.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

  disabled <- true
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.False(tip.IsVisible)

  disabled <- false
  root.Rebuild()
  pumpFrames(host, 2)
  Assert.False(tip.IsVisible)

  target.OnPointerLeave.Invoke(PointerEvent())
  Assert.False(tip.IsVisible)

  target.OnPointerEnter.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

[<Fact>]
let ``a controlled tooltip ignores the pointer leave``() =
  use host = createHost(400, 300)
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              Open = ValueSome true
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  Assert.True(tip.IsVisible)

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnPointerLeave.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

[<Fact>]
let ``a warm build of the message dialog and of the flyout allocates below the ceiling``
  ()
  =
  use host = createHost(600, 400)
  let anchor = ElementHandle()

  let dialogProps =
    MetroMessageDialog.defaults(ValueSome "Confirm", "Delete item?", true)

  let flyoutProps = MetroFlyout.defaults(anchor, true)

  // Measured 10488 B in Release and 10824 B in Debug on 2026-11-24.
  let dialogCeiling = 22528L

  // Measured 7184 B in Release and 7384 B in Debug on 2026-11-24.
  let flyoutCeiling = 15360L

  let mutable dialogAllocated = 0L
  let mutable flyoutAllocated = 0L

  let root =
    CaptureCell.create(fun () ->
      let measure(build: unit -> Blob) : int64 =
        for _ = 1 to 10 do
          build() |> ignore

        let before = GC.GetAllocatedBytesForCurrentThread()
        let _ = build()
        GC.GetAllocatedBytesForCurrentThread() - before

      dialogAllocated <-
        measure(fun () -> MetroMessageDialog.build(dialogProps, ignore))

      flyoutAllocated <-
        measure(fun () ->
          MetroFlyout.build(
            flyoutProps,
            ignore,
            fun () -> MetroText.body "Flyout content"
          ))

      Container())

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 1)

  Assert.True(
    dialogAllocated <= dialogCeiling,
    $"Message dialog build allocated {dialogAllocated} B (ceiling {dialogCeiling} B)."
  )

  Assert.True(
    flyoutAllocated <= flyoutCeiling,
    $"Flyout build allocated {flyoutAllocated} B (ceiling {flyoutCeiling} B)."
  )

[<Fact>]
let ``the tooltip is not visible before the hover delay ends``() =
  use host = createHost(400, 300)
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              DelayMs = 200.0
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnPointerEnter.Invoke(PointerEvent())
  Assert.False(tip.IsVisible)

  // A window timer uses the wall clock, thus the test waits in real time for
  // more than the delay. The next frames run the timer action.
  System.Threading.Thread.Sleep(600)
  pumpFrames(host, 4)

  Assert.True(tip.IsVisible)

[<Fact>]
let ``a controlled hidden tooltip ignores the focus``() =
  use host = createHost(400, 300)
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              Open = ValueSome false
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnFocus.Invoke(FocusEvent())
  Assert.False(tip.IsVisible)

[<Fact>]
let ``an invalid delay shows the tooltip at once``() =
  use host = createHost(400, 300)
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              DelayMs = Double.PositiveInfinity
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnPointerEnter.Invoke(PointerEvent())
  Assert.True(tip.IsVisible)

[<Fact>]
let ``a controlled state cancels the pending hover``() =
  use host = createHost(400, 300)
  let mutable controlledOpen: bool voption = ValueNone
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              Open = controlledOpen
              DelayMs = 200.0
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  let mutable tree: Blob = null
  root.DuringBuild(host, fun () -> tree <- tip.View)

  let target = requireBlobByKey(tree, "metro-tooltip-target") :?> Container

  target.OnPointerEnter.Invoke(PointerEvent())

  controlledOpen <- ValueSome false
  root.Rebuild()
  pumpFrames(host, 2)

  // A window timer uses the wall clock, thus the test waits in real time past
  // the delay. The next frames would run a timer that was not cancelled.
  System.Threading.Thread.Sleep(600)
  pumpFrames(host, 4)

  controlledOpen <- ValueNone
  root.Rebuild()
  pumpFrames(host, 4)

  Assert.False(tip.IsVisible)

[<Fact>]
let ``the tooltip hides when the controlled open state ends and no trigger is active``
  ()
  =
  use host = createHost(400, 300)
  let mutable controlledOpen: bool voption = ValueSome true
  let mutable tipRef: MetroTooltipCell voption = ValueNone

  let factory() =
    let c = MetroTooltip.create()
    tipRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroTooltip.mountWith(
        "tip",
        {
          MetroTooltip.defaults(Button(), "Helpful hint") with
              Open = controlledOpen
              DelayMs = 200.0
        },
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let tip =
    match tipRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroTooltipCell."

  Assert.True(tip.IsVisible)

  controlledOpen <- ValueNone
  root.Rebuild()
  pumpFrames(host, 4)

  Assert.False(tip.IsVisible)

[<Fact>]
let ``two results before the host rebuild give one result``() =
  use host = createHost(600, 400)
  let mutable message = "Delete item?"
  let mutable resultCount = 0
  let mutable dialogRef: MetroMessageDialogCell voption = ValueNone

  let factory() =
    let c = MetroMessageDialog.create(fun _ -> resultCount <- resultCount + 1)

    dialogRef <- ValueSome c
    c

  let root =
    CaptureCell.create(fun () ->
      MetroMessageDialog.mount(
        "msg",
        MetroMessageDialog.defaults(ValueSome "Confirm", message, true),
        factory
      ))

  let window = Window(Root = root)
  Metro.attach(window, darkConfig)
  window.Attach(host) |> ignore
  pumpFrames(host, 2)

  let dialog =
    match dialogRef with
    | ValueSome c -> c
    | ValueNone -> failwith "Expected MetroMessageDialogCell."

  dialog.Handle(MetroMessageDialogResult.Dismissed)
  dialog.Handle(MetroMessageDialogResult.Dismissed)
  pumpFrames(host, 2)

  Assert.Equal(1, resultCount)

  message <- "Deleted."
  root.Rebuild()
  pumpFrames(host, 2)

  dialog.Handle(MetroMessageDialogResult.Dismissed)
  pumpFrames(host, 2)

  Assert.Equal(2, resultCount)
