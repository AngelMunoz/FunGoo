module MetroGallery.OverlaysTab

open Goo
open Goo.Widgets.Navigation
open FunGoo.Children
open FunGoo.Metro

[<AbstractClass>]
type OverlaysTabCell() =
  inherit Cell()

  abstract MessageDialog: MetroMessageDialogCell voption
  abstract Tooltip: MetroTooltipCell voption
  abstract MenuInput: MenuInput
  abstract ContextMenuInput: MenuInput
  abstract LastAction: string
  abstract Builds: int

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

type OverlaysState = {
  FlyoutAnchor: ElementHandle
  MenuAnchor: ElementHandle
  mutable FlyoutOpen: bool
  mutable MessageOpen: bool
  mutable MenuOpen: bool
  mutable ContextOpen: bool
  mutable ContextPoint: Point
  mutable PinnedTooltip: bool
  mutable LastMenuInput: MenuInput
  mutable LastContextInput: MenuInput
  mutable LastAction: string
  mutable Builds: int
}

type OverlaysFactories = {
  Flyout: unit -> MetroFlyoutCell
  MessageDialog: unit -> MetroMessageDialogCell
  MenuFlyout: IMetroMenuFlyoutFactory
  ContextMenu: IMetroContextMenuFactory
  Tooltip: unit -> MetroTooltipCell
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

let flyoutBody(onQuickApply: unit -> unit) : Blob =
  Container(FlexDirection = FlexDirection.Column, Gap = Length.Pixels 12.0)
    .Children(
      MetroText.subtitle "Quick Filter",
      MetroText.secondary
        "Anchored Portal flyout with 2 px border and Escape or outside-click light dismiss.",
      MetroButton.accent("Apply Filter", onQuickApply)
    )

let view(state: OverlaysState, factories: OverlaysFactories) : Blob =
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
        BorderWidth = Edges(Length.Pixels 2.0),
        BorderColor = Edges<Color>(p.BorderStrong),
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
    OverflowY = Overflow.Scroll,
    OverflowX = Overflow.Hidden,
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
        .Children(dialogsCol, menusCol),
      MetroText.caption state.LastAction
    )

let create() : OverlaysTabCell =
  let state: OverlaysState = {
    FlyoutAnchor = ElementHandle()
    MenuAnchor = ElementHandle()
    FlyoutOpen = false
    MessageOpen = false
    MenuOpen = false
    ContextOpen = false
    ContextPoint = Point(X = 240.0, Y = 220.0)
    PinnedTooltip = true
    LastMenuInput = Unchecked.defaultof<MenuInput>
    LastContextInput = Unchecked.defaultof<MenuInput>
    LastAction = "Ready"
    Builds = 0
  }

  let mutable self: OverlaysTabCell voption = ValueNone
  let mutable msgRef: MetroMessageDialogCell voption = ValueNone
  let mutable tooltipRef: MetroTooltipCell voption = ValueNone

  // Flyout renderContent callbacks belong to MetroFlyoutCell, so mutating host
  // state from inside flyoutBody rebuilds this tab through `self`.
  let onQuickApply() =
    state.FlyoutOpen <- false
    state.LastAction <- "Applied flyout filter"
    self |> ValueOption.iter(fun tab -> tab.Rebuild())

  let factories: OverlaysFactories = {
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
  }

  let cell = {
    new OverlaysTabCell() with
      override _.MessageDialog = msgRef
      override _.Tooltip = tooltipRef
      override _.MenuInput = state.LastMenuInput
      override _.ContextMenuInput = state.LastContextInput
      override _.LastAction = state.LastAction
      override _.Builds = state.Builds

      override _.Build() : Blob =
        state.Builds <- state.Builds + 1
        view(state, factories)
  }

  self <- ValueSome cell
  cell
