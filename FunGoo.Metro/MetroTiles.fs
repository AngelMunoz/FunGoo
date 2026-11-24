namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open Goo.Widgets.Icons
open FunGoo.Children

[<Struct>]
type MetroTileSize =
  | Small
  | Medium
  | Wide
  | Large

/// The arguments of a tile grid layout.
type TileGridArgs = {
  /// Single-cell edge length in logical pixels.
  CellSize: float
  /// Gutter width between cells in logical pixels.
  Gap: float
  /// Column count of the grid.
  Columns: int
  /// The tile size of each child.
  Sizes: MetroTileSize array
}

/// The resolved values of a tile grid layout.
type TileGridSpec = {
  CellSize: float
  Gap: float
  Columns: int
  Sizes: MetroTileSize array
}

[<RequireQualifiedAccess>]
module MetroTileMetrics =

  let cellSpans(size: MetroTileSize) : struct (int * int) =
    match size with
    | MetroTileSize.Small -> struct (1, 1)
    | MetroTileSize.Medium -> struct (2, 2)
    | MetroTileSize.Wide -> struct (4, 2)
    | MetroTileSize.Large -> struct (4, 4)

  let pixelSize
    (cellSize: float, gap: float, size: MetroTileSize)
    : struct (float * float) =
    let c =
      if Double.IsFinite cellSize && cellSize > 0.0 then
        cellSize
      else
        72.0

    let g = if Double.IsFinite gap && gap >= 0.0 then gap else 0.0
    let struct (colSpan, rowSpan) = cellSpans size
    let w = float colSpan * c + float(colSpan - 1) * g
    let h = float rowSpan * c + float(rowSpan - 1) * g
    struct (w, h)

module private TileInternals =

  let emptyShadows: BoxShadow array = Array.empty
  let zeroRadius = Length.Pixels 0.0
  let zeroBorder = Edges(Length.Pixels 0.0)
  let hcBorder = Edges(Length.Pixels 2.0)
  let transparentBorder = Edges<Color>(Color.Transparent)
  let zeroPoint = Point(X = 0.0, Y = 0.0)
  let pressScaleTransform = PanelTransform(Scale = 0.97)

  let resolveCellSize(cellSize: float) : float =
    if Double.IsFinite cellSize && cellSize > 0.0 then
      cellSize
    else
      72.0

  let resolveGap(gap: float) : float =
    if Double.IsFinite gap && gap >= 0.0 then gap else 8.0

  let resolveColumns(columns: int) : int = if columns >= 1 then columns else 4

  let resolveIntervalMs(intervalMs: float) : float =
    if Double.IsFinite intervalMs && intervalMs >= 100.0 then
      intervalMs
    else
      4500.0

  let iconSizeFor(size: MetroTileSize) : float =
    match size with
    | MetroTileSize.Small -> 28.0
    | MetroTileSize.Medium -> 40.0
    | MetroTileSize.Wide -> 44.0
    | MetroTileSize.Large -> 56.0

  let ensureRows(grid: List<bool array>, neededRows: int, cols: int) =
    while grid.Count < neededRows do
      grid.Add(Array.zeroCreate<bool> cols)

  let canPlace
    (grid: List<bool array>, row: int, col: int, spanW: int, spanH: int)
    : bool =
    let mutable ok = true
    let mutable r = row

    while r < row + spanH && ok do
      if r < grid.Count then
        let rowArr = grid[r]
        let mutable c = col

        while c < col + spanW && ok do
          if rowArr[c] then
            ok <- false

          c <- c + 1

      r <- r + 1

    ok

  let markOccupied
    (
      grid: List<bool array>,
      cols: int,
      row: int,
      col: int,
      spanW: int,
      spanH: int
    ) =
    ensureRows(grid, row + spanH, cols)

    for r = row to row + spanH - 1 do
      let rowArr = grid[r]

      for c = col to col + spanW - 1 do
        rowArr[c] <- true

  let packTiles
    (spec: TileGridSpec, childCount: int)
    : struct (ElementRect array * float * float) =
    if childCount <= 0 then
      struct (Array.empty, 0.0, 0.0)
    else
      let cellSize = spec.CellSize
      let gap = spec.Gap
      let cols = spec.Columns
      let sizes = spec.Sizes
      let rects = Array.zeroCreate<ElementRect> childCount
      let grid = List<bool array>()
      let step = cellSize + gap
      let mutable maxRowEnd = 0
      let mutable maxColEnd = 0

      for i = 0 to childCount - 1 do
        let tileSize =
          if i < sizes.Length then sizes[i] else MetroTileSize.Medium

        let struct (rawW, spanH) = MetroTileMetrics.cellSpans tileSize
        let spanW = Math.Min(rawW, cols)
        let mutable placed = false
        let mutable row = 0
        let mutable chosenRow = 0
        let mutable chosenCol = 0

        while not placed do
          let mutable col = 0

          while col <= cols - spanW && not placed do
            if canPlace(grid, row, col, spanW, spanH) then
              chosenRow <- row
              chosenCol <- col
              placed <- true

            col <- col + 1

          row <- row + 1

        markOccupied(grid, cols, chosenRow, chosenCol, spanW, spanH)

        let x = float chosenCol * step
        let y = float chosenRow * step
        let w = float spanW * cellSize + float(spanW - 1) * gap
        let h = float spanH * cellSize + float(spanH - 1) * gap
        rects[i] <- ElementRect(X = x, Y = y, Width = w, Height = h)

        if chosenCol + spanW > maxColEnd then
          maxColEnd <- chosenCol + spanW

        if chosenRow + spanH > maxRowEnd then
          maxRowEnd <- chosenRow + spanH

      let totalW =
        if maxColEnd > 0 then
          float maxColEnd * cellSize + float(maxColEnd - 1) * gap
        else
          0.0

      let totalH =
        if maxRowEnd > 0 then
          float maxRowEnd * cellSize + float(maxRowEnd - 1) * gap
        else
          0.0

      struct (rects, totalW, totalH)

