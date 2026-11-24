namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroTooltipInput = {
  Target: Blob
  Text: string
  Placement: MetroFlyoutPlacement
  Open: bool voption
  DelayMs: float
  Disabled: bool
}

[<AbstractClass>]
type MetroTooltipCell() =
  inherit Cell<MetroTooltipInput>()

  abstract IsVisible: bool

  abstract View: Blob

  // Exposes protected Cell.After and Cell.Host.IsOpen to the module functions
  // without FS0491.
  member internal this.Schedule(delayMs: float, action: Action) : WindowTimer =
    this.After(delayMs, action)

  member internal this.IsHostOpen: bool =
    not(isNull this.Host) && this.Host.IsOpen

module TooltipInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)

  [<Literal>]
  let defaultDelayMs = 400.0

[<RequireQualifiedAccess>]
module MetroTooltip =

  let defaultDelayMs = TooltipInternals.defaultDelayMs

  let defaults(target: Blob, text: string) : MetroTooltipInput = {
    Target = target
    Text = text
    Placement = MetroFlyoutPlacement.Top
    Open = ValueNone
    DelayMs = defaultDelayMs
    Disabled = false
  }

  type TooltipState = {
    TargetHandle: ElementHandle
    mutable Input: MetroTooltipInput
    mutable Hovered: bool
    mutable Focused: bool
    mutable Shown: bool
    mutable DelaySubscription: WindowTimer voption
    mutable LastReducedMotion: bool
    mutable Opacity: Anim<float>
  }

  type TooltipDelegates = {
    PointerEnter: Action<PointerEvent>
    PointerLeave: Action<PointerEvent>
    Focus: Action<FocusEvent>
    Blur: Action<FocusEvent>
  }

  let cancelTimer(state: TooltipState) : unit =
    state.DelaySubscription |> ValueOption.iter(fun sub -> sub.Dispose())

    state.DelaySubscription <- ValueNone

  let isVisible(state: TooltipState) : bool =
    let input = state.Input

    if input.Disabled || String.IsNullOrWhiteSpace input.Text then
      false
    else
      match input.Open with
      | ValueSome explicitOpen -> explicitOpen
      | ValueNone -> state.Shown

  let showNow(state: TooltipState) : unit =
    let input = state.Input

    if
      input.Open.IsNone
      && not input.Disabled
      && (state.Hovered || state.Focused)
    then
      cancelTimer state

      // A fade from 0.0 while the tooltip is already on screen would flicker.
      if not state.Shown then
        state.Shown <- true

        if state.LastReducedMotion then
          state.Opacity.Snap(1.0)
        else
          state.Opacity.Play(0.0, 1.0, MetroMotion.fast)

  let hideNow(state: TooltipState) : unit =
    cancelTimer state
    state.Shown <- false
    state.Opacity.Snap(0.0)

  let hideWhenUntriggered(state: TooltipState) : unit =
    if not(state.Hovered || state.Focused) then
      hideNow state

  let hideForDisabled(state: TooltipState) : unit =
    state.Hovered <- false
    state.Focused <- false
    hideNow state

  let applyControlledOpen
    (
      previousInput: MetroTooltipInput,
      input: MetroTooltipInput,
      state: TooltipState
    ) : unit =
    match input.Open with
    | ValueSome isOpen ->
      // A controlled input cancels a pending hover timer and owns Shown.
      cancelTimer state
      state.Shown <- isOpen
      state.Opacity.Snap(if isOpen then 1.0 else 0.0)

      // A controlled hidden state consumes the hover or focus that asked for
      // the show, so a later release cannot reshow the tooltip.
      if not isOpen then
        state.Hovered <- false
        state.Focused <- false
    | ValueNone when previousInput.Open.IsSome ->
      // Leaving controlled mode follows the trigger that is still active.
      if state.Hovered || state.Focused then
        showNow state
      else
        state.Shown <- false
    | ValueNone -> ()

  let buildView(state: TooltipState, delegates: TooltipDelegates) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let input = state.Input
    let visible = isVisible state

    let targetWrapper =
      Container(
        Key = "metro-tooltip-target",
        Handle = state.TargetHandle,
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        FlexShrink = 0.0,
        OnPointerEnter = delegates.PointerEnter,
        OnPointerLeave = delegates.PointerLeave,
        OnFocus = delegates.Focus,
        OnBlur = delegates.Blur
      )

    if not(isNull input.Target) then
      targetWrapper.Add(input.Target)

    let root =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        FlexShrink = 0.0
      )
        .Children(targetWrapper)

    if visible then
      let text = if isNull input.Text then "" else input.Text

      let bubble =
        Container(
          Key = "metro-tooltip-bubble",
          Padding = Edges(Length.Pixels 6.0, Length.Pixels 10.0),
          BorderRadius = TooltipInternals.zeroRadius,
          BorderWidth = TooltipInternals.metroBorder,
          BorderColor =
            Edges<Color>(
              if theme.HighContrast then p.Foreground else p.BorderStrong
            ),
          BoxShadows = TooltipInternals.emptyShadows,
          BackgroundColor = p.BackgroundAlt,
          OpacityAnim = state.Opacity,
          HitTestSelf = false,
          Focusable = false,
          TabStop = false,
          Accessibility =
            Accessibility(Role = AccessibilityRole.Tooltip, Name = text)
        )
          .Children(
            Text(
              Content = text,
              Color = p.Foreground,
              FontFamily = theme.FontFamily,
              FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
              FontWeight = 400.0,
              TextWrap = TextWrap.NoWrap
            )
          )

      let portal =
        Portal(
          Key = "metro-tooltip-portal",
          Anchor = state.TargetHandle,
          Placement = MetroFlyout.toPortalPlacement input.Placement,
          ZIndex = 30,
          Padding = Edges(Length.Pixels 6.0)
        )

      portal.Add(bubble)
      root.Add(portal)

    root :> Blob

  let create() : MetroTooltipCell =
    let state: TooltipState = {
      TargetHandle = ElementHandle()
      Input = defaults(null, "")
      Hovered = false
      Focused = false
      Shown = false
      DelaySubscription = ValueNone
      LastReducedMotion = false
      Opacity = Unchecked.defaultof<Anim<float>>
    }

    let mutable self: MetroTooltipCell voption = ValueNone

    let requestHoverShow() =
      if state.Input.Open.IsNone && not state.Input.Disabled then
        cancelTimer state
        let delay = state.Input.DelayMs

        let canUseTimer =
          self
          |> ValueOption.map(fun cell ->
            cell.IsHostOpen && Double.IsFinite delay && delay > 0.0)
          |> ValueOption.defaultValue false

        if canUseTimer then
          self
          |> ValueOption.iter(fun cell ->
            // Goo rebuilds the owning cell automatically after a timer action
            // runs (hard fact 15).
            let sub =
              cell.Schedule(
                delay,
                Action(fun () ->
                  state.DelaySubscription <- ValueNone
                  showNow state)
              )

            state.DelaySubscription <- ValueSome sub)
        else
          showNow state

    let delegates: TooltipDelegates = {
      PointerEnter =
        Action<PointerEvent>(fun _ ->
          state.Hovered <- true
          requestHoverShow())
      PointerLeave =
        Action<PointerEvent>(fun _ ->
          state.Hovered <- false

          if state.Input.Open.IsNone then
            hideWhenUntriggered state)
      Focus =
        Action<FocusEvent>(fun _ ->
          state.Focused <- true

          if state.Input.Open.IsNone && not state.Input.Disabled then
            showNow state)
      Blur =
        Action<FocusEvent>(fun _ ->
          state.Focused <- false

          if state.Input.Open.IsNone then
            hideWhenUntriggered state)
    }

    let cell = {
      new MetroTooltipCell() with
        override _.IsVisible = isVisible state
        override _.View = buildView(state, delegates)

        override _.InputChanged(input) =
          let previousInput = state.Input
          state.Input <- input

          if input.Disabled then
            hideForDisabled state
          else
            applyControlledOpen(previousInput, input, state)

        override _.Unmounting() = hideNow state

        override _.Build(input) : Blob =
          let previousInput = state.Input
          state.Input <- input
          let theme = Metro.read()
          state.LastReducedMotion <- theme.ReducedMotion

          if input.Disabled then
            hideForDisabled state
          else
            applyControlledOpen(previousInput, input, state)

          buildView(state, delegates)
    }

    state.Opacity <- cell.AnimateValue(0.0)
    self <- ValueSome cell
    cell

  let defaultFactory() : MetroTooltipCell = create()

  let mountInput(key: string, input: MetroTooltipInput) : Blob =
    Cell.Mount<MetroTooltipInput, MetroTooltipCell>(key, input, defaultFactory)

  let mount(key: string, target: Blob, text: string) : Blob =
    mountInput(key, defaults(target, text))

  let mountWith
    (key: string, input: MetroTooltipInput, factory: unit -> MetroTooltipCell)
    : Blob =
    Cell.Mount<MetroTooltipInput, MetroTooltipCell>(key, input, factory)
