namespace FunGoo.Metro

open System
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct; NoComparison>]
type MetroFlipViewItem = {
  Id: string
  AccessibilityName: string
}

[<Struct; NoComparison>]
type MetroFlipViewInput = {
  Items: MetroFlipViewItem array
  SelectedId: string
  ShowNavigation: bool
  ShowIndicators: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroFlipViewEvent =
  | Navigate of index: int
  | PointerDown of pointer: PointerEvent
  | PointerMoved of pointer: PointerEvent
  | PointerUp of pointer: PointerEvent
  | PointerCanceled of pointer: PointerEvent

[<Struct; NoComparison>]
type MetroFlipViewState = {
  Input: MetroFlipViewInput
  ActiveIndex: int
  OutgoingId: string voption
  Direction: float
  TrackTranslate: Anim<Point>
  ViewportHandle: ElementHandle
}

module private FlipInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let hcBorder = Edges(Length.Pixels 2.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPadding = Edges(Length.Pixels 0.0)
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  [<Literal>]
  let swipeThresholdPx = 48.0

  // Used until the viewport has a measured width.
  [<Literal>]
  let fallbackSlidePx = 320.0

  // Delay between checks for a track animation that outlives its timer.
  [<Literal>]
  let settlePollMs = 16.0

  [<Literal>]
  let outgoingKey = "flip-outgoing"

  [<Literal>]
  let activeKey = "flip-active"

  [<Literal>]
  let previousKey = "flip-previous"

  [<Literal>]
  let nextKey = "flip-next"

  [<Literal>]
  let indicatorsKey = "flip-indicators"

  let itemsOf(input: MetroFlipViewInput) : MetroFlipViewItem array =
    if isNull input.Items then Array.empty else input.Items

  let tryIndexOf
    (items: MetroFlipViewItem array, selectedId: string)
    : int voption =
    if isNull items || isNull selectedId then
      ValueNone
    else
      let mutable found = ValueNone
      let mutable i = 0

      while i < items.Length && found.IsNone do
        if items[i].Id = selectedId then
          found <- ValueSome i

        i <- i + 1

      found

  let indexOf(items: MetroFlipViewItem array, selectedId: string) : int =
    if isNull items || items.Length = 0 then
      -1
    else
      tryIndexOf(items, selectedId) |> ValueOption.defaultValue 0

  let buildView
    (
      state: MetroFlipViewState,
      renderItem: string -> Blob,
      onEvent: MetroFlipViewEvent -> unit
    ) : Blob =
    if isNull state.TrackTranslate then
      raise(ArgumentNullException(nameof state))

    let theme = Metro.read()
    let p = theme.Palette
    let input = state.Input
    let items = itemsOf input
    let count = items.Length
    let activeIndex = state.ActiveIndex
    let hasActive = activeIndex >= 0 && activeIndex < count
    let showChrome = count > 1
    let transitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs)

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let hoverStyle = Style(BackgroundColor = p.Hover)
    let disabledOpacity = if theme.HighContrast then 1.0 else 0.4

    let itemAt(index: int) : MetroFlipViewItem voption =
      if index >= 0 && index < count then
        ValueSome items[index]
      else
        ValueNone

    let itemName(item: MetroFlipViewItem) =
      if String.IsNullOrEmpty item.AccessibilityName then
        item.Id
      else
        item.AccessibilityName

    let navigate(targetIdx: int) =
      if targetIdx >= 0 && targetIdx < count && targetIdx <> activeIndex then
        onEvent(MetroFlipViewEvent.Navigate targetIdx)

    let stepBy(delta: int) = navigate(activeIndex + delta)

    let keyBindings = [|
      KeyBinding(Key.Left, KeyModifiers.None, fun () -> stepBy -1)
      KeyBinding(Key.Right, KeyModifiers.None, fun () -> stepBy 1)
      KeyBinding(Key.PageUp, KeyModifiers.None, fun () -> stepBy -1)
      KeyBinding(Key.PageDown, KeyModifiers.None, fun () -> stepBy 1)
      KeyBinding(Key.Home, KeyModifiers.None, fun () -> navigate 0)
      KeyBinding(Key.End, KeyModifiers.None, fun () -> navigate(count - 1))
    |]

    let panel(key: string, item: MetroFlipViewItem, isSelected: bool) : Blob =
      Container(
        Key = key,
        FlexDirection = FlexDirection.Column,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        FlexShrink = 0.0,
        Overflow = Overflow.Hidden,
        TranslateAnim = state.TrackTranslate,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.ListItem,
            Name = itemName item,
            Selected = Nullable isSelected
          )
      )
        .Children(renderItem item.Id)

    let outgoingItem =
      state.OutgoingId
      |> ValueOption.bind(fun id ->
        tryIndexOf(items, id) |> ValueOption.map(fun i -> items[i]))

    let activeItem = if hasActive then itemAt activeIndex else ValueNone

    let viewport =
      Container(
        Handle = state.ViewportHandle,
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.NoWrap,
        AlignItems = AlignItems.Stretch,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        OnPointerDown = (fun e -> onEvent(MetroFlipViewEvent.PointerDown e)),
        OnPointerMove = (fun e -> onEvent(MetroFlipViewEvent.PointerMoved e)),
        OnPointerUp = (fun e -> onEvent(MetroFlipViewEvent.PointerUp e)),
        OnPointerCancel =
          (fun e -> onEvent(MetroFlipViewEvent.PointerCanceled e)),
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.List,
            Name =
              (input.AccessibilityName |> ValueOption.defaultValue "Flip view")
          )
      )

    // A forward slide keeps the outgoing panel first. A backward slide puts the
    // incoming panel first, so it starts one viewport to the left.
    match outgoingItem, activeItem with
    | ValueSome outgoing, ValueSome active ->
      if state.Direction >= 0.0 then
        viewport.Add(panel(outgoingKey, outgoing, false))
        viewport.Add(panel(activeKey, active, true))
      else
        viewport.Add(panel(activeKey, active, true))
        viewport.Add(panel(outgoingKey, outgoing, false))
    | ValueSome outgoing, ValueNone ->
      viewport.Add(panel(outgoingKey, outgoing, false))
    | ValueNone, ValueSome active ->
      viewport.Add(panel(activeKey, active, true))
    | ValueNone, ValueNone -> ()

    if showChrome && input.ShowNavigation then
      let navButton
        (
          key: string,
          iconName: string,
          label: string,
          targetIdx: int,
          isStart: bool
        ) : Blob =
        let enabled = hasActive && targetIdx >= 0 && targetIdx < count

        let borderWidth = if theme.HighContrast then hcBorder else zeroBorder

        let borderColor =
          if theme.HighContrast then
            Edges<Color>(p.Foreground)
          else
            transparentBorder

        let iconColor = if enabled then p.Foreground else p.ForegroundDisabled

        let icon = MaterialIcons.Create(iconName, 20.0, Nullable iconColor)

        let button =
          Button(
            Key = key,
            Position = PositionType.Absolute,
            Top = Length.Percent 50.0,
            Margin = Edges(Length.Pixels -20.0, Length.Pixels 0.0),
            Width = Length.Pixels 40.0,
            Height = Length.Pixels 40.0,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Padding = zeroPadding,
            BorderRadius = zeroRadius,
            BorderWidth = borderWidth,
            BorderColor = borderColor,
            BoxShadows = emptyShadows,
            BackgroundColor = p.Highlight,
            HoverStyle = hoverStyle,
            ActiveStyle = Style(BackgroundColor = theme.Accent.Base),
            DisabledStyle = Style(Opacity = disabledOpacity),
            FocusStyle = focusStyle,
            TransitionMs = transitionMs,
            TransitionEasing = Easing.EaseOut,
            Cursor = (if enabled then Cursor.Pointer else Cursor.Default),
            Focusable = enabled,
            TabStop = enabled,
            Disabled = not enabled,
            Accessibility =
              Accessibility(Role = AccessibilityRole.Button, Name = label)
          )
            .Children(icon)

        if isStart then
          button.Start <- Length.Pixels 0.0
        else
          button.End <- Length.Pixels 0.0

        if enabled then
          button.OnClick <- fun () -> navigate targetIdx

        button

      viewport.Add(
        navButton(
          previousKey,
          "chevron_left",
          "Previous",
          activeIndex - 1,
          true
        )
      )

      viewport.Add(
        navButton(nextKey, "chevron_right", "Next", activeIndex + 1, false)
      )

    if showChrome && input.ShowIndicators then
      let dots =
        Container(
          Key = indicatorsKey,
          Position = PositionType.Absolute,
          Bottom = Length.Pixels 12.0,
          Start = Length.Pixels 0.0,
          End = Length.Pixels 0.0,
          FlexDirection = FlexDirection.Row,
          JustifyContent = JustifyContent.Center,
          AlignItems = AlignItems.Center,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.TabList,
              Name = "Flip view pages",
              Orientation = AccessibilityOrientation.Horizontal
            )
        )

      for i = 0 to count - 1 do
        let isActive = i = activeIndex
        // The active dot is 1.25x the resting dot, inside a 24 px target.
        let size = if isActive then 10.0 else 8.0

        let dot =
          Container(
            Width = Length.Pixels size,
            Height = Length.Pixels size,
            BorderRadius = Length.Pixels(size * 0.5),
            BackgroundColor = p.Foreground,
            Opacity = (if isActive || theme.HighContrast then 1.0 else 0.4)
          )

        let name =
          itemAt i
          |> ValueOption.map itemName
          |> ValueOption.defaultValue $"Item {i + 1}"

        dots.Add(
          Button(
            Key = $"flip-indicator-{i}",
            Width = Length.Pixels 24.0,
            Height = Length.Pixels 24.0,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Padding = zeroPadding,
            BorderRadius = zeroRadius,
            BorderWidth = zeroBorder,
            BorderColor = transparentBorder,
            BoxShadows = emptyShadows,
            BackgroundColor = Color.Transparent,
            HoverStyle = hoverStyle,
            FocusStyle = focusStyle,
            TransitionMs = transitionMs,
            TransitionEasing = Easing.EaseOut,
            Cursor = Cursor.Pointer,
            Focusable = true,
            TabStop = true,
            OnClick = (fun () -> navigate i),
            Accessibility =
              Accessibility(
                Role = AccessibilityRole.Tab,
                Name = name,
                Selected = Nullable isActive
              )
          )
            .Children(dot)
        )

      viewport.Add(dots)

    Container(
      FlexDirection = FlexDirection.Column,
      FlexGrow = 1.0,
      Width = Length.Percent 100.0,
      Overflow = Overflow.Hidden,
      KeyBindings = keyBindings
    )
      .Children(viewport)

