module MetroGallery.Pages.DialogsPage

open Goo
open Goo.Widgets.Navigation
open FunGoo.Children
open FunGoo.Metro

/// Demo data of the dialogs page. Menu entries, context entries, and dialog
/// commands keep the pixel-equal values of the retired overlays tab.
module Content =

  let menuItems: MetroMenuEntry array = [|
    MetroMenuFlyout.iconItem("share", "Share", "share")
    MetroMenuFlyout.iconItem("pin", "Pin to Start", "push_pin")
    MetroMenuFlyout.submenu(
      "export",
      "Export As",
      [|
        MetroMenuFlyout.item("export-png", "PNG Frame")
        MetroMenuFlyout.item("export-json", "Token JSON")
      |]
    )
    MetroMenuFlyout.separator "sep-1"
    MetroMenuFlyout.iconItem("delete", "Delete", "delete")
  |]

  let contextItems: MetroMenuEntry array = [|
    MetroMenuFlyout.iconItem("copy", "Copy", "content_copy")
    MetroMenuFlyout.iconItem("paste", "Paste", "content_paste")
    MetroMenuFlyout.separator "ctx-sep"
    MetroMenuFlyout.iconItem("inspect", "Inspect Properties", "info")
  |]

  let dialogCommands: MetroDialogCommand array = [|
    MetroMessageDialog.primaryCommand("save", "Save")
    MetroMessageDialog.command("discard", "Don't Save")
    MetroMessageDialog.command("cancel", "Cancel")
  |]

/// The dialogs page. The headless check of the gallery reads the overlay and
/// the notice state through these members.
[<AbstractClass>]
type DialogsPageCell() =
  inherit Cell()

  /// The mounted message dialog, for the headless check.
  abstract MessageDialog: MetroMessageDialogCell voption

  /// The mounted tooltip, for the headless check.
  abstract Tooltip: MetroTooltipCell voption

  /// The latest menu flyout input, for the headless check.
  abstract MenuInput: MenuInput

  /// The latest context menu input, for the headless check.
  abstract ContextMenuInput: MenuInput

  /// The mounted toast host, for the headless check.
  abstract ToastHost: MetroToastHostCell voption

  /// The mounted teaching tip, for the headless check.
  abstract TeachingTip: MetroTeachingTipCell voption

  /// The bit mask of the open info bars.
  abstract InfoOpenMask: int

  /// The number of queued toasts.
  abstract ToastCount: int

  /// <c>true</c> when the anchored teaching tip is open.
  abstract TipOpen: bool

  /// Text of the last action, for the headless check.
  abstract LastAction: string

  /// The number of builds, for the headless check.
  abstract Builds: int

  /// Queues one sample toast and rebuilds this page.
  abstract PushSampleToast: unit -> unit

  /// Opens or closes the anchored teaching tip and rebuilds this page.
  abstract SetTipOpen: isOpen: bool -> unit

/// State of the page. One mutable field per concern.
type DialogsState = {
  FlyoutAnchor: ElementHandle
  MenuAnchor: ElementHandle
  TipAnchor: ElementHandle
  mutable FlyoutOpen: bool
  mutable MessageOpen: bool
  mutable MenuOpen: bool
  mutable ContextOpen: bool
  mutable ContextPoint: Point
  mutable PinnedTooltip: bool
  mutable LastMenuInput: MenuInput
  mutable LastContextInput: MenuInput
  mutable InfoOpenMask: int
  mutable Toasts: MetroToastItem array
  mutable NextToastId: int
  mutable TipOpen: bool
  mutable LastAction: string
  mutable Builds: int
}

/// The factories of the cells that the page mounts. The page makes each one
/// once, so that each mount reuses its factory.
type DialogsFactories = {
  Flyout: unit -> MetroFlyoutCell
  MessageDialog: unit -> MetroMessageDialogCell
  MenuFlyout: IMetroMenuFlyoutFactory
  ContextMenu: IMetroContextMenuFactory
  Tooltip: unit -> MetroTooltipCell
  ToastHost: unit -> MetroToastHostCell
  TeachingTip: unit -> MetroTeachingTipCell
}

/// One titled section of the page: a subheader in the type ramp, then the
/// content.
let section(title: string, content: Blob) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(MetroText.subheader title, content)

/// One column of a two-column row: a caption above the content.
let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 12.0
  )
    .Children(MetroText.subheader title, content)

