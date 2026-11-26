namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroPivotItem = { Id: string; Header: string }

[<Struct; NoComparison>]
type MetroPivotInput = {
  Title: string voption
  Items: MetroPivotItem array
  SelectedId: string
  Loop: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroPivotCell() =
  inherit Cell<MetroPivotInput>()

  abstract HeaderScrollHandle: ElementHandle

  abstract ContentTranslate: Anim<Point>

  abstract ContentOpacity: Anim<float>

  abstract Mode: MetroTiltMode

  abstract IsShaderAttached: bool

  abstract ProgressAnim: Anim<float>

  abstract View: Blob

  // Gives a selection of the host to the host cell, as a Goo.Widgets control
  // does. The owner rule then rebuilds the host cell.
  member this.Raise(callback: Action<string>, value: string) =
    this.InvokeInputCallback(callback, value)

module PivotInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  [<Literal>]
  let swipeThresholdPx = 48.0

  [<Literal>]
  let slideDistancePx = 48.0

  let findItemIndex(items: MetroPivotItem array, selectedId: string) : int =
    if isNull items || items.Length = 0 then
      -1
    else
      let mutable found = -1
      let mutable i = 0

      while i < items.Length && found < 0 do
        if items[i].Id = selectedId then
          found <- i

        i <- i + 1

      if found >= 0 then found else 0

  let stepIndex(current: int, delta: int, count: int, loop: bool) : int =
    if count <= 0 then -1
    elif loop then ((current + delta) % count + count) % count
    else Math.Clamp(current + delta, 0, count - 1)

  let transitionDirection(prevIdx: int, nextIdx: int, count: int, loop: bool) =
    if prevIdx < 0 || count <= 1 then 1.0
    elif loop && prevIdx = count - 1 && nextIdx = 0 then 1.0
    elif loop && prevIdx = 0 && nextIdx = count - 1 then -1.0
    elif nextIdx >= prevIdx then 1.0
    else -1.0

[<RequireQualifiedAccess>]
module MetroPivot =

  let defaults
    (items: MetroPivotItem array, selectedId: string)
    : MetroPivotInput = {
    Title = ValueNone
    Items = items
    SelectedId = selectedId
    Loop = true
    AccessibilityName = ValueNone
  }

  // State of a Pivot cell. One mutable field per concern.
  type PivotState = {
    // Optional 3D perspective effect for the turnstile transition. ValueNone
    // keeps the 2D slide.
    Effect: ShaderEffect voption
    HeaderScrollHandle: ElementHandle
    ActiveTabHandle: ElementHandle
    mutable Subscribed: bool
    mutable PendingHeaderPan: bool
    mutable Input: MetroPivotInput
    mutable LastSelectedId: string voption
    mutable LastSelectedIndex: int
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable SwipeActive: bool
    mutable SwipePointerId: int64
    mutable SwipeStartX: float
    mutable SwipeStartY: float
    // True while the content panel carries the 3D effect. The cell detaches
    // the effect when the turnstile settles.
    mutable ShaderAttached: bool
    // Slots for the animation values. `createWithEffect` fills them after the
    // cell exists.
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
    mutable Progress: Anim<float>
  }

  // The delegates of a Pivot cell. `create` makes them once.
  type PivotDelegates = {
    KeyLeft: Action
    KeyRight: Action
    KeyHome: Action
    KeyEnd: Action
    NavKeys: KeyBinding array
    HeaderWheel: Action<WheelEvent>
    HeaderMetrics: Action<ElementMetrics>
    PointerDown: Action<PointerEvent>
    PointerMove: Action<PointerEvent>
    PointerUp: Action<PointerEvent>
    PointerCancel: Action<PointerEvent>
  }

  // The values that the functions of a Pivot cell work on.
  type PivotContext = {
    State: PivotState
    // Gives one selection to the host cell.
    Select: Action<string>
    RenderContent: string -> Blob
    Delegates: PivotDelegates
  }

  let initialState(effect: ShaderEffect voption) : PivotState = {
    Effect = effect
    HeaderScrollHandle = ElementHandle()
    ActiveTabHandle = ElementHandle()
    Subscribed = false
    PendingHeaderPan = true
    Input = {
      Title = ValueNone
      Items = Array.empty
      SelectedId = ""
      Loop = true
      AccessibilityName = ValueNone
    }
    LastSelectedId = ValueNone
    LastSelectedIndex = -1
    LastReducedMotion = false
    ScopedReducedMotion = false
    SwipeActive = false
    SwipePointerId = 0L
    SwipeStartX = 0.0
    SwipeStartY = 0.0
    ShaderAttached = false
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
    Progress = Unchecked.defaultof<Anim<float>>
  }

  let itemsOf(input: MetroPivotInput) : MetroPivotItem array =
    if isNull input.Items then Array.empty else input.Items

  /// Puts the content at rest: no translation and fully opaque, with the 3D
  /// effect detached and reset to identity.
  let snapToRest(state: PivotState) : unit =
    state.Translate.Snap(PivotInternals.zeroPoint)
    state.Opacity.Snap(1.0)
    state.Progress.Snap(1.0)
    state.ShaderAttached <- false

    state.Effect
    |> ValueOption.iter(fun eff ->
      MetroPerspective.applyParams(eff, MetroPerspective.identity))

  let selectIndex
    (state: PivotState, select: Action<string>, targetIdx: int)
    : unit =
    let input = state.Input
    let items = itemsOf input

    if targetIdx >= 0 && targetIdx < items.Length then
      let curIdx = PivotInternals.findItemIndex(items, input.SelectedId)

      if targetIdx <> curIdx then
        select.Invoke(items[targetIdx].Id)

  let selectLast(state: PivotState, select: Action<string>) : unit =
    let items = state.Input.Items

    if not(isNull items) && items.Length > 0 then
      selectIndex(state, select, items.Length - 1)

  let navigateBy(state: PivotState, select: Action<string>, delta: int) : unit =
    let input = state.Input
    let items = itemsOf input

    if items.Length > 1 then
      let curIdx = PivotInternals.findItemIndex(items, input.SelectedId)

      let nextIdx =
        PivotInternals.stepIndex(curIdx, delta, items.Length, input.Loop)

      if nextIdx >= 0 && nextIdx <> curIdx then
        select.Invoke(items[nextIdx].Id)

  let pointerDown(state: PivotState, e: PointerEvent) : unit =
    if e.IsPrimary then
      state.SwipeActive <- true
      state.SwipePointerId <- e.PointerId
      state.SwipeStartX <- e.Position.X
      state.SwipeStartY <- e.Position.Y

  let pointerMove(state: PivotState, e: PointerEvent) : unit =
    if state.SwipeActive && e.PointerId = state.SwipePointerId then
      let dx = e.Position.X - state.SwipeStartX
      let dy = e.Position.Y - state.SwipeStartY

      // A vertical movement cancels the swipe.
      if abs dy > abs dx && abs dy > PivotInternals.swipeThresholdPx then
        state.SwipeActive <- false

  let pointerUp
    (state: PivotState, select: Action<string>, e: PointerEvent)
    : unit =
    if state.SwipeActive && e.PointerId = state.SwipePointerId then
      state.SwipeActive <- false
      let dx = e.Position.X - state.SwipeStartX
      let dy = e.Position.Y - state.SwipeStartY

      // A horizontal movement of the threshold navigates. The content moves
      // with the pointer, thus a swipe to the left advances.
      if abs dx >= PivotInternals.swipeThresholdPx && abs dx > abs dy then
        if dx < 0.0 then
          navigateBy(state, select, 1)
        else
          navigateBy(state, select, -1)

  let pointerCancel(state: PivotState, e: PointerEvent) : unit =
    if state.SwipeActive && e.PointerId = state.SwipePointerId then
      state.SwipeActive <- false

  let headerWheel(state: PivotState, e: WheelEvent) : unit =
    let rangeX = state.HeaderScrollHandle.ScrollRange.X

    if rangeX > 0.0 && abs e.Delta.Y > abs e.Delta.X then
      let currentX = state.HeaderScrollHandle.ScrollOffset.X
      let targetX = Math.Clamp(currentX - e.Delta.Y * 40.0, 0.0, rangeX)

      // At either end the offset cannot move, so the wheel keeps scrolling
      // the parent. `PreventDefault` blocks the default scroll of the whole
      // hit chain.
      if targetX <> currentX then
        state.HeaderScrollHandle.JumpTo(x = Nullable targetX) |> ignore
        e.PreventDefault()

  let syncHeaderPan(state: PivotState) : unit =
    let tabBox = state.ActiveTabHandle.BorderBox
    let stripBox = state.HeaderScrollHandle.ContentBox

    if state.PendingHeaderPan && tabBox.Width > 0.0 && stripBox.Width > 0.0 then
      state.PendingHeaderPan <- false
      let curScrollX = state.HeaderScrollHandle.ScrollOffset.X
      let maxScrollX = state.HeaderScrollHandle.ScrollRange.X
      let stripLeft = stripBox.X
      let stripRight = stripBox.X + stripBox.Width

      let nextScrollX =
        if tabBox.X < stripLeft then
          curScrollX + (tabBox.X - stripLeft)
        elif tabBox.X + tabBox.Width > stripRight then
          curScrollX + (tabBox.X + tabBox.Width - stripRight)
        else
          curScrollX

      let clampedX = Math.Clamp(nextScrollX, 0.0, maxScrollX)

      if abs(clampedX - curScrollX) > 0.5 then
        state.HeaderScrollHandle.JumpTo(x = Nullable clampedX) |> ignore

  let delegatesOf(state: PivotState, select: Action<string>) : PivotDelegates =
    let keyLeft = Action(fun () -> navigateBy(state, select, -1))
    let keyRight = Action(fun () -> navigateBy(state, select, 1))
    let keyHome = Action(fun () -> selectIndex(state, select, 0))
    let keyEnd = Action(fun () -> selectLast(state, select))

    let navKeys = [|
      KeyBinding(Key.Left, KeyModifiers.None, keyLeft)
      KeyBinding(Key.Right, KeyModifiers.None, keyRight)
      KeyBinding(Key.Home, KeyModifiers.None, keyHome)
      KeyBinding(Key.End, KeyModifiers.None, keyEnd)
    |]

    {
      KeyLeft = keyLeft
      KeyRight = keyRight
      KeyHome = keyHome
      KeyEnd = keyEnd
      NavKeys = navKeys
      HeaderWheel = Action<WheelEvent>(fun e -> headerWheel(state, e))
      HeaderMetrics = Action<ElementMetrics>(fun _ -> syncHeaderPan state)
      PointerDown = Action<PointerEvent>(fun e -> pointerDown(state, e))
      PointerMove = Action<PointerEvent>(fun e -> pointerMove(state, e))
      PointerUp = Action<PointerEvent>(fun e -> pointerUp(state, select, e))
      PointerCancel = Action<PointerEvent>(fun e -> pointerCancel(state, e))
    }

  let mounted(ctx: PivotContext) : unit =
    let state = ctx.State

    if not state.Subscribed then
      state.ActiveTabHandle.add_MetricsChanged ctx.Delegates.HeaderMetrics
      state.HeaderScrollHandle.add_MetricsChanged ctx.Delegates.HeaderMetrics
      state.Subscribed <- true

    syncHeaderPan state

  // Plays the entrance transition of the new selection, or snaps to rest
  // under reduced motion.
  let inputChanged
    (ctx: PivotContext, input: MetroPivotInput, hostReduced: bool voption)
    : unit =
    let state = ctx.State
    state.Input <- input
    let items = itemsOf input
    let nextIdx = PivotInternals.findItemIndex(items, input.SelectedId)

    let reducedMotion =
      state.ScopedReducedMotion
      || ValueOption.defaultValue state.LastReducedMotion hostReduced

    let selectionChanged =
      state.LastSelectedId
      |> ValueOption.map(fun prevId -> prevId <> input.SelectedId)
      |> ValueOption.defaultValue false

    if selectionChanged && nextIdx >= 0 then
      state.PendingHeaderPan <- true

      if reducedMotion then
        snapToRest state
      else
        match state.Effect with
        | ValueSome eff ->
          state.ShaderAttached <- true
          state.Translate.Snap(PivotInternals.zeroPoint)
          state.Opacity.Snap(1.0)

          // A switch during the turnstile continues from the current value,
          // so no frame renders at p = 0 mid-flight. Fresh starts begin at 0.
          let resume =
            if state.Progress.Running then state.Progress.Value else 0.0

          MetroPerspective.applyParams(
            eff,
            MetroPerspective.turnstileIn(resume)
          )

          if state.Progress.Running then
            // Play would snap back to the start value.
            state.Progress.To(1.0, MetroMotion.normal)
          else
            state.Progress.Play(0.0, 1.0, MetroMotion.normal)
        | ValueNone ->
          state.ShaderAttached <- false

          let dir =
            PivotInternals.transitionDirection(
              state.LastSelectedIndex,
              nextIdx,
              items.Length,
              input.Loop
            )

          state.Translate.Play(
            Point(X = dir * PivotInternals.slideDistancePx, Y = 0.0),
            PivotInternals.zeroPoint,
            MetroMotion.normal
          )

          state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
    elif state.LastSelectedId.IsNone then
      state.PendingHeaderPan <- true
      snapToRest state

    state.LastSelectedId <- ValueSome input.SelectedId
    state.LastSelectedIndex <- nextIdx

  // Resets the swipe, the selection tracking, and the animations.
  let unmount(ctx: PivotContext) : unit =
    let state = ctx.State

    if state.Subscribed then
      state.ActiveTabHandle.remove_MetricsChanged ctx.Delegates.HeaderMetrics
      state.HeaderScrollHandle.remove_MetricsChanged ctx.Delegates.HeaderMetrics
      state.Subscribed <- false

    state.PendingHeaderPan <- true
    state.SwipeActive <- false
    state.LastSelectedId <- ValueNone
    state.LastSelectedIndex <- -1
    state.ScopedReducedMotion <- false
    snapToRest state

  // Builds the title, the header strip, and the content panel.
  let buildTree
    (ctx: PivotContext, input: MetroPivotInput, theme: MetroTheme)
    : Blob =
    let state = ctx.State
    let delegates = ctx.Delegates
    let p = theme.Palette
    let items = itemsOf input
    let selectedIdx = PivotInternals.findItemIndex(items, input.SelectedId)
    let headerSpec = MetroTypography.spec MetroTextRole.Header

    let inactiveColor =
      if theme.HighContrast then
        p.ForegroundSecondary
      else
        p.ForegroundDisabled

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels -2.0
      )

    let hoverStyle = Style(Color = p.Foreground)

    let tabListName =
      input.AccessibilityName
      |> ValueOption.orElse input.Title
      |> ValueOption.defaultValue "Pivot"

    let headerStrip =
      Container(
        Handle = state.HeaderScrollHandle,
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.NoWrap,
        AlignItems = AlignItems.Baseline,
        Gap = Length.Pixels 24.0,
        OverflowX = Overflow.Scroll,
        OverflowY = Overflow.Hidden,
        ScrollbarVisibilityX = ScrollbarVisibility.Hidden,
        FlexShrink = 0.0,
        Width = Length.Percent 100.0,
        OnWheel = delegates.HeaderWheel,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.TabList,
            Name = tabListName,
            Orientation = AccessibilityOrientation.Horizontal
          )
      )

    for i = 0 to items.Length - 1 do
      let item = items[i]
      let isSelected = i = selectedIdx
      let headerText = if isNull item.Header then "" else item.Header
      let itemId = item.Id

      let selectAction =
        Action(fun () ->
          if not isSelected then
            ctx.Select.Invoke(itemId))

      let tabKeyBindings = [|
        KeyBinding(Key.Enter, KeyModifiers.None, selectAction)
        KeyBinding(Key.Space, KeyModifiers.None, selectAction)
        KeyBinding(Key.Left, KeyModifiers.None, delegates.KeyLeft)
        KeyBinding(Key.Right, KeyModifiers.None, delegates.KeyRight)
        KeyBinding(Key.Home, KeyModifiers.None, delegates.KeyHome)
        KeyBinding(Key.End, KeyModifiers.None, delegates.KeyEnd)
      |]

      let tabBtn =
        Button(
          Key = $"pivot-tab-{itemId}",
          FlexDirection = FlexDirection.Row,
          AlignItems = AlignItems.Center,
          FlexShrink = 0.0,
          Padding = Edges(Length.Pixels 4.0, Length.Pixels 0.0),
          BorderRadius = PivotInternals.zeroRadius,
          BorderWidth = PivotInternals.zeroBorder,
          BorderColor = PivotInternals.transparentBorder,
          BoxShadows = PivotInternals.emptyShadows,
          BackgroundColor = Color.Transparent,
          Color = (if isSelected then p.Foreground else inactiveColor),
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(headerSpec.FontSize * theme.TextScaleFactor),
          FontWeight = float headerSpec.FontWeight,
          LetterSpacing =
            Length.Pixels(headerSpec.LetterSpacing * theme.TextScaleFactor),
          LineHeight = headerSpec.LineHeight,
          HoverStyle = hoverStyle,
          FocusStyle = focusStyle,
          TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
          TransitionEasing = Easing.EaseOut,
          Cursor = Cursor.Pointer,
          Focusable = true,
          TabStop = true,
          OnClick = selectAction,
          KeyBindings = tabKeyBindings,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.Tab,
              Name = headerText,
              Selected = Nullable isSelected,
              PositionInSet = Nullable(i + 1),
              SizeOfSet = Nullable items.Length
            )
        )
          .Children(Text(Content = headerText, TextWrap = TextWrap.NoWrap))

      if isSelected then
        tabBtn.Handle <- state.ActiveTabHandle

      headerStrip.Add(tabBtn)

    let activeHeader =
      if selectedIdx >= 0 && selectedIdx < items.Length then
        items[selectedIdx].Header
      else
        ""

    let contentPanel =
      Container(
        Key = "pivot-content",
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        TranslateAnim = state.Translate,
        OpacityAnim = state.Opacity,
        OnPointerDown = delegates.PointerDown,
        OnPointerMove = delegates.PointerMove,
        OnPointerUp = delegates.PointerUp,
        OnPointerCancel = delegates.PointerCancel,
        Accessibility =
          Accessibility(Role = AccessibilityRole.TabPanel, Name = activeHeader)
      )

    if selectedIdx >= 0 && selectedIdx < items.Length then
      contentPanel.Add(ctx.RenderContent items[selectedIdx].Id)

    if state.ShaderAttached then
      state.Effect
      |> ValueOption.iter(fun eff -> contentPanel.ShaderEffect <- eff)

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Gap = Length.Pixels 16.0,
        KeyBindings = delegates.NavKeys
      )

    input.Title
    |> ValueOption.iter(fun title ->
      if not(String.IsNullOrWhiteSpace title) then
        root.Add(
          MetroText.build {
            MetroText.defaults(title, MetroTextRole.Badge) with
                Tone = MetroTextTone.Secondary
          }
        ))

    root.Children(headerStrip, contentPanel)

  // Reads the theme, stops a running transition under reduced motion, then
  // builds the tree.
  let build
    (ctx: PivotContext, input: MetroPivotInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if
      theme.ReducedMotion
      && (state.Translate.Running
          || state.Opacity.Running
          || state.Progress.Running)
    then
      snapToRest state

    buildTree(ctx, input, theme)

  // The reduced motion preference of the host window. ValueNone when the cell
  // has no window.
  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let createWithEffect
    (
      effect: ShaderEffect voption,
      onSelect: string -> unit,
      renderContent: string -> Blob
    ) : MetroPivotCell =
    let state = initialState effect

    // The slot of the cell: the selection delegate is made before the cell
    // exists.
    let mutable self: MetroPivotCell voption = ValueNone
    let onSelectAction = Action<string>(onSelect)

    // One delegate for the life of the cell. It gives each selection to the
    // host cell, so the owner rule rebuilds the host.
    let select =
      Action<string>(fun id ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onSelectAction, id)))

    let context: PivotContext = {
      State = state
      Select = select
      RenderContent = renderContent
      Delegates = delegatesOf(state, select)
    }

    let cell = {
      new MetroPivotCell() with
        override _.HeaderScrollHandle = state.HeaderScrollHandle
        override _.ContentTranslate = state.Translate
        override _.ContentOpacity = state.Opacity

        override _.Mode =
          if state.Effect.IsSome then
            MetroTiltMode.Perspective3D
          else
            MetroTiltMode.Fallback2D

        override _.IsShaderAttached = state.ShaderAttached
        override _.ProgressAnim = state.Progress

        override _.View = buildTree(context, state.Input, Metro.read())

        override _.Mounted() = mounted context

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount context

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    // The cell exists now: Goo ties each animation to it.
    state.Translate <- cell.AnimateValue(PivotInternals.zeroPoint)
    state.Opacity <- cell.AnimateValue(1.0)

    match state.Effect with
    | ValueSome eff ->
      MetroPerspective.applyParams(eff, MetroPerspective.identity)

      let progress =
        cell.Animate(
          0.0,
          fun p ->
            MetroPerspective.applyParams(eff, MetroPerspective.turnstileIn(p))
        )

      // Detaching the 3D effect when the turnstile settles runs from the
      // motion pump, not a Blob callback, so the cell rebuilds itself once.
      progress.add_Completed(
        Action<MotionCompletionReason>(fun _ ->
          if state.ShaderAttached then
            state.ShaderAttached <- false
            MetroPerspective.applyParams(eff, MetroPerspective.identity)
            cell.Rebuild())
      )

      state.Progress <- progress
    | ValueNone -> state.Progress <- cell.AnimateValue(0.0)

    self <- ValueSome cell
    cell

  let create
    (onSelect: string -> unit, renderContent: string -> Blob)
    : MetroPivotCell =
    createWithEffect(
      MetroPerspective.tryCreateDefault(),
      onSelect,
      renderContent
    )

  let createFactory
    (onSelect: string -> unit, renderContent: string -> Blob)
    : unit -> MetroPivotCell =
    fun () -> create(onSelect, renderContent)

  let mount
    (key: string, input: MetroPivotInput, factory: unit -> MetroPivotCell)
    : Blob =
    Cell.Mount<MetroPivotInput, MetroPivotCell>(key, input, factory)
