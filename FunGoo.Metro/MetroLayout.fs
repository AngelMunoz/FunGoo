namespace FunGoo.Metro

open System
open System.Collections.Generic
open Goo
open FunGoo.Children

[<Struct>]
type MetroScrollMode =
  | Disabled
  | Auto
  | HiddenScrollbar

[<Struct; NoComparison>]
type MetroScrollViewerProps = {
  HorizontalMode: MetroScrollMode
  VerticalMode: MetroScrollMode
  Padding: Edges
  Handle: ElementHandle voption
}

[<RequireQualifiedAccess>]
module MetroScrollViewer =

  let toOverflow(mode: MetroScrollMode) : Overflow * ScrollbarVisibility =
    match mode with
    | MetroScrollMode.Disabled -> Overflow.Hidden, ScrollbarVisibility.Hidden
    | MetroScrollMode.Auto -> Overflow.Scroll, ScrollbarVisibility.Auto
    | MetroScrollMode.HiddenScrollbar ->
      Overflow.Scroll, ScrollbarVisibility.Hidden

  let vertical() : MetroScrollViewerProps = {
    HorizontalMode = MetroScrollMode.Disabled
    VerticalMode = MetroScrollMode.Auto
    Padding = Edges(Length.Pixels 0.0)
    Handle = ValueNone
  }

  let horizontal() : MetroScrollViewerProps = {
    HorizontalMode = MetroScrollMode.Auto
    VerticalMode = MetroScrollMode.Disabled
    Padding = Edges(Length.Pixels 0.0)
    Handle = ValueNone
  }

  let both() : MetroScrollViewerProps = {
    HorizontalMode = MetroScrollMode.Auto
    VerticalMode = MetroScrollMode.Auto
    Padding = Edges(Length.Pixels 0.0)
    Handle = ValueNone
  }

  let build(props: MetroScrollViewerProps, content: Blob) : Blob =
    let ox, sbx = toOverflow props.HorizontalMode
    let oy, sby = toOverflow props.VerticalMode

    let box =
      Container(
        FlexDirection = FlexDirection.Column,
        FlexGrow = 1.0,
        Width = Length.Percent 100.0,
        Padding = props.Padding,
        OverflowX = ox,
        OverflowY = oy,
        ScrollbarVisibilityX = sbx,
        ScrollbarVisibilityY = sby
      )

    props.Handle |> ValueOption.iter(fun h -> box.Handle <- h)

    if not(isNull content) then
      box.Add(content)

    box :> Blob

[<Struct>]
type MetroWrapOrientation =
  | Horizontal
  | Vertical

[<Struct; NoComparison>]
type MetroWrapPanelProps = {
  WrapOrientation: MetroWrapOrientation
  RowGap: float
  ColumnGap: float
}

[<RequireQualifiedAccess>]
module MetroWrapPanel =

  let defaults() : MetroWrapPanelProps = {
    WrapOrientation = MetroWrapOrientation.Horizontal
    RowGap = 8.0
    ColumnGap = 8.0
  }

  let withGap
    (orientation: MetroWrapOrientation, gap: float)
    : MetroWrapPanelProps = {
    WrapOrientation = orientation
    RowGap = gap
    ColumnGap = gap
  }

  let build(props: MetroWrapPanelProps, items: Blob array) : Blob =
    let rGap =
      if Double.IsFinite props.RowGap && props.RowGap >= 0.0 then
        props.RowGap
      else
        0.0

    let cGap =
      if Double.IsFinite props.ColumnGap && props.ColumnGap >= 0.0 then
        props.ColumnGap
      else
        0.0

    let panel =
      Container(
        FlexDirection =
          (match props.WrapOrientation with
           | MetroWrapOrientation.Horizontal -> FlexDirection.Row
           | MetroWrapOrientation.Vertical -> FlexDirection.Column),
        FlexWrap = FlexWrap.Wrap,
        RowGap = Length.Pixels rGap,
        ColumnGap = Length.Pixels cGap
      )

    if not(isNull items) then
      for i = 0 to items.Length - 1 do
        let child = items[i]

        if not(isNull child) then
          panel.Add(child)

    panel :> Blob