/// <c>true</c> when the info bar of <paramref name="bit"/> is open.
let isBarOpen(mask: int, bit: int) : bool = (mask &&& (1 <<< bit)) <> 0

/// Closes the info bar of <paramref name="bit"/>.
let closeBar(state: DialogsState, bit: int, name: string) : unit =
  state.InfoOpenMask <- state.InfoOpenMask &&& ~~~(1 <<< bit)
  state.LastAction <- $"Closed {name} info bar"

/// Queues one sample toast.
let pushToast(state: DialogsState) : unit =
  let n = state.NextToastId
  state.NextToastId <- n + 1

  let sev =
    match n % 4 with
    | 1 -> MetroInfoBarSeverity.Informational
    | 2 -> MetroInfoBarSeverity.Success
    | 3 -> MetroInfoBarSeverity.Warning
    | _ -> MetroInfoBarSeverity.Error

  let item = {
    MetroToast.titledItem(
      $"toast-{n}",
      $"Telemetry Alert #{n}",
      "Retained Vulkan compositor frame completed."
    ) with
        Severity = sev
        ActionText = ValueSome "Inspect"
        DurationMs = ValueSome 5000.0
  }

  state.Toasts <- Array.append state.Toasts [| item |]
  state.LastAction <- $"Queued toast-{n}"

/// The body of the quick filter flyout.
let flyoutBody(onQuickApply: unit -> unit) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.subtitle "Quick Filter",
      MetroText.secondary
        "Anchored Portal flyout with 2 px border and Escape or outside-click light dismiss.",
      MetroButton.accent("Apply Filter", onQuickApply)
    )

/// The surfaces of the page: the flyout, the message dialog, and the tooltip
/// in one column, and the menu flyout and the context menu in the other. The
/// sizes, the order, and the demo data match the retired overlays tab.
let surfaces(state: DialogsState, factories: DialogsFactories) : Blob =
  let theme = Metro.read()
  let p = theme.Palette

  let flyoutTrigger =
    MetroButton.standard(
      "Open Flyout",
      fun () -> state.FlyoutOpen <- not state.FlyoutOpen
    )

  flyoutTrigger.Handle <- state.FlyoutAnchor

  let menuTrigger =
    MetroButton.standard(
      "Open Menu Flyout",
      fun () -> state.MenuOpen <- not state.MenuOpen
    )

  menuTrigger.Handle <- state.MenuAnchor

  let flyoutProps = {
    MetroFlyout.defaults(state.FlyoutAnchor, state.FlyoutOpen) with
        MinWidth = 260.0
        MaxWidth = ValueSome 320.0
        AccessibilityName = ValueSome "Quick filter flyout"
  }

  let msgProps = {
    MetroMessageDialog.defaults(
      ValueSome "Unsaved Metro Layout",
      "Do you want to save changes to your Start tile layout before closing?",
      state.MessageOpen
    ) with
        Commands = Content.dialogCommands
  }

  let menuProps =
    MetroMenuFlyout.defaults(
      state.MenuAnchor,
      Content.menuItems,
      state.MenuOpen
    )

  let ctxProps =
    MetroContextMenu.defaults(
      state.ContextPoint,
      Content.contextItems,
      state.ContextOpen
    )

  state.LastMenuInput <- factories.MenuFlyout.CreateInput menuProps
  state.LastContextInput <- factories.ContextMenu.CreateInput ctxProps

  let contextSurface =
    MetroContextMenu.target(
      Container(
        Width = Length.Percent 100.0,
        MinHeight = Length.Pixels 96.0,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Padding = Edges(Length.Pixels 16.0),
        BackgroundColor = p.BackgroundAlt
      )
        .Children(
          MetroText.body
            "Right-click anywhere inside this surface to open MetroContextMenu"
        ),
      fun pt ->
        state.ContextPoint <- pt
        state.ContextOpen <- true
        state.LastAction <- $"Context menu at ({int pt.X}, {int pt.Y})"
    )

  let tooltipInput = {
    MetroTooltip.defaults(
      MetroButton.subtle(
        "Hover or Pin Tooltip Target",
        fun () ->
          state.PinnedTooltip <- not state.PinnedTooltip

          state.LastAction <-
            (if state.PinnedTooltip then
               "Tooltip pinned"
             else
               "Tooltip auto")
      ),
      "Sharp 0 px Metro tooltip with 2 px border"
    ) with
        Placement = MetroFlyoutPlacement.Right
        Open = (if state.PinnedTooltip then ValueSome true else ValueNone)
  }

  let dialogsCol =
    column(
      "flyout & message dialog",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
        .Children(
          Container(
            FlexDirection = FlexDirection.Row,
            Gap = Length.Pixels 12.0,
            AlignItems = AlignItems.Center
          )
            .Children(
              flyoutTrigger,
              MetroButton.accent(
                "Show Message Dialog",
                fun () -> state.MessageOpen <- true
              )
            ),
          MetroText.caption "TOOLTIP",
          Container(
            FlexDirection = FlexDirection.Row,
            AlignItems = AlignItems.Center,
            Padding = Edges(Length.Pixels 8.0, Length.Pixels 0.0)
          )
            .Children(
              MetroTooltip.mountWith(
                "gallery-tooltip",
                tooltipInput,
                factories.Tooltip
              )
            ),
          MetroFlyout.mount("gallery-flyout", flyoutProps, factories.Flyout),
          MetroMessageDialog.mount(
            "gallery-message-dialog",
            msgProps,
            factories.MessageDialog
          )
        )
    )

  let menusCol =
    column(
      "menu flyout & context menu",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 16.0)
        .Children(
          Container(FlexDirection = FlexDirection.Row).Children(menuTrigger),
          contextSurface,
          factories.MenuFlyout.Mount("gallery-menu-flyout", menuProps),
          factories.ContextMenu.Mount("gallery-context-menu", ctxProps)
        )
    )

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    Gap = Length.Pixels 16.0
  )
    .Children(
      MetroText.secondary
        "Anchored flyouts, full-bleed message dialogs with multi-command \
         footers, nested menu flyouts, right-click context menus, and sharp \
         Metro tooltips.",
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        Gap = Length.Pixels 32.0
      )
        .Children(dialogsCol, menusCol)
    )