[<RequireQualifiedAccess>]
module TileGridLayout =

  let resolve(args: TileGridArgs) : TileGridSpec = {
    CellSize = TileInternals.resolveCellSize args.CellSize
    Gap = TileInternals.resolveGap args.Gap
    Columns = TileInternals.resolveColumns args.Columns
    Sizes =
      if isNull args.Sizes then
        Array.empty
      else
        Array.copy args.Sizes
  }

  let private measure(spec: TileGridSpec, context: LayoutContext) : LayoutSize =
    let struct (rects, totalW, totalH) =
      TileInternals.packTiles(spec, context.ChildCount)

    for i = 0 to rects.Length - 1 do
      let r = rects[i]
      context.MeasureChild(i, LayoutSize(r.Width, r.Height)) |> ignore

    LayoutSize(totalW, totalH)

  let private arrange(spec: TileGridSpec, context: LayoutContext) : unit =
    let struct (rects, _, _) = TileInternals.packTiles(spec, context.ChildCount)

    for i = 0 to rects.Length - 1 do
      context.ArrangeChild(i, rects[i])

  let create(args: TileGridArgs) : LayoutAlgorithm =
    let spec = resolve args

    {
      new LayoutAlgorithm with
        member _.Measure
          (context: LayoutContext, _available: LayoutSize)
          : LayoutSize =
          measure(spec, context)

        member _.Arrange
          (context: LayoutContext, _finalSize: LayoutSize)
          : unit =
          arrange(spec, context)
    }

[<Struct; NoComparison>]
type MetroIconicTileProps = {
  Title: string
  IconName: string
  Count: int voption
  Size: MetroTileSize
  CellSize: float
  Gap: float
  AccentOverride: Color voption
  Disabled: bool
}

