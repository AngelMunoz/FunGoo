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

module private PivotInternals =

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

[<Sealed>]
type MetroPivotCell(onSelect: string -> unit, renderContent: string -> Blob) as this
  =
  inherit Cell<MetroPivotInput>()

  let translateAnim = this.AnimateValue(PivotInternals.zeroPoint)
  let opacityAnim = this.AnimateValue(1.0)

  let mutable currentInput = {
    Title = ValueNone
    Items = Array.empty
    SelectedId = ""
    Loop = true
    AccessibilityName = ValueNone
  }

  let mutable lastSelectedId: string voption = ValueNone
  let mutable lastSelectedIndex = -1
  let mutable lastReducedMotion = false
  let mutable scopedReducedMotion = false

  let mutable swipeActive = false
  let mutable swipePointerId = 0L
  let mutable swipeStartX = 0.0
  let mutable swipeStartY = 0.0

  let selectIndex(targetIdx: int) =
    let input = currentInput
    let items = if isNull input.Items then Array.empty else input.Items

    if targetIdx >= 0 && targetIdx < items.Length then
      let curIdx = PivotInternals.findItemIndex(items, input.SelectedId)

      if targetIdx <> curIdx then
        onSelect items[targetIdx].Id

  let navigateBy(delta: int) =
    let input = currentInput
    let items = if isNull input.Items then Array.empty else input.Items

    if items.Length > 1 then
      let curIdx = PivotInternals.findItemIndex(items, input.SelectedId)

      let nextIdx =
        PivotInternals.stepIndex(curIdx, delta, items.Length, input.Loop)

      if nextIdx >= 0 && nextIdx <> curIdx then
        onSelect items[nextIdx].Id

  let onKeyLeft = Action(fun () -> navigateBy -1)
  let onKeyRight = Action(fun () -> navigateBy 1)
  let onKeyHome = Action(fun () -> selectIndex 0)

  let onKeyEnd =
    Action(fun () ->
      let items = currentInput.Items

      if not(isNull items) && items.Length > 0 then
        selectIndex(items.Length - 1))

  let navKeyBindings = [|
    KeyBinding(Key.Left, KeyModifiers.None, onKeyLeft)
    KeyBinding(Key.Right, KeyModifiers.None, onKeyRight)
    KeyBinding(Key.Home, KeyModifiers.None, onKeyHome)
    KeyBinding(Key.End, KeyModifiers.None, onKeyEnd)
  |]

  let onPointerDown =
    Action<PointerEvent>(fun e ->
      if e.IsPrimary then
        swipeActive <- true
        swipePointerId <- e.PointerId
        swipeStartX <- e.Position.X
        swipeStartY <- e.Position.Y)

  let onPointerMove =
    Action<PointerEvent>(fun e ->
      if swipeActive && e.PointerId = swipePointerId then
        let dx = e.Position.X - swipeStartX
        let dy = e.Position.Y - swipeStartY

        if abs dy > abs dx && abs dy > PivotInternals.swipeThresholdPx then
          swipeActive <- false)

  let onPointerUp =
    Action<PointerEvent>(fun e ->
      if swipeActive && e.PointerId = swipePointerId then
        swipeActive <- false
        let dx = e.Position.X - swipeStartX
        let dy = e.Position.Y - swipeStartY

        if abs dx >= PivotInternals.swipeThresholdPx && abs dx > abs dy then
          if dx < 0.0 then navigateBy 1 else navigateBy -1)

  let onPointerCancel =
    Action<PointerEvent>(fun e ->
      if swipeActive && e.PointerId = swipePointerId then
        swipeActive <- false)

  member _.ContentTranslate = translateAnim
  member _.ContentOpacity = opacityAnim

  override this.InputChanged(input: MetroPivotInput) =
    currentInput <- input
    let items = if isNull input.Items then Array.empty else input.Items
    let nextIdx = PivotInternals.findItemIndex(items, input.SelectedId)

    let reducedMotion =
      scopedReducedMotion
      || if not(isNull this.Host) then
           Metro.readWindow(this.Host).ReducedMotion
         else
           lastReducedMotion

    let selectionChanged =
      lastSelectedId
      |> ValueOption.map(fun prevId -> prevId <> input.SelectedId)
      |> ValueOption.defaultValue false

    if selectionChanged && nextIdx >= 0 then
      if reducedMotion then
        translateAnim.Snap(PivotInternals.zeroPoint)
        opacityAnim.Snap(1.0)
      else
        let dir =
          PivotInternals.transitionDirection(
            lastSelectedIndex,
            nextIdx,
            items.Length,
            input.Loop
          )

        translateAnim.Play(
          Point(X = dir * PivotInternals.slideDistancePx, Y = 0.0),
          PivotInternals.zeroPoint,
          MetroMotion.normal
        )

        opacityAnim.Play(0.0, 1.0, MetroMotion.normal)
    elif lastSelectedId.IsNone then
      translateAnim.Snap(PivotInternals.zeroPoint)
      opacityAnim.Snap(1.0)

    lastSelectedId <- ValueSome input.SelectedId
    lastSelectedIndex <- nextIdx

  override _.Unmounting() =
    swipeActive <- false
    lastSelectedId <- ValueNone
    lastSelectedIndex <- -1
    scopedReducedMotion <- false
    translateAnim.Snap(PivotInternals.zeroPoint)
    opacityAnim.Snap(1.0)

  override this.Build(input: MetroPivotInput) : Blob =
    currentInput <- input
    let theme = Metro.read()

    let windowReduced =
      if not(isNull this.Host) then
        Metro.readWindow(this.Host).ReducedMotion
      else
        false

    scopedReducedMotion <- theme.ReducedMotion && not windowReduced
    lastReducedMotion <- theme.ReducedMotion

    if
      theme.ReducedMotion && (translateAnim.Running || opacityAnim.Running)
    then
      translateAnim.Snap(PivotInternals.zeroPoint)
      opacityAnim.Snap(1.0)

    let p = theme.Palette
    let items = if isNull input.Items then Array.empty else input.Items
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
        OutlineOffset = Length.Pixels 2.0
      )

    let hoverStyle = Style(Color = p.Foreground)

    let tabListName =
      input.AccessibilityName
      |> ValueOption.orElse input.Title
      |> ValueOption.defaultValue "Pivot"

    let headerStrip =
      Container(
        FlexDirection = FlexDirection.Row,
        FlexWrap = FlexWrap.NoWrap,
        AlignItems = AlignItems.Baseline,
        Gap = Length.Pixels 24.0,
        OverflowX = Overflow.Hidden,
        OverflowY = Overflow.Visible,
        FlexShrink = 0.0,
        Width = Length.Percent 100.0,
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
            onSelect itemId)

      let tabKeyBindings = [|
        KeyBinding(Key.Enter, KeyModifiers.None, selectAction)
        KeyBinding(Key.Space, KeyModifiers.None, selectAction)
        KeyBinding(Key.Left, KeyModifiers.None, onKeyLeft)
        KeyBinding(Key.Right, KeyModifiers.None, onKeyRight)
        KeyBinding(Key.Home, KeyModifiers.None, onKeyHome)
        KeyBinding(Key.End, KeyModifiers.None, onKeyEnd)
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

      headerStrip.Add(tabBtn)

    let activeHeader =
      if selectedIdx >= 0 && selectedIdx < items.Length then
        items[selectedIdx].Header
      else
        ""

    let contentPanel =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Overflow = Overflow.Hidden,
        TranslateAnim = translateAnim,
        OpacityAnim = opacityAnim,
        OnPointerDown = onPointerDown,
        OnPointerMove = onPointerMove,
        OnPointerUp = onPointerUp,
        OnPointerCancel = onPointerCancel,
        Accessibility =
          Accessibility(Role = AccessibilityRole.TabPanel, Name = activeHeader)
      )

    if selectedIdx >= 0 && selectedIdx < items.Length then
      contentPanel.Add(renderContent items[selectedIdx].Id)

    let root =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Gap = Length.Pixels 16.0,
        KeyBindings = navKeyBindings
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

  let createFactory
    (onSelect: string -> unit, renderContent: string -> Blob)
    : Func<MetroPivotCell> =
    Func<MetroPivotCell>(fun () -> MetroPivotCell(onSelect, renderContent))

  let mount
    (key: string, input: MetroPivotInput, factory: Func<MetroPivotCell>)
    : Blob =
    Cell.Mount<MetroPivotInput, MetroPivotCell>(key, input, factory)
