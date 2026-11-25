namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct>]
type MetroFlyoutPlacement =
  | Top
  | TopStart
  | TopEnd
  | Bottom
  | BottomStart
  | BottomEnd
  | Left
  | Right

[<Struct; NoComparison>]
type MetroFlyoutProps = {
  Open: bool
  Anchor: ElementHandle
  Placement: MetroFlyoutPlacement
  MinWidth: float
  MaxWidth: float voption
  LightDismiss: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroFlyoutCell() =
  inherit Cell<MetroFlyoutProps>()

  abstract IsOpen: bool

  abstract IsVisible: bool

  abstract IsAnimating: bool

  abstract View: Blob

  abstract Dismiss: unit -> unit

  // Gives one dismiss request to the host cell through InvokeInputCallback.
  member this.Raise(callback: Action) = this.InvokeInputCallback(callback)

module FlyoutInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  let toPortalPlacement(placement: MetroFlyoutPlacement) : PortalPlacement =
    match placement with
    | MetroFlyoutPlacement.Top -> PortalPlacement.Top
    | MetroFlyoutPlacement.TopStart -> PortalPlacement.TopStart
    | MetroFlyoutPlacement.TopEnd -> PortalPlacement.TopEnd
    | MetroFlyoutPlacement.Bottom -> PortalPlacement.Bottom
    | MetroFlyoutPlacement.BottomStart -> PortalPlacement.BottomStart
    | MetroFlyoutPlacement.BottomEnd -> PortalPlacement.BottomEnd
    | MetroFlyoutPlacement.Left -> PortalPlacement.Left
    | MetroFlyoutPlacement.Right -> PortalPlacement.Right

  let entryOffset(placement: MetroFlyoutPlacement) : Point =
    match placement with
    | MetroFlyoutPlacement.Top
    | MetroFlyoutPlacement.TopStart
    | MetroFlyoutPlacement.TopEnd -> Point(X = 0.0, Y = 10.0)
    | MetroFlyoutPlacement.Bottom
    | MetroFlyoutPlacement.BottomStart
    | MetroFlyoutPlacement.BottomEnd -> Point(X = 0.0, Y = -10.0)
    | MetroFlyoutPlacement.Left -> Point(X = 10.0, Y = 0.0)
    | MetroFlyoutPlacement.Right -> Point(X = -10.0, Y = 0.0)

  let resolveMinWidth(value: float) : float =
    if Double.IsFinite value && value > 0.0 then
      value
    else
      200.0

  [<Struct; NoComparison>]
  type PanelArgs = {
    Props: MetroFlyoutProps
    Translate: Anim<Point> voption
    Opacity: Anim<float> voption
  }

  let buildPortal
    (args: PanelArgs, onDismiss: unit -> unit, renderContent: unit -> Blob)
    : Blob =
    let props = args.Props
    let theme = Metro.read()
    let p = theme.Palette
    let minWidth = resolveMinWidth props.MinWidth
    let accName = props.AccessibilityName |> ValueOption.defaultValue "Flyout"

    let panel =
      Container(
        Key = "metro-flyout-panel",
        FlexDirection = FlexDirection.Column,
        MinWidth = Length.Pixels minWidth,
        Padding = Edges(Length.Pixels 16.0),
        BorderRadius = zeroRadius,
        BorderWidth = metroBorder,
        BorderColor =
          Edges<Color>(
            if theme.HighContrast then p.Foreground else p.BorderStrong
          ),
        BoxShadows = emptyShadows,
        BackgroundColor = p.BackgroundAlt,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Dialog, Name = accName)
      )

    props.MaxWidth
    |> ValueOption.iter(fun maxW ->
      if Double.IsFinite maxW && maxW >= minWidth then
        panel.MaxWidth <- Length.Pixels maxW)

    args.Translate |> ValueOption.iter(fun anim -> panel.TranslateAnim <- anim)
    args.Opacity |> ValueOption.iter(fun anim -> panel.OpacityAnim <- anim)

    let content = renderContent()

    if not(isNull content) then
      panel.Add(content)

    let portal =
      Portal(
        Key = "metro-flyout-portal",
        Anchor = props.Anchor,
        Placement = toPortalPlacement props.Placement,
        ZIndex = 20,
        Padding = Edges(Length.Pixels 6.0),
        FocusScope = FocusScopeOptions(Modal = false, RestoreFocus = true)
      )

    if props.LightDismiss then
      portal.OnDismiss <- Action<PopupDismissReason>(fun _ -> onDismiss())

    portal.Add(panel)
    portal :> Blob