[<RequireQualifiedAccess>]
module MetroIconicTile =

  let defaults(title: string, iconName: string) : MetroIconicTileProps = {
    Title = title
    IconName = iconName
    Count = ValueNone
    Size = MetroTileSize.Medium
    CellSize = 72.0
    Gap = 8.0
    AccentOverride = ValueNone
    Disabled = false
  }

  let build(props: MetroIconicTileProps, onClick: unit -> unit) : Blob =
    let theme = Metro.read()
    let p = theme.Palette

    let accent =
      props.AccentOverride
      |> ValueOption.map MetroAccent.fromColor
      |> ValueOption.defaultValue theme.Accent

    let struct (w, h) =
      MetroTileMetrics.pixelSize(props.CellSize, props.Gap, props.Size)

    let title = if isNull props.Title then "" else props.Title
    let iconName = if isNull props.IconName then "apps" else props.IconName
    let iconSize = TileInternals.iconSizeFor props.Size

    let icon =
      MaterialIcons.Create(iconName, iconSize, Nullable accent.OnAccent)

    let hoverBg = if theme.IsDark then accent.Light else accent.Dark
    let hoverStyle = Style(BackgroundColor = hoverBg)
    let activeStyle = Style(BackgroundColor = accent.Dark)

    if not theme.ReducedMotion then
      activeStyle.Transform <- TileInternals.pressScaleTransform

    let disabledStyle =
      Style(
        Color = accent.OnAccent,
        Opacity = (if theme.HighContrast then 1.0 else 0.4)
      )

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let borderWidth =
      if theme.HighContrast then
        TileInternals.hcBorder
      else
        TileInternals.zeroBorder

    let borderColor =
      if theme.HighContrast then
        Edges<Color>(p.Foreground)
      else
        TileInternals.transparentBorder

    let pad = if props.Size = MetroTileSize.Small then 6.0 else 12.0

    let accName =
      props.Count
      |> ValueOption.map(fun c ->
        if String.IsNullOrEmpty title then
          $"{c}"
        else
          $"{title}, {c}")
      |> ValueOption.defaultValue title

    let centerRow =
      Container(
        FlexDirection = FlexDirection.Row,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        FlexGrow = 1.0,
        Gap = Length.Pixels 10.0
      )
        .Children(icon)

    props.Count
    |> ValueOption.iter(fun count ->
      let countSize = if props.Size = MetroTileSize.Small then 18.0 else 32.0

      centerRow.Add(
        Text(
          Content = string count,
          Color = accent.OnAccent,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(countSize * theme.TextScaleFactor),
          FontWeight = 300.0,
          TextWrap = TextWrap.NoWrap
        )
      ))

    let tileBtn =
      Button(
        Width = Length.Pixels w,
        Height = Length.Pixels h,
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.SpaceBetween,
        AlignItems = AlignItems.Stretch,
        FlexShrink = 0.0,
        Padding = Edges(Length.Pixels pad),
        BorderRadius = TileInternals.zeroRadius,
        BoxShadows = TileInternals.emptyShadows,
        BorderWidth = borderWidth,
        BorderColor = borderColor,
        BackgroundColor = accent.Base,
        Color = accent.OnAccent,
        HoverStyle = hoverStyle,
        ActiveStyle = activeStyle,
        DisabledStyle = disabledStyle,
        FocusStyle = focusStyle,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if props.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not props.Disabled,
        TabStop = not props.Disabled,
        Disabled = props.Disabled,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Button, Name = accName)
      )

    if not props.Disabled then
      let clickAction = Action(onClick)
      tileBtn.OnClick <- clickAction

      tileBtn.KeyBindings <- [|
        KeyBinding(Key.Enter, KeyModifiers.None, clickAction)
        KeyBinding(Key.Space, KeyModifiers.None, clickAction)
      |]

    tileBtn.Add(centerRow)

    if props.Size <> MetroTileSize.Small && not(String.IsNullOrEmpty title) then
      tileBtn.Add(
        Text(
          Content = title,
          Color = accent.OnAccent,
          FontFamily = theme.FontFamily,
          FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
          FontWeight = 400.0,
          TextWrap = TextWrap.NoWrap,
          TextTrimming = TextTrimming.Ellipsis
        )
      )

    tileBtn