[<Struct; NoComparison>]
type MetroGridSpan = { ColSpan: int; RowSpan: int }

[<Struct; NoComparison>]
type MetroVariableGridArgs = {
  Orientation: MetroWrapOrientation
  ItemWidth: float
  ItemHeight: float
  Gap: float
  MaximumRowsOrColumns: int voption
  Spans: MetroGridSpan array
}

module VariableGridInternals =

  let resolveDim(v: float) : float =
    if Double.IsFinite v && v > 0.0 && v <= 1.0e6 then
      v
    else
      64.0

  let resolveGap(g: float) : float =
    if Double.IsFinite g && g >= 0.0 then g else 8.0

  // The int conversion overflows for a ratio of 65,536.0 or above, so the
  // cap of 65,536 keeps the conversion finite. pack bounds the limit
  // further by the summed primary span, which keeps its two arrays small
  // for tiny cells.
  let ratioLimit(ratio: float) : int =
    if Double.IsNaN ratio || ratio < 1.0 then 1
    elif ratio >= 65536.0 then 65536
    else int ratio

  let maxSpan = 1024
  let maxCap = 1024

  // One flat occupancy buffer of Limit-wide rows over the secondary axis.
  // Rows past the filled length stay false, so a scan reads them as free.
  [<Struct; NoComparison>]
  type MarkArgs = {
    Buffer: bool[]
    Limit: int
    Secondary: int
    Primary: int
    SecSpan: int
    PriSpan: int
  }

  let ensurePrimarySlots(m: MarkArgs) : bool[] =
    let needed = (int64 m.Secondary + int64 m.SecSpan) * int64 m.Limit

    if needed > int64 Int32.MaxValue then
      raise(
        InvalidOperationException(
          "The variable grid needs an occupancy buffer that is too large. Lower the item count, the spans, or MaximumRowsOrColumns."
        )
      )
    elif int64 m.Buffer.Length >= needed then
      m.Buffer
    else
      let mutable len =
        if m.Buffer.Length = 0 then
          int64 m.Limit
        else
          int64 m.Buffer.Length * 2L

      while len < needed do
        len <- len * 2L

      let len = int(min len (int64 Int32.MaxValue))
      let grown = Array.zeroCreate<bool> len
      Array.blit m.Buffer 0 grown 0 m.Buffer.Length
      grown

  let canFit(m: MarkArgs) : bool =
    let rows = m.Buffer.Length / m.Limit
    let mutable ok = true
    let mutable s = m.Secondary

    while s < m.Secondary + m.SecSpan && ok do
      if s < rows then
        let rowBase = s * m.Limit
        let mutable p = m.Primary

        while p < m.Primary + m.PriSpan && ok do
          if m.Buffer[rowBase + p] then
            ok <- false

          p <- p + 1

      s <- s + 1

    ok

  let markFit(m: MarkArgs) : bool[] =
    let buffer = ensurePrimarySlots m

    for s = m.Secondary to m.Secondary + m.SecSpan - 1 do
      let rowBase = s * m.Limit

      for p = m.Primary to m.Primary + m.PriSpan - 1 do
        buffer[rowBase + p] <- true

    buffer