/// The notices of the page: the info bars in one column, and the toast and
/// the teaching tip in the other. The sizes, the order, and the demo data
/// match the retired notifications tab.
let notices(state: DialogsState, factories: DialogsFactories) : Blob =
  let infoCol =
    column(
      "info bar",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 10.0)
        .Children(
          MetroInfoBar.build(
            {
              MetroInfoBar.informational(
                "Cloud Tile Sync",
                "Start layout preferences are ready to synchronize across devices."
              ) with
                  ActionText = ValueSome "Sync Now"
                  Open = isBarOpen(state.InfoOpenMask, 0)
            },
            function
            | MetroInfoBarEvent.ActionInvoked ->
              state.LastAction <- "InfoBar action: Sync Now"
            | MetroInfoBarEvent.Closed -> closeBar(state, 0, "informational")
          ),
          MetroInfoBar.build(
            {
              MetroInfoBar.success(
                "Package Verified",
                "All 21 Metro accents and typography ramps passed validation."
              ) with
                  Open = isBarOpen(state.InfoOpenMask, 1)
            },
            function
            | MetroInfoBarEvent.Closed -> closeBar(state, 1, "success")
            | MetroInfoBarEvent.ActionInvoked -> ()
          ),
          MetroInfoBar.build(
            {
              MetroInfoBar.warning(
                "Metered Connection",
                "Live tile background refresh is throttled on cellular data."
              ) with
                  Open = isBarOpen(state.InfoOpenMask, 2)
            },
            function
            | MetroInfoBarEvent.Closed -> closeBar(state, 2, "warning")
            | MetroInfoBarEvent.ActionInvoked -> ()
          ),
          MetroInfoBar.build(
            {
              MetroInfoBar.error(
                "Shader Cache Offline",
                "Falling back to 2D Metro motion primitives."
              ) with
                  Open = isBarOpen(state.InfoOpenMask, 3)
            },
            function
            | MetroInfoBarEvent.Closed -> closeBar(state, 3, "error")
            | MetroInfoBarEvent.ActionInvoked -> ()
          ),
          Container(FlexDirection = FlexDirection.Row)
            .Children(
              MetroButton.standard(
                "Restore Info Bars",
                fun () ->
                  state.InfoOpenMask <- 0b1111
                  state.LastAction <- "Restored all info bars"
              )
            )
        )
    )

  let tipBtn =
    MetroButton.standard(
      "Toggle Teaching Tip",
      fun () ->
        state.TipOpen <- not state.TipOpen

        state.LastAction <-
          (if state.TipOpen then
             "Teaching tip opened"
           else
             "Teaching tip closed")
    )

  tipBtn.Handle <- state.TipAnchor

  let previewToast = {
    MetroToast.titledItem(
      "preview",
      "Build Succeeded",
      "4 px top severity bar, sharp 0 px geometry, and optional action."
    ) with
        Severity = MetroInfoBarSeverity.Success
        ActionText = ValueSome "Open Log"
        DurationMs = ValueNone
  }

  let previewTipProps = {
    MetroTeachingTip.defaults("Swipe Between Sections", true) with
        Subtitle = ValueSome "Gesture & keyboard navigation"
        Body =
          ValueSome
            "Use Left and Right arrow keys or horizontal drag to move across Pivot and Panorama sections."
        ActionText = ValueSome "Got It"
  }

  let anchoredTipProps = {
    MetroTeachingTip.anchored(
      state.TipAnchor,
      "Contextual Guidance",
      state.TipOpen
    ) with
        Subtitle = ValueSome "Anchored Portal callout"
        Body =
          ValueSome
            "Press Escape, click outside, or activate Got It to dismiss."
        ActionText = ValueSome "Got It"
  }

  let toastCaption = MetroText.caption "TOAST CARD SURFACE"
  toastCaption.Key <- "notifications-toast-caption"

  let tipCaption = MetroText.caption "TEACHING TIP SURFACE"
  tipCaption.Key <- "notifications-tip-caption"

  let toastAndTipCol =
    column(
      "toast & teaching tip",
      Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
        .Children(
          Container(
            Key = "notifications-actions-row",
            FlexDirection = FlexDirection.Row,
            FlexWrap = FlexWrap.Wrap,
            Gap = Length.Pixels 10.0,
            AlignItems = AlignItems.Center
          )
            .Children(
              MetroButton.accent("Push Toast", fun () -> pushToast state),
              MetroButton.standard(
                "Clear Toasts",
                fun () ->
                  state.Toasts <- Array.empty
                  state.LastAction <- "Cleared toasts"
              ),
              tipBtn
            ),
          toastCaption,
          MetroToast.buildCard(
            previewToast,
            function
            | MetroToastEvent.ActionInvoked _ ->
              state.LastAction <- "Preview toast action: Open Log"
            | MetroToastEvent.Dismissed _ ->
              state.LastAction <- "Preview toast close clicked"
          ),
          tipCaption,
          MetroTeachingTip.buildSurface(
            previewTipProps,
            function
            | MetroTeachingTipEvent.ActionInvoked ->
              state.LastAction <- "Preview tip action: Got It"
            | MetroTeachingTipEvent.Closed ->
              state.LastAction <- "Preview tip close clicked"
          ),
          MetroToast.mountHost(
            "gallery-toast-host",
            MetroToast.hostDefaults state.Toasts,
            factories.ToastHost
          ),
          MetroTeachingTip.mount(
            "gallery-teaching-tip",
            anchoredTipProps,
            factories.TeachingTip
          )
        )
    )

  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    Width = Length.Percent 100.0,
    Gap = Length.Pixels 16.0
  )
    .Children(
      MetroText.secondary
        "Inline severity banners with logical start stripes, stacked top-edge \
         toast notifications with Window.IsOpen-guarded timers, and 3 px \
         accent-topped teaching-tip callouts.",
      Container(
        FlexDirection = FlexDirection.Row,
        Width = Length.Percent 100.0,
        FlexShrink = 0.0,
        Gap = Length.Pixels 32.0
      )
        .Children(infoCol, toastAndTipCol)
    )