[<Struct; NoComparison>]
type MetroLiveTileFace = {
  Title: string
  Subtitle: string voption
  Metric: string voption
  AccentOverride: Color voption
}

[<Struct; NoComparison>]
type MetroLiveTileInput = {
  Faces: MetroLiveTileFace array
  Size: MetroTileSize
  CellSize: float
  Gap: float
  IntervalMs: float
  Disabled: bool
  AccessibilityName: string voption
}

[<AbstractClass>]
type MetroLiveTileCell() =
  inherit Cell<MetroLiveTileInput>()

  abstract ActiveFaceIndex: int

  abstract IsTimerActive: bool

  abstract View: Blob

  // Gives a click of the host to the host cell, as a Goo.Widgets control
  // does. The owner rule then rebuilds the host cell.
  member this.Raise(callback: Action) = this.InvokeInputCallback(callback)

  // Runs an action again and again on the window timer of this cell. `Every`
  // is a protected member of the cell, and F# does not permit a call to it
  // from a function outside the type.
  member internal this.ScheduleEvery
    (intervalMs: float, action: Action)
    : WindowTimer =
    this.Every(intervalMs, action)

[<RequireQualifiedAccess>]
module MetroLiveTile =

  let defaults(faces: MetroLiveTileFace array) : MetroLiveTileInput = {
    Faces = faces
    Size = MetroTileSize.Wide
    CellSize = 72.0
    Gap = 8.0
    IntervalMs = 4500.0
    Disabled = false
    AccessibilityName = ValueNone
  }

  /// State of a live tile cell. One mutable field per concern.
  type private LiveTileState = {
    mutable Input: MetroLiveTileInput
    mutable AttachedHost: Window voption
    mutable FaceIndex: int
    mutable Timer: WindowTimer voption
    mutable ActiveIntervalMs: float
    mutable IsMounted: bool
    mutable LastReducedMotion: bool
    mutable ScopedReducedMotion: bool
    // Slots for the two animation values. `create` fills them after the cell
    // exists.
    mutable Translate: Anim<Point>
    mutable Opacity: Anim<float>
  }

  /// The values that the functions of a live tile cell work on.
  type private LiveTileContext = {
    State: LiveTileState
    /// Gives one click to the host cell.
    Click: Action
    /// The bindings of the Enter key and of the Space key. `create` makes
    /// them once.
    Keys: KeyBinding array
  }

  /// The host window with the handler of its preferences.
  type private HostAttachment = {
    Host: Window voption
    Handler: Action<PlatformPreferences>
  }

  let private emptyInput: MetroLiveTileInput = {
    Faces = Array.empty
    Size = MetroTileSize.Wide
    CellSize = 72.0
    Gap = 8.0
    IntervalMs = 4500.0
    Disabled = false
    AccessibilityName = ValueNone
  }

  let private initialState() : LiveTileState = {
    Input = emptyInput
    AttachedHost = ValueNone
    FaceIndex = 0
    Timer = ValueNone
    ActiveIntervalMs = 0.0
    IsMounted = false
    LastReducedMotion = false
    ScopedReducedMotion = false
    Translate = Unchecked.defaultof<Anim<Point>>
    Opacity = Unchecked.defaultof<Anim<float>>
  }

  let private stopTimer(state: LiveTileState) : unit =
    state.Timer |> ValueOption.iter(fun timer -> timer.Dispose())
    state.Timer <- ValueNone
    state.ActiveIntervalMs <- 0.0

  /// <c>true</c> when a scope or the host window asks for reduced motion.
  let private isReducedMotion(state: LiveTileState) : bool =
    state.ScopedReducedMotion
    || (state.AttachedHost
        |> ValueOption.map(fun w -> Metro.readWindow(w).ReducedMotion)
        |> ValueOption.defaultValue state.LastReducedMotion)

  let private isTimerActive(state: LiveTileState) : bool =
    state.Timer
    |> ValueOption.map(fun timer -> timer.IsActive)
    |> ValueOption.defaultValue false

  /// Puts the face at rest: no translation and fully opaque.
  let private snapToRest(state: LiveTileState) : unit =
    state.Translate.Snap(TileInternals.zeroPoint)
    state.Opacity.Snap(1.0)

  /// Moves to the next face with a slide. It stops the cycle when there is
  /// one face or reduced motion.
  let private advanceFace
    (cell: MetroLiveTileCell, ctx: LiveTileContext)
    : unit =
    let state = ctx.State
    let faces = state.Input.Faces

    if isNull faces || faces.Length <= 1 || isReducedMotion state then
      stopTimer state
      snapToRest state
    else
      state.FaceIndex <- (state.FaceIndex + 1) % faces.Length

      state.Translate.Play(
        Point(X = 0.0, Y = 24.0),
        TileInternals.zeroPoint,
        MetroMotion.normal
      )

      state.Opacity.Play(0.0, 1.0, MetroMotion.normal)
      cell.Rebuild()

  /// Starts, keeps, or stops the cycle timer for the input.
  let private syncTimer
    (cell: MetroLiveTileCell, ctx: LiveTileContext, input: MetroLiveTileInput)
    : unit =
    let state = ctx.State
    let faceCount = if isNull input.Faces then 0 else input.Faces.Length
    let interval = TileInternals.resolveIntervalMs input.IntervalMs

    // A mounted window that is not open, such as a `goo render` frame,
    // rejects window timers, so an open host window is required to cycle.
    let hostOpen =
      state.AttachedHost
      |> ValueOption.map(fun w -> w.IsOpen)
      |> ValueOption.defaultValue false

    let shouldRun =
      state.IsMounted && hostOpen && faceCount > 1 && not(isReducedMotion state)

    if not shouldRun then
      stopTimer state
    elif state.Timer.IsNone || state.ActiveIntervalMs <> interval then
      stopTimer state
      state.ActiveIntervalMs <- interval

      let advance = Action(fun () -> advanceFace(cell, ctx))
      state.Timer <- ValueSome(cell.ScheduleEvery(interval, advance))

  /// Keeps the index of the face inside the new faces, then syncs the timer.
  let private inputChanged
    (cell: MetroLiveTileCell, ctx: LiveTileContext, input: MetroLiveTileInput)
    : unit =
    let state = ctx.State
    state.Input <- input
    let count = if isNull input.Faces then 0 else input.Faces.Length

    if count = 0 then
      state.FaceIndex <- 0
    elif state.FaceIndex >= count then
      state.FaceIndex <- 0

    if state.IsMounted then
      syncTimer(cell, ctx, input)

  /// Subscribes the handler to the preferences of the host window, then
  /// syncs the timer.
  let private mounted
    (cell: MetroLiveTileCell, ctx: LiveTileContext, attachment: HostAttachment)
    : unit =
    let state = ctx.State
    state.IsMounted <- true
    state.AttachedHost <- attachment.Host

    attachment.Host
    |> ValueOption.iter(fun w -> w.add_PreferencesChanged attachment.Handler)

    syncTimer(cell, ctx, state.Input)

  /// Unsubscribes the handler, stops the timer, and puts the face at rest.
  let private unmount
    (ctx: LiveTileContext, handler: Action<PlatformPreferences>)
    : unit =
    let state = ctx.State
    state.IsMounted <- false

    state.AttachedHost
    |> ValueOption.iter(fun w -> w.remove_PreferencesChanged handler)

    state.AttachedHost <- ValueNone
    state.ScopedReducedMotion <- false
    stopTimer state
    snapToRest state

  let private view
    (ctx: LiveTileContext, input: MetroLiveTileInput, theme: MetroTheme)
    : Blob =
    let state = ctx.State
    let p = theme.Palette
    let faces = if isNull input.Faces then Array.empty else input.Faces

    let faceOpt =
      if faces.Length = 0 then
        ValueNone
      else
        let idx = Math.Clamp(state.FaceIndex, 0, faces.Length - 1)
        ValueSome faces[idx]

    let accent =
      faceOpt
      |> ValueOption.bind(fun f -> f.AccentOverride)
      |> ValueOption.map MetroAccent.fromColor
      |> ValueOption.defaultValue theme.Accent

    let struct (w, h) =
      MetroTileMetrics.pixelSize(input.CellSize, input.Gap, input.Size)

    let hoverBg = if theme.IsDark then accent.Light else accent.Dark
    let hoverStyle = Style(BackgroundColor = hoverBg)
    let activeStyle = Style(BackgroundColor = accent.Dark)

    if not theme.ReducedMotion then
      activeStyle.Transform <- TileInternals.pressScaleTransform

    let disabledStyle =
      Style(
        Color = accent.OnAccent,
        Opacity = (if theme.HighContrast then 1.0 else 0.4)
      )

    let focusStyle =
      Style(
        OutlineWidth = Length.Pixels 2.0,
        OutlineColor = p.Foreground,
        OutlineOffset = Length.Pixels 2.0
      )

    let borderWidth =
      if theme.HighContrast then
        TileInternals.hcBorder
      else
        TileInternals.zeroBorder

    let borderColor =
      if theme.HighContrast then
        Edges<Color>(p.Foreground)
      else
        TileInternals.transparentBorder

    let activeTitle =
      faceOpt
      |> ValueOption.map(fun f -> if isNull f.Title then "" else f.Title)
      |> ValueOption.defaultValue ""

    let accName =
      input.AccessibilityName |> ValueOption.defaultValue activeTitle

    let faceContainer =
      Container(
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.SpaceBetween,
        AlignItems = AlignItems.Stretch,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Height = Length.Percent 100.0,
        TranslateAnim = state.Translate,
        OpacityAnim = state.Opacity
      )

    faceOpt
    |> ValueOption.iter(fun face ->
      let bodyGroup =
        Container(
          FlexDirection = FlexDirection.Column,
          Gap = Length.Pixels 4.0,
          FlexGrow = 1.0
        )

      face.Metric
      |> ValueOption.iter(fun metric ->
        bodyGroup.Add(
          Text(
            Content = metric,
            Color = accent.OnAccent,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(34.0 * theme.TextScaleFactor),
            FontWeight = 300.0,
            LineHeight = 1.05,
            TextWrap = TextWrap.NoWrap,
            TextTrimming = TextTrimming.Ellipsis
          )
        ))

      face.Subtitle
      |> ValueOption.iter(fun subtitle ->
        bodyGroup.Add(
          Text(
            Content = subtitle,
            Color = accent.OnAccent,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(13.0 * theme.TextScaleFactor),
            FontWeight = 400.0,
            TextMaxLines = 2,
            TextTrimming = TextTrimming.Ellipsis
          )
        ))

      faceContainer.Add(bodyGroup)

      if not(String.IsNullOrEmpty activeTitle) then
        faceContainer.Add(
          Text(
            Content = activeTitle,
            Color = accent.OnAccent,
            FontFamily = theme.FontFamily,
            FontSize = Length.Pixels(12.0 * theme.TextScaleFactor),
            FontWeight = 400.0,
            TextWrap = TextWrap.NoWrap,
            TextTrimming = TextTrimming.Ellipsis
          )
        ))

    let tileBtn =
      Button(
        Width = Length.Pixels w,
        Height = Length.Pixels h,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Stretch,
        FlexShrink = 0.0,
        Padding = Edges(Length.Pixels 12.0),
        BorderRadius = TileInternals.zeroRadius,
        BoxShadows = TileInternals.emptyShadows,
        BorderWidth = borderWidth,
        BorderColor = borderColor,
        BackgroundColor = accent.Base,
        Color = accent.OnAccent,
        HoverStyle = hoverStyle,
        ActiveStyle = activeStyle,
        DisabledStyle = disabledStyle,
        FocusStyle = focusStyle,
        TransitionMs = MetroTheme.effectiveMs(theme, MetroTiming.fastMs),
        TransitionEasing = Easing.EaseOut,
        Cursor = (if input.Disabled then Cursor.Default else Cursor.Pointer),
        Focusable = not input.Disabled,
        TabStop = not input.Disabled,
        Disabled = input.Disabled,
        Overflow = Overflow.Hidden,
        Accessibility =
          Accessibility(Role = AccessibilityRole.Button, Name = accName)
      )

    if not input.Disabled then
      tileBtn.OnClick <- ctx.Click
      tileBtn.KeyBindings <- ctx.Keys

    tileBtn.Children(faceContainer)

  /// Reads the theme, stops the cycle and the animations under reduced
  /// motion, then builds the tree.
  let private build
    (ctx: LiveTileContext, input: MetroLiveTileInput, hostReduced: bool voption)
    : Blob =
    let state = ctx.State
    state.Input <- input
    let theme = Metro.read()
    let windowReduced = ValueOption.defaultValue false hostReduced
    state.ScopedReducedMotion <- theme.ReducedMotion && not windowReduced
    state.LastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      stopTimer state
      snapToRest state

    view(ctx, input, theme)

  /// The reduced motion preference of the host window. <c>ValueNone</c> when
  /// the cell has no window.
  let private reducedMotionOf(host: Window) : bool voption =
    if isNull host then
      ValueNone
    else
      ValueSome(Metro.readWindow(host).ReducedMotion)

  /// The window of a cell with the handler of its preferences.
  let private attachmentOf
    (host: Window, handler: Action<PlatformPreferences>)
    : HostAttachment = {
    Host = (if isNull host then ValueNone else ValueSome host)
    Handler = handler
  }

  let create(onClick: unit -> unit) : MetroLiveTileCell =
    let state = initialState()

    // The slot of the cell: the click delegate is made before the cell exists.
    let mutable self: MetroLiveTileCell voption = ValueNone
    let onClickAction = Action(onClick)

    // One delegate for the life of the cell. It gives each click to the host
    // cell, so the owner rule rebuilds the host.
    let click =
      Action(fun () ->
        self |> ValueOption.iter(fun cell -> cell.Raise(onClickAction)))

    let keys = [|
      KeyBinding(Key.Enter, KeyModifiers.None, click)
      KeyBinding(Key.Space, KeyModifiers.None, click)
    |]

    let context: LiveTileContext = {
      State = state
      Click = click
      Keys = keys
    }

    // One delegate for the life of the cell. The host window subscribes to it.
    let onPreferencesChanged =
      Action<PlatformPreferences>(fun _ ->
        self
        |> ValueOption.iter(fun cell -> syncTimer(cell, context, state.Input)))

    let cell = {
      new MetroLiveTileCell() with
        override _.ActiveFaceIndex = state.FaceIndex
        override _.IsTimerActive = isTimerActive state

        override _.View = view(context, state.Input, Metro.read())

        override this.Mounted() =
          mounted(this, context, attachmentOf(this.Host, onPreferencesChanged))

        override this.InputChanged(input) = inputChanged(this, context, input)

        override _.Unmounting() = unmount(context, onPreferencesChanged)

        override this.Build(input) : Blob =
          build(context, input, reducedMotionOf this.Host)
    }

    // The cell exists now: Goo ties each animation to it.
    state.Translate <- cell.AnimateValue(TileInternals.zeroPoint)
    state.Opacity <- cell.AnimateValue(1.0)
    self <- ValueSome cell
    cell

  let createFactory(onClick: unit -> unit) : Func<MetroLiveTileCell> =
    Func<MetroLiveTileCell>(fun () -> create onClick)

  let mount
    (key: string, input: MetroLiveTileInput, factory: Func<MetroLiveTileCell>)
    : Blob =
    Cell.Mount<MetroLiveTileInput, MetroLiveTileCell>(key, input, factory)
