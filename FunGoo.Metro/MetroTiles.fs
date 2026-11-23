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
    (
      cellSize: float,
      gap: float,
      cols: int,
      sizes: MetroTileSize array,
      childCount: int
    ) : struct (ElementRect array * float * float) =
    if childCount <= 0 then
      struct (Array.empty, 0.0, 0.0)
    else
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

[<Sealed>]
type TileGridLayout
  (cellSize: float, gap: float, columns: int, sizes: MetroTileSize array) =

  let resolvedCell = TileInternals.resolveCellSize cellSize
  let resolvedGap = TileInternals.resolveGap gap
  let resolvedColumns = TileInternals.resolveColumns columns

  let resolvedSizes = if isNull sizes then Array.empty else Array.copy sizes

  member _.CellSize = resolvedCell
  member _.Gap = resolvedGap
  member _.Columns = resolvedColumns

  interface LayoutAlgorithm with
    member _.Measure
      (context: LayoutContext, available: LayoutSize)
      : LayoutSize =
      ignore available

      let struct (rects, totalW, totalH) =
        TileInternals.packTiles(
          resolvedCell,
          resolvedGap,
          resolvedColumns,
          resolvedSizes,
          context.ChildCount
        )

      for i = 0 to rects.Length - 1 do
        let r = rects[i]
        context.MeasureChild(i, LayoutSize(r.Width, r.Height)) |> ignore

      LayoutSize(totalW, totalH)

    member _.Arrange(context: LayoutContext, finalSize: LayoutSize) =
      ignore finalSize

      let struct (rects, _, _) =
        TileInternals.packTiles(
          resolvedCell,
          resolvedGap,
          resolvedColumns,
          resolvedSizes,
          context.ChildCount
        )

      for i = 0 to rects.Length - 1 do
        context.ArrangeChild(i, rects[i])

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

[<Sealed>]
type MetroLiveTileCell(onClick: unit -> unit) as this =
  inherit Cell<MetroLiveTileInput>()

  let translateAnim = this.AnimateValue(TileInternals.zeroPoint)
  let opacityAnim = this.AnimateValue(1.0)
  let clickAction = Action(onClick)

  let keyBindings = [|
    KeyBinding(Key.Enter, KeyModifiers.None, clickAction)
    KeyBinding(Key.Space, KeyModifiers.None, clickAction)
  |]

  let mutable currentInput: MetroLiveTileInput = {
    Faces = Array.empty
    Size = MetroTileSize.Wide
    CellSize = 72.0
    Gap = 8.0
    IntervalMs = 4500.0
    Disabled = false
    AccessibilityName = ValueNone
  }

  let mutable attachedHost: Window voption = ValueNone
  let mutable faceIndex = 0
  let mutable timer: WindowTimer voption = ValueNone
  let mutable activeIntervalMs = 0.0
  let mutable isMounted = false
  let mutable lastReducedMotion = false
  let mutable scopedReducedMotion = false

  let stopTimer() =
    timer |> ValueOption.iter(fun t -> t.Dispose())
    timer <- ValueNone
    activeIntervalMs <- 0.0

  let isReducedMotion() =
    scopedReducedMotion
    || (attachedHost
        |> ValueOption.map(fun w -> Metro.readWindow(w).ReducedMotion)
        |> ValueOption.defaultValue lastReducedMotion)

  let onPreferencesChanged =
    Action<PlatformPreferences>(fun _ -> this.SyncTimer currentInput)

  member private this.AdvanceFace() =
    let faces = currentInput.Faces

    if isNull faces || faces.Length <= 1 || isReducedMotion() then
      stopTimer()
      translateAnim.Snap(TileInternals.zeroPoint)
      opacityAnim.Snap(1.0)
    else
      faceIndex <- (faceIndex + 1) % faces.Length

      translateAnim.Play(
        Point(X = 0.0, Y = 24.0),
        TileInternals.zeroPoint,
        MetroMotion.normal
      )

      opacityAnim.Play(0.0, 1.0, MetroMotion.normal)
      this.Rebuild()

  member private this.SyncTimer(input: MetroLiveTileInput) =
    let faceCount = if isNull input.Faces then 0 else input.Faces.Length
    let interval = TileInternals.resolveIntervalMs input.IntervalMs

    let shouldRun =
      isMounted
      && attachedHost.IsSome
      && faceCount > 1
      && not(isReducedMotion())

    if not shouldRun then
      stopTimer()
    elif timer.IsNone || activeIntervalMs <> interval then
      stopTimer()
      activeIntervalMs <- interval
      timer <- ValueSome(this.Every(interval, Action(this.AdvanceFace)))

  member _.ActiveFaceIndex = faceIndex

  member _.IsTimerActive =
    timer
    |> ValueOption.map(fun t -> t.IsActive)
    |> ValueOption.defaultValue false

  member _.FaceTranslate = translateAnim
  member _.FaceOpacity = opacityAnim

  override this.Mounted() =
    isMounted <- true
    attachedHost <- if isNull this.Host then ValueNone else ValueSome this.Host

    attachedHost
    |> ValueOption.iter(fun w -> w.add_PreferencesChanged onPreferencesChanged)

    this.SyncTimer currentInput

  override this.InputChanged(input: MetroLiveTileInput) =
    currentInput <- input
    let count = if isNull input.Faces then 0 else input.Faces.Length

    if count = 0 then
      faceIndex <- 0
    elif faceIndex >= count then
      faceIndex <- 0

    if isMounted then
      this.SyncTimer input

  override _.Unmounting() =
    isMounted <- false

    attachedHost
    |> ValueOption.iter(fun w ->
      w.remove_PreferencesChanged onPreferencesChanged)

    attachedHost <- ValueNone
    scopedReducedMotion <- false
    stopTimer()
    translateAnim.Snap(TileInternals.zeroPoint)
    opacityAnim.Snap(1.0)

  override this.Build(input: MetroLiveTileInput) : Blob =
    currentInput <- input
    let theme = Metro.read()

    let windowReduced =
      if not(isNull this.Host) then
        Metro.readWindow(this.Host).ReducedMotion
      else
        false

    scopedReducedMotion <- theme.ReducedMotion && not windowReduced
    lastReducedMotion <- theme.ReducedMotion

    if theme.ReducedMotion then
      stopTimer()
      translateAnim.Snap(TileInternals.zeroPoint)
      opacityAnim.Snap(1.0)

    let p = theme.Palette
    let faces = if isNull input.Faces then Array.empty else input.Faces

    let faceOpt =
      if faces.Length = 0 then
        ValueNone
      else
        let idx = Math.Clamp(faceIndex, 0, faces.Length - 1)
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
        TranslateAnim = translateAnim,
        OpacityAnim = opacityAnim
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
      tileBtn.OnClick <- clickAction
      tileBtn.KeyBindings <- keyBindings

    tileBtn.Children(faceContainer)

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

  let createFactory(onClick: unit -> unit) : Func<MetroLiveTileCell> =
    Func<MetroLiveTileCell>(fun () -> MetroLiveTileCell(onClick))

  let mount
    (key: string, input: MetroLiveTileInput, factory: Func<MetroLiveTileCell>)
    : Blob =
    Cell.Mount<MetroLiveTileInput, MetroLiveTileCell>(key, input, factory)