[<Sealed>]
type MetroFlipViewCell(onSelect: string -> unit, renderItem: string -> Blob) as this
  =
  inherit Cell<MetroFlipViewInput>()

  let viewportHandle = ElementHandle()
  let trackTranslate = this.AnimateValue(FlipInternals.zeroPoint)

  let emptyInput: MetroFlipViewInput = {
    Items = Array.empty
    SelectedId = ""
    ShowNavigation = true
    ShowIndicators = true
    AccessibilityName = ValueNone
  }

  let mutable currentInput = emptyInput
  let mutable lastSelectedId: string voption = ValueNone
  let mutable outgoingId: string voption = ValueNone
  let mutable slideDirection = 1.0
  let mutable slideTimer: WindowTimer voption = ValueNone
  let mutable lastReducedMotion = false
  let mutable scopedReducedMotion = false

  let mutable swipeActive = false
  let mutable swipePointerId = 0L
  let mutable swipeStartX = 0.0
  let mutable swipeStartY = 0.0

  let hostIsOpen() =
    let host = this.Host
    not(isNull host) && host.IsOpen

  let isReducedMotion() =
    scopedReducedMotion
    || (if isNull this.Host then
          lastReducedMotion
        else
          Metro.readWindow(this.Host).ReducedMotion)

  let cancelSlideTimer() =
    slideTimer |> ValueOption.iter(fun timer -> timer.Dispose())
    slideTimer <- ValueNone

  // Drops the outgoing panel and restores the resting track position in the
  // same turn, so the active panel never shifts between the two trees.
  let finishSlide() =
    slideTimer <- ValueNone
    outgoingId <- ValueNone
    slideDirection <- 1.0
    trackTranslate.Snap(FlipInternals.zeroPoint)

  let endSlide() =
    cancelSlideTimer()
    finishSlide()

  // Ends the slide after the track animation settles. The timer follows the
  // wall clock and the animation follows simulated time, so a stalled frame can
  // leave the track running when the first timer fires. The wait is bounded by
  // one more slide duration, so a window that stops rendering still settles.
  let rec scheduleSlideEnd(delayMs: float, remainingMs: float) =
    slideTimer <-
      ValueSome(
        this.After(
          delayMs,
          fun () ->
            if
              trackTranslate.Running && remainingMs > FlipInternals.settlePollMs
            then
              scheduleSlideEnd(
                FlipInternals.settlePollMs,
                remainingMs - FlipInternals.settlePollMs
              )
            else
              finishSlide()
        )
      )

  let navigateTo(targetIdx: int) =
    let items = FlipInternals.itemsOf currentInput

    if targetIdx >= 0 && targetIdx < items.Length then
      let currentIdx = FlipInternals.indexOf(items, currentInput.SelectedId)

      if targetIdx <> currentIdx then
        onSelect items[targetIdx].Id

  let navigateBy(delta: int) =
    let items = FlipInternals.itemsOf currentInput

    navigateTo(FlipInternals.indexOf(items, currentInput.SelectedId) + delta)

  let handle(event: MetroFlipViewEvent) : unit =
    match event with
    | MetroFlipViewEvent.Navigate index -> navigateTo index
    | MetroFlipViewEvent.PointerDown e ->
      if e.IsPrimary then
        swipeActive <- true
        swipePointerId <- e.PointerId
        swipeStartX <- e.Position.X
        swipeStartY <- e.Position.Y
    | MetroFlipViewEvent.PointerMoved e ->
      if swipeActive && e.PointerId = swipePointerId then
        let dx = e.Position.X - swipeStartX
        let dy = e.Position.Y - swipeStartY

        // A mostly vertical travel ends the gesture, so vertical pans win.
        if abs dy > abs dx && abs dy > FlipInternals.swipeThresholdPx then
          swipeActive <- false
    | MetroFlipViewEvent.PointerUp e ->
      if swipeActive && e.PointerId = swipePointerId then
        swipeActive <- false
        let dx = e.Position.X - swipeStartX
        let dy = e.Position.Y - swipeStartY

        if abs dx >= FlipInternals.swipeThresholdPx && abs dx > abs dy then
          if dx < 0.0 then navigateBy 1 else navigateBy -1
    | MetroFlipViewEvent.PointerCanceled e ->
      if swipeActive && e.PointerId = swipePointerId then
        swipeActive <- false

  member _.TrackTranslate = trackTranslate

  member _.OutgoingId = outgoingId

  member _.IsSliding = outgoingId.IsSome

  member _.State: MetroFlipViewState = {
    Input = currentInput
    ActiveIndex =
      FlipInternals.indexOf(
        FlipInternals.itemsOf currentInput,
        currentInput.SelectedId
      )
    OutgoingId = outgoingId
    Direction = slideDirection
    TrackTranslate = trackTranslate
    ViewportHandle = viewportHandle
  }

  member this.View: Blob =
    FlipInternals.buildView(this.State, renderItem, handle)

  member _.Handle(event: MetroFlipViewEvent) : unit = handle event

  override this.InputChanged(input: MetroFlipViewInput) =
    currentInput <- input
    let items = FlipInternals.itemsOf input
    let nextIdx = FlipInternals.indexOf(items, input.SelectedId)

    let selectionChanged =
      lastSelectedId
      |> ValueOption.map(fun prevId -> prevId <> input.SelectedId)
      |> ValueOption.defaultValue false

    cancelSlideTimer()

    let previousItem =
      lastSelectedId
      |> ValueOption.bind(fun prevId -> FlipInternals.tryIndexOf(items, prevId))

    if selectionChanged && nextIdx >= 0 then
      if previousItem.IsNone || isReducedMotion() || not(hostIsOpen()) then
        endSlide()
      else
        let previousIdx = previousItem.Value
        let direction = if nextIdx >= previousIdx then 1.0 else -1.0
        let width = viewportHandle.BorderBox.Width

        let distance =
          if Double.IsFinite width && width > 0.0 then
            width
          else
            FlipInternals.fallbackSlidePx

        outgoingId <- lastSelectedId
        slideDirection <- direction

        let startPoint = Point(X = min direction 0.0 * distance, Y = 0.0)

        let endPoint = Point(X = -(max direction 0.0) * distance, Y = 0.0)

        trackTranslate.Snap(startPoint)
        trackTranslate.Play(startPoint, endPoint, MetroMotion.slow)

        // Goo rebuilds this cell after the final timer action, so the rebuilt
        // tree holds the active panel only.
        scheduleSlideEnd(MetroTiming.slowMs, MetroTiming.slowMs)
    elif lastSelectedId.IsNone then
      endSlide()

    lastSelectedId <-
      (if nextIdx >= 0 then
         ValueSome input.SelectedId
       else
         ValueNone)

  override _.Unmounting() =
    cancelSlideTimer()
    swipeActive <- false
    outgoingId <- ValueNone
    slideDirection <- 1.0
    lastSelectedId <- ValueNone
    scopedReducedMotion <- false
    trackTranslate.Snap(FlipInternals.zeroPoint)

  override this.Build(input: MetroFlipViewInput) : Blob =
    currentInput <- input
    let theme = Metro.read()

    let windowReduced =
      if isNull this.Host then
        false
      else
        Metro.readWindow(this.Host).ReducedMotion

    scopedReducedMotion <- theme.ReducedMotion && not windowReduced
    lastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      endSlide()

    this.View

[<RequireQualifiedAccess>]
module MetroFlipView =

  let defaults
    (items: MetroFlipViewItem array, selectedId: string)
    : MetroFlipViewInput = {
    Items = items
    SelectedId = selectedId
    ShowNavigation = true
    ShowIndicators = true
    AccessibilityName = ValueNone
  }

  let swipeThresholdPx: float = FlipInternals.swipeThresholdPx

  let indexOf(items: MetroFlipViewItem array, selectedId: string) : int =
    FlipInternals.indexOf(items, selectedId)

  let buildView
    (
      state: MetroFlipViewState,
      renderItem: string -> Blob,
      onEvent: MetroFlipViewEvent -> unit
    ) : Blob =
    FlipInternals.buildView(state, renderItem, onEvent)

  let createFactory
    (onSelect: string -> unit, renderItem: string -> Blob)
    : unit -> MetroFlipViewCell =
    fun () -> MetroFlipViewCell(onSelect, renderItem)

  let mount
    (key: string, input: MetroFlipViewInput, factory: unit -> MetroFlipViewCell)
    : Blob =
    Cell.Mount<MetroFlipViewInput, MetroFlipViewCell>(key, input, factory)