[<RequireQualifiedAccess>]
module MetroVariableSizedWrapGrid =

  open VariableGridInternals

  [<Struct; NoComparison>]
  type PlacedCell = {
    Col: int
    Row: int
    ColSpan: int
    RowSpan: int
  }

  let pack
    (args: MetroVariableGridArgs, count: int, available: LayoutSize)
    : struct (PlacedCell array * int * int) =
    if count <= 0 then
      struct (Array.empty, 0, 0)
    else
      let cellW = resolveDim args.ItemWidth
      let cellH = resolveDim args.ItemHeight
      let gap = resolveGap args.Gap
      let spans = if isNull args.Spans then Array.empty else args.Spans

      let isHoriz =
        match args.Orientation with
        | MetroWrapOrientation.Horizontal -> true
        | MetroWrapOrientation.Vertical -> false

      let availLimit =
        if isHoriz then
          if Double.IsFinite available.Width && available.Width > 0.0 then
            ratioLimit((available.Width + gap) / (cellW + gap))
          else
            4
        elif Double.IsFinite available.Height && available.Height > 0.0 then
          ratioLimit((available.Height + gap) / (cellH + gap))
        else
          4

      let primaryLimit =
        args.MaximumRowsOrColumns
        |> ValueOption.bind(fun m ->
          if m >= 1 then ValueSome(min m maxCap) else ValueNone)
        |> ValueOption.map(fun m ->
          if
            (isHoriz
             && Double.IsFinite available.Width
             && available.Width > 0.0)
            || (not isHoriz
                && Double.IsFinite available.Height
                && available.Height > 0.0)
          then
            min m availLimit
          else
            m)
        |> ValueOption.defaultValue availLimit

      // A placement never uses a primary index at or beyond the summed
      // primary span: the occupied cells of one row are disjoint item
      // spans, so first-fit always finds its window inside that sum.
      // The int64 sum cannot overflow and needs no allocation.
      let spanSum =
        let mutable total = 0L
        let mutable i = 0

        while i < count && total < int64 primaryLimit do
          let raw =
            if i < spans.Length then
              spans[i]
            else
              { ColSpan = 1; RowSpan = 1 }

          let q = if isHoriz then raw.ColSpan else raw.RowSpan
          total <- total + int64(min maxSpan (max 1 q))
          i <- i + 1

        total

      let primaryLimit = min primaryLimit (int spanSum)

      let mutable buffer = Array.zeroCreate<bool> primaryLimit
      let nextFree = Array.zeroCreate<int> primaryLimit
      let placed = Array.zeroCreate<PlacedCell> count
      let mutable maxCol = 0
      let mutable maxRow = 0

      for i = 0 to count - 1 do
        let raw =
          if i < spans.Length then
            spans[i]
          else
            { ColSpan = 1; RowSpan = 1 }

        let cs = min maxSpan (max 1 raw.ColSpan)
        let rs = min maxSpan (max 1 raw.RowSpan)

        let priSpan, secSpan =
          if isHoriz then
            min primaryLimit cs, rs
          else
            min primaryLimit rs, cs

        // The smallest row over the windows of priSpan neighbor columns of
        // the largest nextFree in the window. Below it each window holds an
        // occupied column, so no window of priSpan columns fits and the scan
        // of sec can start there.
        let mutable startRow = Int32.MaxValue
        let mutable w = 0

        while w <= primaryLimit - priSpan do
          let mutable top = 0
          let mutable p = w

          while p < w + priSpan do
            if nextFree[p] > top then
              top <- nextFree[p]

            p <- p + 1

          if top < startRow then
            startRow <- top

          w <- w + 1

        let mutable sec = startRow
        let mutable chosenPri = -1

        while chosenPri < 0 do
          let mutable p = 0

          while p <= primaryLimit - priSpan && chosenPri < 0 do
            if
              canFit {
                Buffer = buffer
                Limit = primaryLimit
                Secondary = sec
                Primary = p
                SecSpan = secSpan
                PriSpan = priSpan
              }
            then
              chosenPri <- p

            p <- p + 1

          if chosenPri < 0 then
            sec <- sec + 1

        buffer <-
          markFit {
            Buffer = buffer
            Limit = primaryLimit
            Secondary = sec
            Primary = chosenPri
            SecSpan = secSpan
            PriSpan = priSpan
          }

        // nextFree[p] is the first row where column p is free. A placement
        // changes that row only when the first free row lies inside the
        // cell, so the walk starts at the bottom of the cell.
        let rows = buffer.Length / primaryLimit
        let priEnd = chosenPri + priSpan
        let mutable p = chosenPri

        while p < priEnd do
          if sec <= nextFree[p] && nextFree[p] < sec + secSpan then
            let mutable r = sec + secSpan

            while r < rows && buffer[r * primaryLimit + p] do
              r <- r + 1

            nextFree[p] <- r

          p <- p + 1

        let col, row, finalCs, finalRs =
          if isHoriz then
            chosenPri, sec, priSpan, secSpan
          else
            sec, chosenPri, secSpan, priSpan

        placed[i] <- {
          Col = col
          Row = row
          ColSpan = finalCs
          RowSpan = finalRs
        }

        if col + finalCs > maxCol then
          maxCol <- col + finalCs

        if row + finalRs > maxRow then
          maxRow <- row + finalRs

      struct (placed, maxCol, maxRow)

  let unitSpan: MetroGridSpan = { ColSpan = 1; RowSpan = 1 }

  let span(colSpan: int, rowSpan: int) : MetroGridSpan = {
    ColSpan = max 1 colSpan
    RowSpan = max 1 rowSpan
  }

  let defaults
    (itemWidth: float, itemHeight: float, spans: MetroGridSpan array)
    : MetroVariableGridArgs = {
    Orientation = MetroWrapOrientation.Horizontal
    ItemWidth = itemWidth
    ItemHeight = itemHeight
    Gap = 8.0
    MaximumRowsOrColumns = ValueNone
    Spans = spans
  }

  let createLayout(args: MetroVariableGridArgs) : LayoutAlgorithm =
    let cellW = VariableGridInternals.resolveDim args.ItemWidth
    let cellH = VariableGridInternals.resolveDim args.ItemHeight
    let gap = VariableGridInternals.resolveGap args.Gap
    // Goo layout algorithms are immutable: own a copy of the spans array
    // instead of the caller's array.
    let args = {
      args with
          Spans =
            if isNull args.Spans then
              Array.empty
            else
              Array.copy args.Spans
    }

    {
      new LayoutAlgorithm with
        member _.Measure(context, available) =
          let count = context.ChildCount

          if count = 0 then
            LayoutSize(0.0, 0.0)
          else
            let struct (placed, usedCols, usedRows) =
              pack(args, count, available)

            for i = 0 to count - 1 do
              let cell = placed[i]
              let w = float cell.ColSpan * cellW + float(cell.ColSpan - 1) * gap
              let h = float cell.RowSpan * cellH + float(cell.RowSpan - 1) * gap
              context.MeasureChild(i, LayoutSize(w, h)) |> ignore

            let totalW =
              if usedCols <= 0 then
                0.0
              else
                float usedCols * cellW + float(usedCols - 1) * gap

            let totalH =
              if usedRows <= 0 then
                0.0
              else
                float usedRows * cellH + float(usedRows - 1) * gap

            let clampedW =
              if Double.IsFinite available.Width then
                min totalW available.Width
              else
                totalW

            let clampedH =
              if Double.IsFinite available.Height then
                min totalH available.Height
              else
                totalH

            LayoutSize(max 0.0 clampedW, max 0.0 clampedH)

        member _.Arrange(context, finalSize) =
          let count = context.ChildCount

          if count > 0 then
            let struct (placed, _, _) = pack(args, count, finalSize)

            for i = 0 to count - 1 do
              let cell = placed[i]
              let x = float cell.Col * (cellW + gap)
              let y = float cell.Row * (cellH + gap)
              let w = float cell.ColSpan * cellW + float(cell.ColSpan - 1) * gap
              let h = float cell.RowSpan * cellH + float(cell.RowSpan - 1) * gap

              context.ArrangeChild(
                i,
                ElementRect(X = x, Y = y, Width = w, Height = h)
              )
    }

  let build(args: MetroVariableGridArgs, items: Blob array) : Blob =
    let spans = if isNull args.Spans then Array.empty else args.Spans
    let hasItems = not(isNull items)
    let mutable keptCount = 0

    if hasItems then
      for i = 0 to items.Length - 1 do
        if not(isNull items[i]) then
          keptCount <- keptCount + 1

    let children = Array.zeroCreate<Blob> keptCount
    let keptSpans = Array.zeroCreate<MetroGridSpan>(min keptCount spans.Length)
    let mutable kept = 0
    let mutable keptSpanCount = 0

    if hasItems then
      for i = 0 to items.Length - 1 do
        let child = items[i]

        if not(isNull child) then
          children[kept] <- child

          if i < spans.Length then
            keptSpans[keptSpanCount] <- spans[i]
            keptSpanCount <- keptSpanCount + 1

          kept <- kept + 1

    let container =
      Container(Layout = createLayout { args with Spans = keptSpans })

    for i = 0 to children.Length - 1 do
      container.Add(children[i])

    container :> Blob