[<RequireQualifiedAccess>]
module MetroFlyout =

  let toPortalPlacement(placement: MetroFlyoutPlacement) : PortalPlacement =
    FlyoutInternals.toPortalPlacement placement

  let defaults(anchor: ElementHandle, isOpen: bool) : MetroFlyoutProps = {
    Open = isOpen
    Anchor = anchor
    Placement = MetroFlyoutPlacement.BottomStart
    MinWidth = 200.0
    MaxWidth = ValueNone
    LightDismiss = true
    AccessibilityName = ValueNone
  }

  let build
    (
      props: MetroFlyoutProps,
      onDismiss: unit -> unit,
      renderContent: unit -> Blob
    ) : Blob =
    if not props.Open || isNull props.Anchor then
      Container(Key = "metro-flyout-closed", Display = Display.None) :> Blob
    else
      FlyoutInternals.buildPortal(
        {
          Props = props
          Translate = ValueNone
          Opacity = ValueNone
        },
        onDismiss,
        renderContent
      )

  type FlyoutState = {
    mutable Props: MetroFlyoutProps
    mutable LastOpen: bool voption
    mutable Closing: bool
    mutable Requested: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
  }

  type FlyoutContext = {
    State: FlyoutState
    RaiseDismiss: Action
    RenderContent: unit -> Blob
  }

  let initialState() : FlyoutState = {
    Props = defaults(null, false)
    LastOpen = ValueNone
    Closing = false
    Requested = false
    LastReducedMotion = false
    ScopedReducedMotion = false
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  let snapOpen(state: FlyoutState) : unit =
    state.Closing <- false
    state.Translate.Snap(FlyoutInternals.zeroPoint)
    state.Opacity.Snap(1.0)

  let snapClosed(state: FlyoutState) : unit =
    state.Closing <- false
    state.Translate.Snap(FlyoutInternals.zeroPoint)
    state.Opacity.Snap(0.0)

  let isVisible(state: FlyoutState) : bool =
    not(isNull state.Props.Anchor) && (state.Props.Open || state.Closing)

  let view(ctx: FlyoutContext) : Blob =
    let state = ctx.State

    if not(isVisible state) then
      Container(Key = "metro-flyout-closed", Display = Display.None) :> Blob
    else
      FlyoutInternals.buildPortal(
        {
          Props = state.Props
          Translate = ValueSome state.Translate
          Opacity = ValueSome state.Opacity
        },
        (fun () -> ctx.RaiseDismiss.Invoke()),
        ctx.RenderContent
      )

  let inputChanged
    (ctx: FlyoutContext, input: MetroFlyoutProps, hostReduced: bool voption)
    : unit =
    let state = ctx.State
    state.Props <- input
    state.Requested <- false
    let nextOpen = input.Open && not(isNull input.Anchor)

    let reduced =
      state.ScopedReducedMotion
      || ValueOption.defaultValue state.LastReducedMotion hostReduced

    let changed =
      state.LastOpen
      |> ValueOption.map(fun prev -> prev <> nextOpen)
      |> ValueOption.defaultValue false

    if changed then
      let offset = FlyoutInternals.entryOffset input.Placement

      if nextOpen then
        let wasClosing = state.Closing
        state.Closing <- false

        if reduced then
          snapOpen state
        elif wasClosing then
          // A reopen during the close animation continues from the current
          // value; Play would snap to the start of the fade.
          state.Translate.To(FlyoutInternals.zeroPoint, MetroMotion.normal)
          state.Opacity.To(1.0, MetroMotion.normal)
        else
          state.Translate.Play(
            offset,
            FlyoutInternals.zeroPoint,
            MetroMotion.normal
          )

          state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
      elif reduced then
        snapClosed state
      else
        state.Closing <- true
        state.Translate.To(offset, MetroMotion.normal)
        state.Opacity.To(0.0, MetroMotion.normal)
    elif state.LastOpen.IsNone then
      if nextOpen then snapOpen state else snapClosed state

    state.LastOpen <- ValueSome nextOpen

  let unmount(state: FlyoutState) : unit =
    state.LastOpen <- ValueNone
    state.ScopedReducedMotion <- false
    snapClosed state

  let buildCell
    (ctx: FlyoutContext, input: MetroFlyoutProps, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Props <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      if input.Open && not(isNull input.Anchor) then
        snapOpen state
      else
        snapClosed state
    elif state.LastOpen.IsNone then
      inputChanged(ctx, input, hostReduced)

    view ctx

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let create
    (onDismiss: unit -> unit, renderContent: unit -> Blob)
    : MetroFlyoutCell =
    let state = initialState()
    let mutable self: MetroFlyoutCell voption = ValueNone
    let onDismissAction = Action(onDismiss)

    let requestDismiss() =
      if not state.Closing && not state.Requested then
        state.Requested <- true

        self |> ValueOption.iter(fun cell -> cell.Raise onDismissAction)

    let raiseDismiss = Action(requestDismiss)

    let context: FlyoutContext = {
      State = state
      RaiseDismiss = raiseDismiss
      RenderContent = renderContent
    }

    let cell = {
      new MetroFlyoutCell() with
        override _.IsOpen = state.Props.Open
        override _.IsVisible = isVisible state

        override _.IsAnimating =
          state.Translate.Running || state.Opacity.Running

        override _.View = view context
        override _.Dismiss() = requestDismiss()

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          buildCell(context, input, reducedMotionOf this.Host)
    }

    state.Translate <- cell.AnimateValue(FlyoutInternals.zeroPoint)
    state.Opacity <- cell.AnimateValue(0.0)

    // Unmounting the portal after the close fade finishes runs from the motion
    // pump, so this cell rebuilds itself once.
    state.Opacity.add_Completed(
      Action<MotionCompletionReason>(fun _ ->
        if not state.Props.Open && state.Closing then
          state.Closing <- false
          cell.Rebuild())
    )

    self <- ValueSome cell
    cell

  let createFactory
    (onDismiss: unit -> unit, renderContent: unit -> Blob)
    : unit -> MetroFlyoutCell =
    fun () -> create(onDismiss, renderContent)

  let mount
    (key: string, input: MetroFlyoutProps, factory: unit -> MetroFlyoutCell)
    : Blob =
    Cell.Mount<MetroFlyoutProps, MetroFlyoutCell>(key, input, factory)
