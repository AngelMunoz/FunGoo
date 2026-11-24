module MetroGallery.NotificationsTab

open Goo
open FunGoo.Children
open FunGoo.Metro

[<AbstractClass>]
type NotificationsTabCell() =
  inherit Cell()

  abstract ToastHost: MetroToastHostCell voption
  abstract TeachingTip: MetroTeachingTipCell voption
  abstract InfoOpenMask: int
  abstract ToastCount: int
  abstract TipOpen: bool
  abstract LastAction: string
  abstract Builds: int
  abstract PushSampleToast: unit -> unit
  abstract SetTipOpen: isOpen: bool -> unit

type NotificationsState = {
  TipAnchor: ElementHandle
  mutable InfoOpenMask: int
  mutable Toasts: MetroToastItem array
  mutable NextToastId: int
  mutable TipOpen: bool
  mutable LastAction: string
  mutable Builds: int
}

type NotificationsFactories = {
  ToastHost: unit -> MetroToastHostCell
  TeachingTip: unit -> MetroTeachingTipCell
}

let column(title: string, content: Blob) : Blob =
  Container(
    FlexDirection = FlexDirection.Column,
    FlexGrow = 1.0,
    FlexBasis = Length.Pixels 0.0,
    MinWidth = Length.Pixels 0.0,
    Gap = Length.Pixels 12.0
  )
    .Children(MetroText.subheader title, content)

let isBarOpen(mask: int, bit: int) : bool = (mask &&& (1 <<< bit)) <> 0

let closeBar(state: NotificationsState, bit: int, name: string) : unit =
  state.InfoOpenMask <- state.InfoOpenMask &&& ~~~(1 <<< bit)
  state.LastAction <- $"Closed {name} info bar"

let pushToast(state: NotificationsState) : unit =
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

let view(state: NotificationsState, factories: NotificationsFactories) : Blob =
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
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
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
        .Children(infoCol, toastAndTipCol),
      MetroText.caption state.LastAction
    )

let create() : NotificationsTabCell =
  let state: NotificationsState = {
    TipAnchor = ElementHandle()
    InfoOpenMask = 0b1111
    Toasts = Array.empty
    NextToastId = 1
    TipOpen = false
    LastAction = "Ready"
    Builds = 0
  }

  let mutable self: NotificationsTabCell voption = ValueNone
  let mutable toastHostRef: MetroToastHostCell voption = ValueNone
  let mutable tipRef: MetroTeachingTipCell voption = ValueNone

  let factories: NotificationsFactories = {
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

  let cell = {
    new NotificationsTabCell() with
      override _.ToastHost = toastHostRef
      override _.TeachingTip = tipRef
      override _.InfoOpenMask = state.InfoOpenMask
      override _.ToastCount = state.Toasts.Length
      override _.TipOpen = state.TipOpen
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.PushSampleToast() =
        pushToast state
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.SetTipOpen(isOpen) =
        state.TipOpen <- isOpen
        self |> ValueOption.iter(fun tab -> tab.Rebuild())

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, factories)
  }

  self <- ValueSome cell
  cell