[<Struct>]
type MetroStretch =
  | None
  | Fill
  | Uniform
  | UniformToFill

[<Struct>]
type MetroStretchDirection =
  | Both
  | UpOnly
  | DownOnly

[<Struct; NoComparison>]
type MetroViewboxProps = {
  Stretch: MetroStretch
  StretchDirection: MetroStretchDirection
  ViewportWidth: float
  ViewportHeight: float
  ContentWidth: float
  ContentHeight: float
}

[<Struct; NoComparison>]
type MetroViewboxInput = {
  Stretch: MetroStretch
  StretchDirection: MetroStretchDirection
  Width: Length
  Height: Length
  FallbackContentWidth: float
  FallbackContentHeight: float
}

[<AbstractClass>]
type MetroViewboxCell() =
  inherit Cell<MetroViewboxInput>()

  abstract ViewportHandle: ElementHandle

  abstract ContentHandle: ElementHandle

  abstract ScaleX: float

  abstract ScaleY: float

[<RequireQualifiedAccess>]
module MetroViewbox =

  // A size is accepted only when finite, above the lower limit, and at most
  // 1.0e7: a larger value overflows the float32 Length of Goo to Infinity.
  // F# `max` returns NaN for `max 0.0 NaN`, so a NaN size would reach the
  // container and the transform.
  let acceptSize(v: float, limit: float, fallback: float) : float =
    if Double.IsFinite v && v > limit && v <= 1.0e7 then
      v
    else
      fallback

  let clampDirection(scale: float, dir: MetroStretchDirection) : float =
    match dir with
    | MetroStretchDirection.Both -> scale
    | MetroStretchDirection.UpOnly -> max 1.0 scale
    | MetroStretchDirection.DownOnly -> min 1.0 scale

  let computeScale(props: MetroViewboxProps) : struct (float * float) =
    if
      not(Double.IsFinite props.ViewportWidth)
      || not(Double.IsFinite props.ViewportHeight)
      || not(Double.IsFinite props.ContentWidth)
      || not(Double.IsFinite props.ContentHeight)
      || props.ViewportWidth <= 0.0
      || props.ViewportHeight <= 0.0
      || props.ContentWidth <= 0.0
      || props.ContentHeight <= 0.0
    then
      struct (1.0, 1.0)
    else
      let rawX = props.ViewportWidth / props.ContentWidth
      let rawY = props.ViewportHeight / props.ContentHeight

      let sx, sy =
        match props.Stretch with
        | MetroStretch.None -> 1.0, 1.0
        | MetroStretch.Fill -> rawX, rawY
        | MetroStretch.Uniform ->
          let u = min rawX rawY
          u, u
        | MetroStretch.UniformToFill ->
          let u = max rawX rawY
          u, u

      let cx = clampDirection(sx, props.StretchDirection)
      let cy = clampDirection(sy, props.StretchDirection)

      // A NaN scale or a division result above 1.0e6 is not a usable scale.
      let bounded(v: float) =
        if Double.IsFinite v && v <= 1.0e6 then v else 1.0

      struct (bounded cx, bounded cy)

  let defaults
    (viewportSize: LayoutSize, contentSize: LayoutSize)
    : MetroViewboxProps = {
    Stretch = MetroStretch.Uniform
    StretchDirection = MetroStretchDirection.Both
    ViewportWidth = viewportSize.Width
    ViewportHeight = viewportSize.Height
    ContentWidth = contentSize.Width
    ContentHeight = contentSize.Height
  }

  let build(props: MetroViewboxProps, content: Blob) : Blob =
    let vw = acceptSize(props.ViewportWidth, 0.0, 0.0)
    let vh = acceptSize(props.ViewportHeight, 0.0, 0.0)
    let cw = acceptSize(props.ContentWidth, 0.0, 1.0)
    let ch = acceptSize(props.ContentHeight, 0.0, 1.0)

    let struct (sx, sy) =
      computeScale {
        props with
            ViewportWidth = vw
            ViewportHeight = vh
            ContentWidth = cw
            ContentHeight = ch
      }

    let scaledW = cw * sx
    let scaledH = ch * sy
    let offsetX = (vw - scaledW) * 0.5
    let offsetY = (vh - scaledH) * 0.5

    let inner =
      Container(
        Position = PositionType.Absolute,
        Left = Length.Pixels 0.0,
        Top = Length.Pixels 0.0,
        Width = Length.Pixels cw,
        Height = Length.Pixels ch,
        TransformOriginX = Length.Pixels 0.0,
        TransformOriginY = Length.Pixels 0.0,
        Transform =
          PanelTransform(
            TranslateX = Length.Pixels offsetX,
            TranslateY = Length.Pixels offsetY,
            ScaleX = sx,
            ScaleY = sy
          )
      )

    if not(isNull content) then
      inner.Add(content)

    Container(
      Position = PositionType.Relative,
      Width = Length.Pixels vw,
      Height = Length.Pixels vh,
      FlexShrink = 0.0,
      Overflow = Overflow.Hidden
    )
      .Children(inner)
    :> Blob

  let inputDefaults(fallbackContentSize: LayoutSize) : MetroViewboxInput = {
    Stretch = MetroStretch.Uniform
    StretchDirection = MetroStretchDirection.Both
    Width = Length.Percent 100.0
    Height = Length.Percent 100.0
    FallbackContentWidth = acceptSize(fallbackContentSize.Width, 0.0, 1.0)
    FallbackContentHeight = acceptSize(fallbackContentSize.Height, 0.0, 1.0)
  }

  type ViewboxState = {
    ViewportHandle: ElementHandle
    ContentHandle: ElementHandle
    mutable Subscribed: bool
    mutable ViewportW: float
    mutable ViewportH: float
    mutable NaturalW: float
    mutable NaturalH: float
    mutable ScaleX: float
    mutable ScaleY: float
  }

  let create(renderContent: unit -> Blob) : MetroViewboxCell =
    let state: ViewboxState = {
      ViewportHandle = ElementHandle()
      ContentHandle = ElementHandle()
      Subscribed = false
      ViewportW = 0.0
      ViewportH = 0.0
      NaturalW = 0.0
      NaturalH = 0.0
      ScaleX = 1.0
      ScaleY = 1.0
    }

    let mutable self: MetroViewboxCell voption = ValueNone

    let syncMetrics() =
      let vbox = state.ViewportHandle.ContentBox
      let cbox = state.ContentHandle.BorderBox

      let nextVw = if vbox.Width > 0.0 then vbox.Width else state.ViewportW
      let nextVh = if vbox.Height > 0.0 then vbox.Height else state.ViewportH

      let nextNw =
        if cbox.Width > 0.0 && state.ScaleX > 0.0 then
          cbox.Width / state.ScaleX
        else
          state.NaturalW

      let nextNh =
        if cbox.Height > 0.0 && state.ScaleY > 0.0 then
          cbox.Height / state.ScaleY
        else
          state.NaturalH

      if
        abs(nextVw - state.ViewportW) > 0.5
        || abs(nextVh - state.ViewportH) > 0.5
        || abs(nextNw - state.NaturalW) > 0.5
        || abs(nextNh - state.NaturalH) > 0.5
      then
        state.ViewportW <- nextVw
        state.ViewportH <- nextVh
        state.NaturalW <- nextNw
        state.NaturalH <- nextNh
        self |> ValueOption.iter(fun c -> c.Rebuild())

    let onMetrics = Action<ElementMetrics>(fun _ -> syncMetrics())

    let cell = {
      new MetroViewboxCell() with
        override _.ViewportHandle = state.ViewportHandle
        override _.ContentHandle = state.ContentHandle
        override _.ScaleX = state.ScaleX
        override _.ScaleY = state.ScaleY

        override _.Mounted() =
          if not state.Subscribed then
            state.ViewportHandle.add_MetricsChanged onMetrics
            state.ContentHandle.add_MetricsChanged onMetrics
            state.Subscribed <- true

          syncMetrics()

        override _.Unmounting() =
          if state.Subscribed then
            state.ViewportHandle.remove_MetricsChanged onMetrics
            state.ContentHandle.remove_MetricsChanged onMetrics
            state.Subscribed <- false

        override _.Build(input) : Blob =
          let cw =
            if state.NaturalW > 0.0 then
              acceptSize(state.NaturalW, 0.0, 1.0)
            else
              acceptSize(input.FallbackContentWidth, 0.0, 1.0)

          let ch =
            if state.NaturalH > 0.0 then
              acceptSize(state.NaturalH, 0.0, 1.0)
            else
              acceptSize(input.FallbackContentHeight, 0.0, 1.0)

          let vw = if state.ViewportW > 0.0 then state.ViewportW else cw

          let vh = if state.ViewportH > 0.0 then state.ViewportH else ch

          let struct (sx, sy) =
            computeScale {
              Stretch = input.Stretch
              StretchDirection = input.StretchDirection
              ViewportWidth = vw
              ViewportHeight = vh
              ContentWidth = cw
              ContentHeight = ch
            }

          state.ScaleX <- sx
          state.ScaleY <- sy

          let offsetX = (vw - cw * sx) * 0.5
          let offsetY = (vh - ch * sy) * 0.5

          let inner =
            Container(
              Handle = state.ContentHandle,
              Position = PositionType.Absolute,
              Left = Length.Pixels 0.0,
              Top = Length.Pixels 0.0,
              TransformOriginX = Length.Pixels 0.0,
              TransformOriginY = Length.Pixels 0.0,
              Transform =
                PanelTransform(
                  TranslateX = Length.Pixels offsetX,
                  TranslateY = Length.Pixels offsetY,
                  ScaleX = sx,
                  ScaleY = sy
                )
            )

          let child = renderContent()

          if not(isNull child) then
            inner.Add(child)

          Container(
            Handle = state.ViewportHandle,
            Position = PositionType.Relative,
            Width = input.Width,
            Height = input.Height,
            FlexShrink = 0.0,
            Overflow = Overflow.Hidden
          )
            .Children(inner)
          :> Blob
    }

    self <- ValueSome cell
    cell

  let mount
    (key: string, input: MetroViewboxInput, factory: unit -> MetroViewboxCell)
    : Blob =
    Cell.Mount<MetroViewboxInput, MetroViewboxCell>(key, input, factory)
