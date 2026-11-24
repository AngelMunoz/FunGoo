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

[<AbstractClass>]
type MetroFlipViewCell() =
  inherit Cell<MetroFlipViewInput>()

  abstract TrackTranslate: Anim<Point>

  abstract OutgoingId: string voption

  abstract IsSliding: bool

  abstract State: MetroFlipViewState

  abstract View: Blob

  abstract Handle: event: MetroFlipViewEvent -> unit

  // Gives a navigation request of the host to the host cell, as a
  // Goo.Widgets control does. The owner rule then rebuilds the host cell.
  member this.Raise(callback: Action<string>, value: string) =
    this.InvokeInputCallback(callback, value)

  // Runs an action once on the window timer of this cell. `After` is a
  // protected member of the cell, and F# does not permit a call to it from a
  // function outside the type.
  member internal this.Schedule(delayMs: float, action: Action) : WindowTimer =
    this.After(delayMs, action)

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

  /// State of a flip view cell. One mutable field per concern.
  type private FlipState = {
    ViewportHandle: ElementHandle
    mutable Input: MetroFlipViewInput
    mutable LastSelectedId: string voption
    mutable OutgoingId: string voption
    mutable SlideDirection: float
    mutable SlideTimer: WindowTimer voption
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable SwipeActive: bool
    mutable SwipePointerId: int64
    mutable SwipeStartX: float
    mutable SwipeStartY: float
    // A slot for the animation value. `create` fills it after the cell exists.
    mutable TrackTranslate: Anim<Point>
  }

  /// The values that the functions of a flip view cell work on.
  type private FlipContext = {
    State: FlipState
    /// Gives one navigation request to the host cell.
    Select: Action<string>
    RenderItem: string -> Blob
  }

  /// What the host window tells a change of the input.
  type private HostFacts = {
    Open: bool
    /// <c>ValueNone</c> when the cell has no window.
    ReducedMotion: bool voption
  }

  /// A change of the input with the facts of the host window.
  type private FlipInputChange = {
    Input: MetroFlipViewInput
    Host: HostFacts
  }

  /// The wait for the end of a slide.
  type private SlideWait = { DelayMs: float; RemainingMs: float }

  let private emptyInput: MetroFlipViewInput = {
    Items = Array.empty
    SelectedId = ""
    ShowNavigation = true
    ShowIndicators = true
    AccessibilityName = ValueNone
  }

  let private initialState() : FlipState = {
    ViewportHandle = ElementHandle()
    Input = emptyInput
    LastSelectedId = ValueNone
    OutgoingId = ValueNone
    SlideDirection = 1.0
    SlideTimer = ValueNone
    LastReducedMotion = false
    ScopedReducedMotion = false
    SwipeActive = false
    SwipePointerId = 0L
    SwipeStartX = 0.0
    SwipeStartY = 0.0
    TrackTranslate = Unchecked.defaultof<Anim<Point>>
  }

  /// The facts of the host window. A cell without a window has no open
  /// window and no preference.
  let private hostFacts(host: Window) : HostFacts = {
    Open = not(isNull host) && host.IsOpen
    ReducedMotion =
      (if isNull host then
         ValueNone
       else
         ValueSome(Metro.readWindow(host).ReducedMotion))
  }

  /// The explicit state of the tree, for <c>buildView</c>.
  let private currentState(ctx: FlipContext) : MetroFlipViewState = {
    Input = ctx.State.Input
    ActiveIndex =
      FlipInternals.indexOf(
        FlipInternals.itemsOf ctx.State.Input,
        ctx.State.Input.SelectedId
      )
    OutgoingId = ctx.State.OutgoingId
    Direction = ctx.State.SlideDirection
    TrackTranslate = ctx.State.TrackTranslate
    ViewportHandle = ctx.State.ViewportHandle
  }

  let private isReducedMotion
    (state: FlipState, hostReduced: bool voption)
    : bool =
    state.ScopedReducedMotion
    || ValueOption.defaultValue state.LastReducedMotion hostReduced

  let private cancelSlideTimer(state: FlipState) : unit =
    state.SlideTimer |> ValueOption.iter(fun timer -> timer.Dispose())
    state.SlideTimer <- ValueNone

  // Drops the outgoing panel and restores the resting track position in the
  // same turn, so the active panel never shifts between the two trees.
  let private finishSlide(state: FlipState) : unit =
    state.SlideTimer <- ValueNone
    state.OutgoingId <- ValueNone
    state.SlideDirection <- 1.0
    state.TrackTranslate.Snap(FlipInternals.zeroPoint)

  let private endSlide(state: FlipState) : unit =
    cancelSlideTimer state
    finishSlide state

  let private navigateTo(ctx: FlipContext, targetIdx: int) : unit =
    let input = ctx.State.Input
    let items = FlipInternals.itemsOf input

    if targetIdx >= 0 && targetIdx < items.Length then
      let currentIdx = FlipInternals.indexOf(items, input.SelectedId)

      if targetIdx <> currentIdx then
        ctx.Select.Invoke(items[targetIdx].Id)

  let private navigateBy(ctx: FlipContext, delta: int) : unit =
    let input = ctx.State.Input
    let items = FlipInternals.itemsOf input

    navigateTo(ctx, FlipInternals.indexOf(items, input.SelectedId) + delta)

  /// Handles one event of the tree: a navigation request or a pointer
  /// gesture on the content area.
  let private handle(ctx: FlipContext, event: MetroFlipViewEvent) : unit =
    let state = ctx.State

    match event with
    | MetroFlipViewEvent.Navigate index -> navigateTo(ctx, index)
    | MetroFlipViewEvent.PointerDown e ->
      if e.IsPrimary then
        state.SwipeActive <- true
        state.SwipePointerId <- e.PointerId
        state.SwipeStartX <- e.Position.X
        state.SwipeStartY <- e.Position.Y
    | MetroFlipViewEvent.PointerMoved e ->
      if state.SwipeActive && e.PointerId = state.SwipePointerId then
        let dx = e.Position.X - state.SwipeStartX
        let dy = e.Position.Y - state.SwipeStartY

        // A mostly vertical travel ends the gesture, so vertical pans win.
        if abs dy > abs dx && abs dy > FlipInternals.swipeThresholdPx then
          state.SwipeActive <- false
    | MetroFlipViewEvent.PointerUp e ->
      if state.SwipeActive && e.PointerId = state.SwipePointerId then
        state.SwipeActive <- false
        let dx = e.Position.X - state.SwipeStartX
        let dy = e.Position.Y - state.SwipeStartY

        if abs dx >= FlipInternals.swipeThresholdPx && abs dx > abs dy then
          // The content moves with the pointer, thus a swipe to the left
          // advances.
          if dx < 0.0 then navigateBy(ctx, 1) else navigateBy(ctx, -1)
    | MetroFlipViewEvent.PointerCanceled e ->
      if state.SwipeActive && e.PointerId = state.SwipePointerId then
        state.SwipeActive <- false

  let private view(ctx: FlipContext) : Blob =
    FlipInternals.buildView(
      currentState ctx,
      ctx.RenderItem,
      fun event -> handle(ctx, event)
    )

  // Ends the slide after the track animation settles. The timer follows the
  // wall clock and the animation follows simulated time, so a stalled frame can
  // leave the track running when the first timer fires. The wait is bounded by
  // one more slide duration, so a window that stops rendering still settles.
  let rec private scheduleSlideEnd
    (cell: MetroFlipViewCell, ctx: FlipContext, wait: SlideWait)
    : unit =
    let timer =
      cell.Schedule(
        wait.DelayMs,
        Action(fun () ->
          let running = ctx.State.TrackTranslate.Running

          if running && wait.RemainingMs > FlipInternals.settlePollMs then
            scheduleSlideEnd(
              cell,
              ctx,
              {
                DelayMs = FlipInternals.settlePollMs
                RemainingMs = wait.RemainingMs - FlipInternals.settlePollMs
              }
            )
          else
            finishSlide ctx.State)
      )

    ctx.State.SlideTimer <- ValueSome timer

  // Starts the slide from the previous item to the next item.
  let private startSlide
    (cell: MetroFlipViewCell, ctx: FlipContext, indices: struct (int * int))
    : unit =
    let state = ctx.State
    let struct (previousIdx, nextIdx) = indices
    let direction = if nextIdx >= previousIdx then 1.0 else -1.0
    let width = state.ViewportHandle.BorderBox.Width

    let distance =
      if Double.IsFinite width && width > 0.0 then
        width
      else
        FlipInternals.fallbackSlidePx

    state.OutgoingId <- state.LastSelectedId
    state.SlideDirection <- direction

    let startPoint = Point(X = min direction 0.0 * distance, Y = 0.0)
    let endPoint = Point(X = -(max direction 0.0 * distance), Y = 0.0)

    state.TrackTranslate.Snap(startPoint)
    state.TrackTranslate.Play(startPoint, endPoint, MetroMotion.slow)

    // Goo rebuilds this cell after the final timer action, so the rebuilt
    // tree holds the active panel only.
    scheduleSlideEnd(
      cell,
      ctx,
      {
        DelayMs = MetroTiming.slowMs
        RemainingMs = MetroTiming.slowMs
      }
    )

  let private inputChanged
    (cell: MetroFlipViewCell, ctx: FlipContext, change: FlipInputChange)
    : unit =
    let state = ctx.State
    let input = change.Input
    state.Input <- input
    let items = FlipInternals.itemsOf input
    let nextIdx = FlipInternals.indexOf(items, input.SelectedId)

    let selectionChanged =
      state.LastSelectedId
      |> ValueOption.map(fun prevId -> prevId <> input.SelectedId)
      |> ValueOption.defaultValue false

    cancelSlideTimer state

    let previousItem =
      state.LastSelectedId
      |> ValueOption.bind(fun prevId -> FlipInternals.tryIndexOf(items, prevId))

    if selectionChanged && nextIdx >= 0 then
      // A slide needs a previous item, no reduced motion, and an open window.
      let canSlide =
        not(isReducedMotion(state, change.Host.ReducedMotion))
        && change.Host.Open

      match previousItem with
      | ValueSome previousIdx when canSlide ->
        startSlide(cell, ctx, struct (previousIdx, nextIdx))
      | _ -> endSlide state
    elif state.LastSelectedId.IsNone then
      endSlide state

    state.LastSelectedId <-
      (if nextIdx >= 0 then
         ValueSome input.SelectedId
       else
         ValueNone)

  /// Cancels the slide timer and the pointer gesture, then puts the track
  /// at rest.
  let private unmount(state: FlipState) : unit =
    cancelSlideTimer state
    state.SwipeActive <- false
    state.OutgoingId <- ValueNone
    state.SlideDirection <- 1.0
    state.LastSelectedId <- ValueNone
    state.ScopedReducedMotion <- false
    state.TrackTranslate.Snap(FlipInternals.zeroPoint)

  /// Reads the theme, ends a slide under reduced motion, then builds the
  /// tree.
  let private build
    (ctx: FlipContext, input: MetroFlipViewInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      endSlide state

    view ctx

  let create
    (onSelect: string -> unit, renderItem: string -> Blob)
    : MetroFlipViewCell =
    let state = initialState()

    // The slot of the cell: the selection delegate is made before the cell
    // exists.
    let mutable self: MetroFlipViewCell voption = ValueNone
    let onSelectAction = Action<string>(onSelect)

    // One delegate for the life of the cell. It gives each navigation request
    // to the host cell, so the owner rule rebuilds the host.
    let select =
      Action<string>(fun id ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onSelectAction, id)))

    let context: FlipContext = {
      State = state
      Select = select
      RenderItem = renderItem
    }

    let cell = {
      new MetroFlipViewCell() with
        override _.TrackTranslate = state.TrackTranslate
        override _.OutgoingId = state.OutgoingId
        override _.IsSliding = state.OutgoingId.IsSome
        override _.State = currentState context
        override _.View = view context

        override this.Handle(event) = handle(context, event)

        override this.InputChanged(input) =
          inputChanged(
            this,
            context,
            {
              Input = input
              Host = hostFacts this.Host
            }
          )

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          build(context, input, (hostFacts this.Host).ReducedMotion)
    }

    // The cell exists now: Goo ties the animation to it.
    state.TrackTranslate <- cell.AnimateValue(FlipInternals.zeroPoint)
    self <- ValueSome cell
    cell

  let createFactory
    (onSelect: string -> unit, renderItem: string -> Blob)
    : unit -> MetroFlipViewCell =
    fun () -> create(onSelect, renderItem)

  let mount
    (key: string, input: MetroFlipViewInput, factory: unit -> MetroFlipViewCell)
    : Blob =
    Cell.Mount<MetroFlipViewInput, MetroFlipViewCell>(key, input, factory)