/// The page: the title, the surfaces, the notices, and the last action. The
/// shell keeps the content dialog and the settings flyout.
let view(state: DialogsState, factories: DialogsFactories) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
    Gap = Length.Pixels 24.0,
    Padding =
      Edges(
        Length.Pixels 4.0,
        Length.Pixels 12.0,
        Length.Pixels 24.0,
        Length.Pixels 0.0
      )
  )
    .Children(
      MetroText.title "dialogs",
      section("SURFACES", surfaces(state, factories)),
      section("NOTICES", notices(state, factories)),
      MetroText.caption state.LastAction
    )

/// Creates the dialogs page. The page owns the open states, the toasts, and
/// the tip state. Callbacks of mounted cells write the state, and the owner
/// rule rebuilds the page; callbacks made before the page exists rebuild it
/// through the slot.
let create() : DialogsPageCell =
  let state: DialogsState = {
    FlyoutAnchor = ElementHandle()
    MenuAnchor = ElementHandle()
    TipAnchor = ElementHandle()
    FlyoutOpen = false
    MessageOpen = false
    MenuOpen = false
    ContextOpen = false
    ContextPoint = Point(X = 240.0, Y = 220.0)
    PinnedTooltip = true
    LastMenuInput = Unchecked.defaultof<MenuInput>
    LastContextInput = Unchecked.defaultof<MenuInput>
    InfoOpenMask = 0b1111
    Toasts = Array.empty
    NextToastId = 1
    TipOpen = false
    LastAction = "Ready"
    Builds = 0
  }

  let mutable self: DialogsPageCell voption = ValueNone
  let mutable msgRef: MetroMessageDialogCell voption = ValueNone
  let mutable tooltipRef: MetroTooltipCell voption = ValueNone
  let mutable toastHostRef: MetroToastHostCell voption = ValueNone
  let mutable tipRef: MetroTeachingTipCell voption = ValueNone

  // Flyout renderContent callbacks belong to MetroFlyoutCell, so mutating
  // host state from inside flyoutBody rebuilds this page through `self`.
  let onQuickApply() =
    state.FlyoutOpen <- false
    state.LastAction <- "Applied flyout filter"
    self |> ValueOption.iter(fun page -> page.Rebuild())

  let factories: DialogsFactories = {
    Flyout =
      fun () ->
        MetroFlyout.create(
          (fun () ->
            state.FlyoutOpen <- false
            state.LastAction <- "Flyout dismissed"),
          fun () -> flyoutBody onQuickApply
        )
    MessageDialog =
      fun () ->
        let c =
          MetroMessageDialog.create(function
            | MetroMessageDialogResult.CommandInvoked id ->
              state.MessageOpen <- false
              state.LastAction <- $"Message command: {id}"
            | MetroMessageDialogResult.Dismissed ->
              state.MessageOpen <- false
              state.LastAction <- "Message dismissed")

        msgRef <- ValueSome c
        c
    MenuFlyout =
      MetroMenuFlyout.createFactory(function
        | MetroMenuEvent.ItemClicked id ->
          state.MenuOpen <- false
          state.LastAction <- $"Menu item: {id}"
        | MetroMenuEvent.OpenChanged isOpen -> state.MenuOpen <- isOpen)
    ContextMenu =
      MetroContextMenu.createFactory(function
        | MetroMenuEvent.ItemClicked id ->
          state.ContextOpen <- false
          state.LastAction <- $"Context item: {id}"
        | MetroMenuEvent.OpenChanged isOpen -> state.ContextOpen <- isOpen)
    Tooltip =
      fun () ->
        let c = MetroTooltip.create()
        tooltipRef <- ValueSome c
        c
    ToastHost =
      fun () ->
        let c =
          MetroToast.createHost(function
            | MetroToastEvent.Dismissed id ->
              state.Toasts <- state.Toasts |> Array.filter(fun t -> t.Id <> id)
              state.LastAction <- $"Dismissed {id}"
            | MetroToastEvent.ActionInvoked id ->
              state.Toasts <- state.Toasts |> Array.filter(fun t -> t.Id <> id)
              state.LastAction <- $"Toast action: {id}")

        toastHostRef <- ValueSome c
        c
    TeachingTip =
      fun () ->
        let c =
          MetroTeachingTip.create(function
            | MetroTeachingTipEvent.Closed ->
              state.TipOpen <- false
              state.LastAction <- "Teaching tip dismissed"
            | MetroTeachingTipEvent.ActionInvoked ->
              state.TipOpen <- false
              state.LastAction <- "Teaching tip action: Got It")

        tipRef <- ValueSome c
        c
  }

  let page = {
    new DialogsPageCell() with
      override _.MessageDialog = msgRef
      override _.Tooltip = tooltipRef
      override _.MenuInput = state.LastMenuInput
      override _.ContextMenuInput = state.LastContextInput
      override _.ToastHost = toastHostRef
      override _.TeachingTip = tipRef
      override _.InfoOpenMask = state.InfoOpenMask
      override _.ToastCount = state.Toasts.Length
      override _.TipOpen = state.TipOpen
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.PushSampleToast() =
        pushToast state
        self |> ValueOption.iter(fun page -> page.Rebuild())

      override _.SetTipOpen(isOpen) =
        state.TipOpen <- isOpen
        self |> ValueOption.iter(fun page -> page.Rebuild())

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, factories)
  }

  self <- ValueSome page
  page
