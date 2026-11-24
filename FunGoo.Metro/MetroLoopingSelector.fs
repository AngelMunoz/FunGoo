namespace FunGoo.Metro

open System
open Goo
open FunGoo.Children

[<Struct; NoComparison>]
type MetroLoopingItem = {
  Id: string
  PrimaryText: string
  SecondaryText: string voption
}

[<Struct; NoComparison>]
type MetroLoopingSelectorInput = {
  Items: MetroLoopingItem array
  SelectedId: string
  Loop: bool
  VisibleNeighbors: int
  ItemWidth: float
  ItemHeight: float
  Disabled: bool
  AccessibilityName: string voption
}

[<Struct; NoComparison>]
type MetroLoopingSelectorState = {
  Input: MetroLoopingSelectorInput
  TrackTranslate: Anim<Point>
}

[<Struct; NoComparison>]
type MetroLoopingStepArgs = {
  Current: int
  Delta: int
  Count: int
  Loop: bool
}

[<AbstractClass>]
type MetroLoopingSelectorCell() =
  inherit Cell<MetroLoopingSelectorInput>()

  abstract SelectedId: string

  abstract SelectedIndex: int

  abstract TrackTranslate: Anim<Point>

  abstract State: MetroLoopingSelectorState

  abstract View: Blob

  abstract Step: delta: int -> unit

  // Gives one selection to the host cell through InvokeInputCallback so the
  // owner rule rebuilds the host cell.
  member this.Raise(callback: Action<string>, value: string) =
    this.InvokeInputCallback(callback, value)

module LoopingInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let metroBorder = Edges(Length.Pixels 2.0)
  let zeroPoint = Point(X = 0.0, Y = 0.0)

  [<Literal>]
  let defaultWidthPx = 84.0

  [<Literal>]
  let defaultHeightPx = 60.0

  [<Literal>]
  let gapPx = 6.0

  let resolveWidth(value: float) : float =
    if Double.IsFinite value && value >= 36.0 then
      value
    else
      defaultWidthPx

  let resolveHeight(value: float) : float =
    if Double.IsFinite value && value >= 32.0 then
      value
    else
      defaultHeightPx

  let resolveNeighbors(value: int) : int = Math.Clamp(value, 1, 3)

  let findIndex(items: MetroLoopingItem array, selectedId: string) : int =
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

  [<Struct; NoComparison>]
  type TransitionArgs = {
    PrevIndex: int
    NextIndex: int
    Count: int
    Loop: bool
  }

  [<Struct; NoComparison>]
  type SlotArgs = {
    SlotKey: string
    Item: MetroLoopingItem voption
    IsCenter: bool
    Width: float
    Height: float
    Disabled: bool
  }

  let resolveOffsetIndex(args: MetroLoopingStepArgs) : int =
    if args.Count <= 0 || args.Current < 0 then
      -1
    elif args.Loop then
      ((args.Current + args.Delta) % args.Count + args.Count) % args.Count
    else
      let target = args.Current + args.Delta
      if target >= 0 && target < args.Count then target else -1

  let stepClamped(args: MetroLoopingStepArgs) : int =
    if args.Count <= 0 || args.Current < 0 then
      -1
    elif args.Loop then
      ((args.Current + args.Delta) % args.Count + args.Count) % args.Count
    else
      Math.Clamp(args.Current + args.Delta, 0, args.Count - 1)

  let transitionDirection(args: TransitionArgs) : float =
    if
      args.PrevIndex < 0 || args.Count <= 1 || args.PrevIndex = args.NextIndex
    then
      0.0
    elif args.Loop && args.PrevIndex = args.Count - 1 && args.NextIndex = 0 then
      1.0
    elif args.Loop && args.PrevIndex = 0 && args.NextIndex = args.Count - 1 then
      -1.0
    elif args.NextIndex > args.PrevIndex then
      1.0
    else
      -1.0

  let buildSlot(args: SlotArgs, onSelect: string -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette
    let a = theme.Accent
    let isCenter = args.IsCenter
    let width = args.Width
    let height = args.Height
    let disabled = args.Disabled
    let slotKey = args.SlotKey

    match args.Item with
    | ValueNone ->
      // Empty spacer slot when Loop = false and the neighbor is past the start
      // or end of the list. Keeps the active center slot vertically aligned.
      Container(
        Key = slotKey,
        Width = Length.Pixels width,
        Height = Length.Pixels height,
        FlexShrink = 0.0
      )
      :> Blob
    | ValueSome item ->
      let primary = if isNull item.PrimaryText then "" else item.PrimaryText

      let bg =
        if isCenter then
          if theme.HighContrast then p.Highlight else a.Base
        else
          p.BackgroundAlt

      let fg =
        if isCenter then
          if theme.HighContrast then p.Foreground else a.OnAccent
        else
          p.ForegroundSecondary

      let borderColor =
        if theme.HighContrast then p.Foreground
        elif isCenter then a.Base
        else p.Border

      let focusStyle =
        Style(
          OutlineWidth = Length.Pixels 2.0,
          OutlineColor = p.Foreground,
          OutlineOffset = Length.Pixels 2.0
        )

      let hoverBg =
        if isCenter then
          if theme.IsDark then a.Light else a.Dark
        else
          p.Hover

      let canFocus = isCenter && not disabled

      let slot =
        Button(
          Key = slotKey,
          Width = Length.Pixels width,
          Height = Length.Pixels height,
          FlexShrink = 0.0,
          FlexDirection = FlexDirection.Column,
          AlignItems = AlignItems.FlexStart,
          JustifyContent = JustifyContent.FlexEnd,
          Padding = Edges(Length.Pixels 6.0, Length.Pixels 8.0),
          BorderRadius = zeroRadius,
          BorderWidth = metroBorder,
          BorderColor = Edges<Color>(borderColor),
          BoxShadows = emptyShadows,
          BackgroundColor = bg,
          HoverStyle = Style(BackgroundColor = hoverBg),
          ActiveStyle = Style(BackgroundColor = a.Dark),
          FocusStyle = focusStyle,
          DisabledStyle =
            Style(Opacity = (if theme.HighContrast then 1.0 else 0.4)),
          TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
          TransitionEasing = Easing.EaseOut,
          Cursor =
            (if disabled || isCenter then
               Cursor.Default
             else
               Cursor.Pointer),
          Focusable = canFocus,
          TabStop = canFocus,
          Disabled = disabled,
          Accessibility =
            Accessibility(
              Role = AccessibilityRole.ListItem,
              Name =
                (item.SecondaryText
                 |> ValueOption.map(fun sec -> $"{primary} {sec}")
                 |> ValueOption.defaultValue primary),
              Selected = Nullable isCenter
            )
        )

      if not disabled && not isCenter then
        let targetId = item.Id
        slot.OnClick <- Action(fun () -> onSelect targetId)

      slot.Add(
        Text(
          Content = primary,
          Color = fg,
          FontFamily = theme.FontFamily,
          FontSize =
            Length.Pixels(
              (if isCenter then 22.0 else 18.0) * theme.TextScaleFactor
            ),
          FontWeight = (if isCenter then 600.0 else 300.0),
          LineHeight = 1.05,
          TextWrap = TextWrap.NoWrap,
          TextTrimming = TextTrimming.Ellipsis
        )
      )

      item.SecondaryText
      |> ValueOption.iter(fun secondary ->
        if not(String.IsNullOrEmpty secondary) then
          slot.Add(
            Text(
              Content = secondary,
              Color = fg,
              FontFamily = theme.FontFamily,
              FontSize = Length.Pixels(11.0 * theme.TextScaleFactor),
              FontWeight = 400.0,
              TextWrap = TextWrap.NoWrap,
              TextTrimming = TextTrimming.Ellipsis
            )
          ))

      slot :> Blob

[<RequireQualifiedAccess>]
module MetroLoopingSelector =

  let defaultWidthPx = LoopingInternals.defaultWidthPx
  let defaultHeightPx = LoopingInternals.defaultHeightPx

  let item(id: string, primaryText: string) : MetroLoopingItem = {
    Id = id
    PrimaryText = primaryText
    SecondaryText = ValueNone
  }

  let itemWithCaption
    (id: string, primaryText: string, secondaryText: string)
    : MetroLoopingItem = {
    Id = id
    PrimaryText = primaryText
    SecondaryText = ValueSome secondaryText
  }

  let defaults
    (items: MetroLoopingItem array, selectedId: string)
    : MetroLoopingSelectorInput = {
    Items = items
    SelectedId = selectedId
    Loop = true
    VisibleNeighbors = 1
    ItemWidth = defaultWidthPx
    ItemHeight = defaultHeightPx
    Disabled = false
    AccessibilityName = ValueNone
  }

  let resolveOffsetIndex(args: MetroLoopingStepArgs) : int =
    LoopingInternals.resolveOffsetIndex args

  type RollerInternalState = {
    mutable Input: MetroLoopingSelectorInput
    mutable LastSelectedId: string voption
    mutable LastSelectedIndex: int
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    mutable DragActive: bool
    mutable DragPointerId: int64
    mutable DragAnchorY: float
    mutable Track: Anim<Point>
  }

  type RollerDelegates = {
    NavKeys: KeyBinding array
    Wheel: Action<WheelEvent>
    PointerDown: Action<PointerEvent>
    PointerMove: Action<PointerEvent>
    PointerUp: Action<PointerEvent>
    PointerCancel: Action<PointerEvent>
  }

  type RollerContext = {
    State: RollerInternalState
    Select: Action<string>
    Delegates: RollerDelegates
  }

  let initialState() : RollerInternalState = {
    Input = defaults(Array.empty, "")
    LastSelectedId = ValueNone
    LastSelectedIndex = -1
    LastReducedMotion = false
    ScopedReducedMotion = false
    DragActive = false
    DragPointerId = 0L
    DragAnchorY = 0.0
    Track = Unchecked.defaultof<Anim<Point>>
  }

  let currentState(ctx: RollerContext) : MetroLoopingSelectorState = {
    Input = ctx.State.Input
    TrackTranslate = ctx.State.Track
  }

  let snapToRest(state: RollerInternalState) : unit =
    state.Track.Snap(LoopingInternals.zeroPoint)

  let stepBy
    (state: RollerInternalState, select: Action<string>, delta: int)
    : unit =
    let input = state.Input
    let items = if isNull input.Items then Array.empty else input.Items

    if not input.Disabled && items.Length > 1 && delta <> 0 then
      let curIdx = LoopingInternals.findIndex(items, input.SelectedId)

      let nextIdx =
        LoopingInternals.stepClamped {
          Current = curIdx
          Delta = delta
          Count = items.Length
          Loop = input.Loop
        }

      if nextIdx >= 0 && nextIdx <> curIdx then
        select.Invoke(items[nextIdx].Id)

  let selectBoundary
    (state: RollerInternalState, select: Action<string>, toEnd: bool)
    : unit =
    let input = state.Input
    let items = if isNull input.Items then Array.empty else input.Items

    if not input.Disabled && items.Length > 1 then
      let curIdx = LoopingInternals.findIndex(items, input.SelectedId)
      let targetIdx = if toEnd then items.Length - 1 else 0

      if targetIdx <> curIdx then
        select.Invoke(items[targetIdx].Id)

  let onWheel
    (state: RollerInternalState, select: Action<string>, e: WheelEvent)
    : unit =
    if not state.Input.Disabled && abs e.Delta.Y > 0.01 then
      let delta = if e.Delta.Y < 0.0 then 1 else -1
      stepBy(state, select, delta)
      e.PreventDefault()

  let pointerDown(state: RollerInternalState, e: PointerEvent) : unit =
    if not state.Input.Disabled && e.IsPrimary then
      state.DragActive <- true
      state.DragPointerId <- e.PointerId
      state.DragAnchorY <- e.Position.Y
      e.Capture()

  let pointerMove
    (state: RollerInternalState, select: Action<string>, e: PointerEvent)
    : unit =
    if state.DragActive && e.PointerId = state.DragPointerId then
      e.PreventDefault()
      let stepPx = LoopingInternals.resolveHeight state.Input.ItemHeight * 0.55

      let dy = e.Position.Y - state.DragAnchorY

      if abs dy >= stepPx then
        // Dragging upward (negative dy) advances to the next item below.
        let steps = if dy < 0.0 then 1 else -1
        state.DragAnchorY <- e.Position.Y
        stepBy(state, select, steps)

  let pointerEnd(state: RollerInternalState, e: PointerEvent) : unit =
    if state.DragActive && e.PointerId = state.DragPointerId then
      state.DragActive <- false
      e.ReleaseCapture()

  let delegatesOf
    (state: RollerInternalState, select: Action<string>)
    : RollerDelegates =
    let keyUp = Action(fun () -> stepBy(state, select, -1))
    let keyDown = Action(fun () -> stepBy(state, select, 1))
    let keyPageUp = Action(fun () -> stepBy(state, select, -5))
    let keyPageDown = Action(fun () -> stepBy(state, select, 5))
    let keyHome = Action(fun () -> selectBoundary(state, select, false))
    let keyEnd = Action(fun () -> selectBoundary(state, select, true))

    {
      NavKeys = [|
        KeyBinding(Key.Up, KeyModifiers.None, keyUp)
        KeyBinding(Key.Down, KeyModifiers.None, keyDown)
        KeyBinding(Key.PageUp, KeyModifiers.None, keyPageUp)
        KeyBinding(Key.PageDown, KeyModifiers.None, keyPageDown)
        KeyBinding(Key.Home, KeyModifiers.None, keyHome)
        KeyBinding(Key.End, KeyModifiers.None, keyEnd)
      |]
      Wheel = Action<WheelEvent>(fun e -> onWheel(state, select, e))
      PointerDown = Action<PointerEvent>(fun e -> pointerDown(state, e))
      PointerMove = Action<PointerEvent>(fun e -> pointerMove(state, select, e))
      PointerUp = Action<PointerEvent>(fun e -> pointerEnd(state, e))
      PointerCancel = Action<PointerEvent>(fun e -> pointerEnd(state, e))
    }

  let buildViewWith
    (
      state: MetroLoopingSelectorState,
      onSelect: string -> unit,
      delegates: RollerDelegates voption
    ) : Blob =
    if isNull state.TrackTranslate then
      raise(ArgumentNullException(nameof state))

    let input = state.Input
    let items = if isNull input.Items then Array.empty else input.Items
    let curIdx = LoopingInternals.findIndex(items, input.SelectedId)
    let neighbors = LoopingInternals.resolveNeighbors input.VisibleNeighbors
    let width = LoopingInternals.resolveWidth input.ItemWidth
    let height = LoopingInternals.resolveHeight input.ItemHeight
    let slotCount = neighbors * 2 + 1

    let totalHeight =
      float slotCount * height + float(slotCount - 1) * LoopingInternals.gapPx

    let track =
      Container(
        Key = "looping-track",
        FlexDirection = FlexDirection.Column,
        Gap = Length.Pixels LoopingInternals.gapPx,
        Width = Length.Pixels width,
        TranslateAnim = state.TrackTranslate
      )

    for offset = -neighbors to neighbors do
      let idx =
        LoopingInternals.resolveOffsetIndex {
          Current = curIdx
          Delta = offset
          Count = items.Length
          Loop = input.Loop
        }

      let maybeItem =
        if idx >= 0 && idx < items.Length then
          ValueSome items[idx]
        else
          ValueNone

      let slotKey =
        if offset = 0 then "looping-slot-center"
        elif offset < 0 then $"looping-slot-prev-{-offset}"
        else $"looping-slot-next-{offset}"

      track.Add(
        LoopingInternals.buildSlot(
          {
            SlotKey = slotKey
            Item = maybeItem
            IsCenter = offset = 0
            Width = width
            Height = height
            Disabled = input.Disabled
          },
          onSelect
        )
      )

    let root =
      Container(
        Width = Length.Pixels width,
        Height = Length.Pixels totalHeight,
        FlexShrink = 0.0,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(
            Role = AccessibilityRole.List,
            Name =
              (input.AccessibilityName
               |> ValueOption.defaultValue "Looping selector")
          )
      )
        .Children(track)

    delegates
    |> ValueOption.iter(fun d ->
      if not input.Disabled then
        root.KeyBindings <- d.NavKeys
        root.OnWheel <- d.Wheel
        root.OnPointerDown <- d.PointerDown
        root.OnPointerMove <- d.PointerMove
        root.OnPointerUp <- d.PointerUp
        root.OnPointerCancel <- d.PointerCancel)

    root

  let buildView
    (state: MetroLoopingSelectorState, onSelect: string -> unit)
    : Blob =
    buildViewWith(state, onSelect, ValueNone)

  let inputChanged
    (
      ctx: RollerContext,
      input: MetroLoopingSelectorInput,
      hostReduced: bool voption
    ) : unit =
    let state = ctx.State
    state.Input <- input
    let items = if isNull input.Items then Array.empty else input.Items
    let nextIdx = LoopingInternals.findIndex(items, input.SelectedId)

    let changed =
      state.LastSelectedId
      |> ValueOption.map(fun prevId -> prevId <> input.SelectedId)
      |> ValueOption.defaultValue false

    if changed && nextIdx >= 0 then
      let reducedMotion =
        state.ScopedReducedMotion
        || ValueOption.defaultValue state.LastReducedMotion hostReduced

      if reducedMotion then
        snapToRest state
      else
        let dir =
          LoopingInternals.transitionDirection {
            PrevIndex = state.LastSelectedIndex
            NextIndex = nextIdx
            Count = items.Length
            Loop = input.Loop
          }

        let travel = dir * LoopingInternals.resolveHeight input.ItemHeight * 0.4

        state.Track.Play(
          Point(X = 0.0, Y = travel),
          LoopingInternals.zeroPoint,
          MetroMotion.fast
        )
    elif state.LastSelectedId.IsNone then
      snapToRest state

    state.LastSelectedId <- ValueSome input.SelectedId
    state.LastSelectedIndex <- nextIdx

  let unmount(state: RollerInternalState) : unit =
    state.DragActive <- false
    state.LastSelectedId <- ValueNone
    state.LastSelectedIndex <- -1
    state.ScopedReducedMotion <- false
    snapToRest state

  let build
    (
      ctx: RollerContext,
      input: MetroLoopingSelectorInput,
      hostReduced: bool voption
    ) : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      snapToRest state

    buildViewWith(
      currentState ctx,
      (fun id -> ctx.Select.Invoke(id)),
      ValueSome ctx.Delegates
    )

  let reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  let create(onSelect: string -> unit) : MetroLoopingSelectorCell =
    let state = initialState()
    let mutable self: MetroLoopingSelectorCell voption = ValueNone
    let onSelectAction = Action<string>(onSelect)

    let select =
      Action<string>(fun id ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onSelectAction, id)))

    let context: RollerContext = {
      State = state
      Select = select
      Delegates = delegatesOf(state, select)
    }

    let cell = {
      new MetroLoopingSelectorCell() with
        override _.SelectedId = state.Input.SelectedId

        override _.SelectedIndex =
          let items =
            if isNull state.Input.Items then
              Array.empty
            else
              state.Input.Items

          LoopingInternals.findIndex(items, state.Input.SelectedId)

        override _.TrackTranslate = state.Track
        override _.State = currentState context

        override _.View =
          buildViewWith(
            currentState context,
            (fun id -> context.Select.Invoke(id)),
            ValueSome context.Delegates
          )

        override _.Step(delta) = stepBy(state, select, delta)

        override this.InputChanged(input) =
          inputChanged(context, input, reducedMotionOf this.Host)

        override _.Unmounting() = unmount state

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    state.Track <- cell.AnimateValue(LoopingInternals.zeroPoint)
    self <- ValueSome cell
    cell

  let createFactory
    (onSelect: string -> unit)
    : unit -> MetroLoopingSelectorCell =
    fun () -> create onSelect

  let mount
    (
      key: string,
      input: MetroLoopingSelectorInput,
      factory: unit -> MetroLoopingSelectorCell
    ) : Blob =
    Cell.Mount<MetroLoopingSelectorInput, MetroLoopingSelectorCell>(
      key,
      input,
      factory
    )
